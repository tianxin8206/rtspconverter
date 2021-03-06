﻿using System;
using FFmpeg.AutoGen;
using FFmpeg.AutoGen.Example;

namespace ConsoleApp1
{
    unsafe class Program
    {
        static AVFormatContext* ifmt_ctx;
        static AVFormatContext* ofmt_ctx;
        static SwrContext* pSwrCtx = null;
        static AVBSFContext* absCtx;

        static void Main(string[] args)
        {
            FFmpegBinariesHelper.RegisterFFmpegBinaries();

            int ret;
            AVPacket packet; //= { .data = NULL, .size = 0 };
            packet.data = null;
            packet.size = 0;
            AVFrame* frame = null;

            AVMediaType type;
            int stream_index;
            int i;
            //ffmpeg.av_register_all();
            //ffmpeg.avfilter_register_all();
            if ((ret = OpenInputFile("rtsp://113.136.42.40:554/PLTV/88888888/224/3221226090/10000100000000060000000001759099_0.smil")) < 0)
                goto end;
            if ((ret = OpenOutputFile("E:\\hls\\out.m3u8")) < 0)
                goto end;
            //var avBitStreamFilter = ffmpeg.av_bsf_get_by_name("h264_mp4toannexb");
            //fixed (AVBSFContext** ctx = &absCtx)
            //ffmpeg.av_bsf_alloc(avBitStreamFilter, ctx);
            //ffmpeg.av_bsf_init(absCtx);
            /* read all packets */
            int count = 0;
            int flag = 1;
            while (true)
            {
                if ((ret = ffmpeg.av_read_frame(ifmt_ctx, &packet)) < 0)
                    break;
                stream_index = packet.stream_index;
                type = ifmt_ctx->streams[packet.stream_index]->codec->codec_type;
                ffmpeg.av_log(null, ffmpeg.AV_LOG_DEBUG, "Demuxer gave frame of stream_index %u\n");

                ffmpeg.av_log(null, ffmpeg.AV_LOG_DEBUG, "Going to reencode&filter the frame\n");
                frame = ffmpeg.av_frame_alloc();
                if (null == frame)
                {
                    ret = ffmpeg.AVERROR(12);
                    break;
                }
                ffmpeg.av_packet_rescale_ts(&packet,
                    ifmt_ctx->streams[stream_index]->time_base,
                    ifmt_ctx->streams[stream_index]->codec->time_base);

                ret = dec_func(ifmt_ctx->streams[stream_index]->codec, frame, &packet);
                if (ret < 0)
                {
                    ffmpeg.av_frame_free(&frame);
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Decoding failed\n");
                    break;
                }
                //if (got_frame == 0)
                //{
                frame->pts = frame->pkt_pts;
                // frame->pts = av_frame_get_best_effort_timestamp(frame);
                // frame->pts=count;
                if (type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    ret = encode_write_frame(frame, stream_index, null);
                }
                else
                {
                    if (flag != 0)
                    {
                        InitSwr(stream_index);
                        flag = 0;
                    }

                    AVFrame* frame_out = ffmpeg.av_frame_alloc();
                    if (0 != TransSample(frame, frame_out, stream_index))
                    {
                        ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "convert audio failed\n");
                        ret = -1;
                    }
                    // frame_out->pts = frame->pkt_pts;
                    ret = encode_write_frame(frame_out, stream_index, null);
                    ffmpeg.av_frame_free(&frame_out);
                }
                ffmpeg.av_frame_free(&frame);
                if (ret < 0)
                    goto end;
                //}
                //else
                //{
                //    ffmpeg.av_frame_free(&frame);
                //}

                ffmpeg.av_packet_unref(&packet);
                ++count;
            }
            /* flush  encoders */
            // for (i = 0; i < ifmt_ctx->nb_streams; i++) {
            // ret = flush_encoder(i);
            // if (ret < 0) {
            // av_log(NULL, AV_LOG_ERROR, "Flushing encoder failed\n");
            // goto end;
            // }
            // }
            ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Flushing encoder failed\n");
            ffmpeg.av_write_trailer(ofmt_ctx);
        end:
            ffmpeg.av_packet_unref(&packet);
            ffmpeg.av_frame_free(&frame);
            //fixed (AVBSFContext** ctx = &absCtx)
            //ffmpeg.av_bsf_free(ctx);
            for (i = 0; i < ifmt_ctx->nb_streams; i++)
            {
                ffmpeg.avcodec_close(ifmt_ctx->streams[i]->codec);
                if (ofmt_ctx != null && ofmt_ctx->nb_streams > i && ofmt_ctx->streams[i] != null && ofmt_ctx->streams[i]->codec != null)
                    ffmpeg.avcodec_close(ofmt_ctx->streams[i]->codec);
            }
            // av_free(filter_ctx);
            fixed (AVFormatContext** ss = &ifmt_ctx)
                ffmpeg.avformat_close_input(ss);

            if (ofmt_ctx != null && (ofmt_ctx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                ffmpeg.avio_closep(&ofmt_ctx->pb);
            ffmpeg.avformat_free_context(ofmt_ctx);

            // if (ret < 0)
            // av_log(NULL, AV_LOG_ERROR, "Error occurred: %s\n", av_err2str(ret)); //av_err2str(ret));
        }

        public static void InitSwr(int audioIndex)
        {
            if (ofmt_ctx->streams[0]->codec->channels != ifmt_ctx->streams[audioIndex]->codec->channels
                || ofmt_ctx->streams[0]->codec->sample_rate != ifmt_ctx->streams[audioIndex]->codec->sample_rate
                || ofmt_ctx->streams[0]->codec->sample_fmt != ifmt_ctx->streams[audioIndex]->codec->sample_fmt)
            {
                if (pSwrCtx == null)
                    pSwrCtx = ffmpeg.swr_alloc();

                pSwrCtx = ffmpeg.swr_alloc_set_opts(null,
                       (long)ofmt_ctx->streams[audioIndex]->codec->channel_layout,
                       ofmt_ctx->streams[audioIndex]->codec->sample_fmt,
                       ofmt_ctx->streams[audioIndex]->codec->sample_rate,
                       (long)ifmt_ctx->streams[audioIndex]->codec->channel_layout,
                       ifmt_ctx->streams[audioIndex]->codec->sample_fmt,
                       ifmt_ctx->streams[audioIndex]->codec->sample_rate,
                       0, null);
                ffmpeg.swr_init(pSwrCtx);
            }
        }

        static void setup_array(int[] arr, int value, int format, int samples)
        {
            if (ffmpeg.av_sample_fmt_is_planar((AVSampleFormat)format) > 0)
            {
                int i;
                int plane_size = ffmpeg.av_get_bytes_per_sample((AVSampleFormat)(format & 0xFF)) * samples;
                format &= 0xFF;
                for (i = 0; i < arr.Length; i++)
                {
                    arr[i] = value + i * plane_size;
                }
            }
            else
            {
                arr[0] = value;
            }
        }

        public static void SetupArray(int[] arr, AVFrame* in_frame, AVSampleFormat format)
        {
            if (ffmpeg.av_sample_fmt_is_planar(format) > 0)
            {
                for (int i = 0; i < in_frame->channels; i++)
                {
                    int arrIndex = *in_frame->data[(uint)i];
                    arr[i] = arrIndex;
                }
            }
            else
            {
                int value = *in_frame->data[0];
                arr[0] = value;
            }
        }


        static int TransSample(AVFrame* in_frame, AVFrame* out_frame, int audio_index)
        {
            int ret;
            int max_dst_nb_samples = 4096;
            int src_nb_samples = in_frame->nb_samples;
            out_frame->pts = in_frame->pts;
            int len;
            if (pSwrCtx != null)
            {
                out_frame->nb_samples = (int)ffmpeg.av_rescale_rnd(ffmpeg.swr_get_delay(pSwrCtx, ofmt_ctx->streams[audio_index]->codec->sample_rate) + src_nb_samples,
                    ofmt_ctx->streams[audio_index]->codec->sample_rate, ifmt_ctx->streams[audio_index]->codec->sample_rate, AVRounding.AV_ROUND_UP);

                int* linesizePtr;
                int linesize = out_frame->linesize[0];
                linesizePtr = &linesize;

                fixed (byte** audio_data = out_frame->data.ToArray())
                {
                    ret = ffmpeg.av_samples_alloc(audio_data, linesizePtr,
                    ofmt_ctx->streams[audio_index]->codec->channels,
                    out_frame->nb_samples,
                    ofmt_ctx->streams[audio_index]->codec->sample_fmt, 0);
                }

                if (ret < 0)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_WARNING, "[%s.%d %s() Could not allocate samples Buffer\n");
                    return -1;
                }

                max_dst_nb_samples = out_frame->nb_samples;
                //输入也可能是分平面的，所以要做如下处理  
                int[] m_ain = new int[32];
                SetupArray(m_ain, in_frame, ifmt_ctx->streams[audio_index]->codec->sample_fmt);

                //注意这里，out_count和in_count是samples单位，不是byte  
                //所以这样av_get_bytes_per_sample(ifmt_ctx->streams[audio_index]->codec->sample_fmt) * src_nb_samples是错的
                fixed (byte** @in = in_frame->data.ToArray())
                fixed (byte** @out = out_frame->data.ToArray())
                {
                    len = ffmpeg.swr_convert(pSwrCtx, @out, out_frame->nb_samples, @in, src_nb_samples);
                }

                if (len != 0)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_WARNING, "[%s:%d] swr_convert!(%d)(%s)");
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("pSwrCtx with out init!");
                return -1;
            }
            return 0;
        }

        static int OpenInputFile(string filename)
        {
            int ret;
            ifmt_ctx = null;
            fixed (AVFormatContext** ps = &ifmt_ctx)
            {
                AVDictionary* options;
                ffmpeg.av_dict_set(&options, "rtsp_transport", "tcp", 0);
                ffmpeg.avformat_open_input(ps, filename, null, &options);

                if ((ret = ffmpeg.avformat_find_stream_info(ifmt_ctx, null)) < 0)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Cannot find stream information\n");
                    return ret;
                }
            }
            
            for (int i = 0; i < ifmt_ctx->nb_streams; i++)
            {
                AVStream* stream;
                AVCodecContext* codec_ctx;
                stream = ifmt_ctx->streams[i];
                codec_ctx = stream->codec;
                /* Reencode video & audio and remux subtitles etc. */
                if (codec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO || codec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    /* Open decoder */
                    ret = ffmpeg.avcodec_open2(codec_ctx, ffmpeg.avcodec_find_decoder(codec_ctx->codec_id), null);
                    if (ret < 0)
                    {
                        ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Failed to open decoder for stream #%u\n");
                        return ret;
                    }
                }
            }
            ffmpeg.av_dump_format(ifmt_ctx, 0, filename, 0);
            return 0;
        }

        static int OpenOutputFile(string filename)
        {
            AVStream* out_stream;
            AVStream* in_stream;
            AVCodecContext* dec_ctx;
            AVCodecContext* enc_ctx;
            AVCodec* encoder;
            int ret;
            ofmt_ctx = null;

            fixed (AVFormatContext** ctx = &ofmt_ctx)
            {
                ffmpeg.avformat_alloc_output_context2(ctx, null, null, filename);
            }

            if (null == ofmt_ctx)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Could not create output context\n");
                return ffmpeg.AVERROR_UNKNOWN;
            }
            for (int i = 0; i < ifmt_ctx->nb_streams; i++)
            {
                out_stream = ffmpeg.avformat_new_stream(ofmt_ctx, null);
                if (out_stream == null)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Failed allocating output stream\n");
                    return ffmpeg.AVERROR_UNKNOWN;
                }

                in_stream = ifmt_ctx->streams[i];
                dec_ctx = in_stream->codec;
                enc_ctx = out_stream->codec;

                if (dec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    encoder = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);
                    if (null == encoder)
                    {
                        ffmpeg.av_log(null, ffmpeg.AV_LOG_FATAL, "Neccessary encoder not found\n");
                        return ffmpeg.AVERROR_INVALIDDATA;
                    }

                    enc_ctx->height = dec_ctx->height;
                    enc_ctx->width = dec_ctx->width;
                    enc_ctx->sample_aspect_ratio = dec_ctx->sample_aspect_ratio;

                    enc_ctx->pix_fmt = encoder->pix_fmts[0];

                    enc_ctx->time_base = dec_ctx->time_base;

                    // enc_ctx->me_range = 25;
                    // enc_ctx->max_qdiff = 4;
                    enc_ctx->qmin = 10;
                    enc_ctx->qmax = 51;
                    // enc_ctx->qcompress = 0.6;
                    // enc_ctx->refs = 3;
                    enc_ctx->max_b_frames = 3;
                    enc_ctx->gop_size = 250;
                    enc_ctx->bit_rate = 500000;
                    enc_ctx->time_base.num = dec_ctx->time_base.num;
                    enc_ctx->time_base.den = dec_ctx->time_base.den;

                    ret = ffmpeg.avcodec_open2(enc_ctx, encoder, null);
                    if (ret < 0)
                    {
                        ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Cannot open video encoder for stream #%u\n");
                        return ret;
                    }

                    //ffmpeg.av_opt_set(ofmt_ctx->priv_data, "preset", "superfast", 0);
                    //ffmpeg.av_opt_set(ofmt_ctx->priv_data, "tune", "zerolatency", 0);
                    ffmpeg.av_opt_set_int(ofmt_ctx->priv_data, "hls_time", 5, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set_int(ofmt_ctx->priv_data, "hls_list_size", 10, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                }
                else if (dec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_UNKNOWN)
                {
                    ffmpeg.av_log(null, ffmpeg.AV_LOG_FATAL, "Elementary stream #%d is of unknown type, cannot proceed\n");
                    return ffmpeg.AVERROR_INVALIDDATA;
                }
                else if (dec_ctx->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    encoder = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC);
                    enc_ctx->sample_rate = dec_ctx->sample_rate;
                    enc_ctx->channel_layout = dec_ctx->channel_layout;
                    enc_ctx->channels = ffmpeg.av_get_channel_layout_nb_channels(enc_ctx->channel_layout);
                    enc_ctx->sample_fmt = encoder->sample_fmts[0];
                    AVRational ar = new AVRational();
                    ar.num = 1;
                    ar.den = enc_ctx->sample_rate;

                    //{ 1, enc_ctx->sample_rate };
                    enc_ctx->time_base = ar;

                    ret = ffmpeg.avcodec_open2(enc_ctx, encoder, null);
                    if (ret < 0)
                    {
                        ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Cannot open video encoder for stream #%u\n");
                        return ret;
                    }
                }
                else
                {
                    ret = ffmpeg.avcodec_parameters_to_context(ofmt_ctx->streams[i]->codec, ofmt_ctx->streams[i]->codecpar);
                    //ret = ffmpeg.avcodec_copy_context(ofmt_ctx->streams[i]->codec, ifmt_ctx->streams[i]->codec);
                    if (ret < 0)
                    {
                        ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Copying stream context failed\n");
                        return ret;
                    }

                    ret = ffmpeg.avcodec_parameters_from_context(ifmt_ctx->streams[i]->codecpar, ifmt_ctx->streams[i]->codec);
                    if (ret < 0)
                    {
                        ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Copying stream context failed\n");
                        return ret;
                    }
                }
                if ((ofmt_ctx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) > 0)
                    enc_ctx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;

            }
            ffmpeg.av_dump_format(ofmt_ctx, 0, filename, 1);

            // if (!(ofmt_ctx->oformat->flags & AVFMT_NOFILE)) {
            ret = ffmpeg.avio_open(&ofmt_ctx->pb, filename, ffmpeg.AVIO_FLAG_WRITE);
            if (ret < 0)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Could not open output file '%s'");
                return ret;
            }
            // }
            /* init muxer, write output file header */
            ret = ffmpeg.avformat_write_header(ofmt_ctx, null);
            if (ret < 0)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Error occurred when opening output file\n");
                return ret;
            }
            return 0;
        }

        public static int enc_func(AVCodecContext* ctx, AVPacket* pct, AVFrame* frame)
        {
            int ret = ffmpeg.avcodec_send_frame(ctx, frame);
            if (ret < 0)
                return ret;

            ret = ffmpeg.avcodec_receive_packet(ctx, pct);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                return 0;

            return -1;
        }

        static int encode_write_frame(AVFrame* filt_frame, int stream_index, int* got_frame)
        {
            long a_total_duration = 0;
            int ret;
            int got_frame_local;
            AVPacket enc_pkt;

            if (null != got_frame)
                got_frame = &got_frame_local;

            ffmpeg.av_log(null, ffmpeg.AV_LOG_INFO, "Encoding frame\n");
            /* encode filtered frame */
            enc_pkt.data = null;
            enc_pkt.size = 0;
            ffmpeg.av_init_packet(&enc_pkt);
            ret = enc_func(ofmt_ctx->streams[stream_index]->codec, &enc_pkt, filt_frame);
            if (ret < 0)
                return ret;

            var toFrame = *got_frame;
            if (toFrame == 0)
                return 0;
            // if (ifmt_ctx->streams[stream_index]->codec->codec_type !=
            // AVMEDIA_TYPE_VIDEO)
            // av_bitstream_filter_filter(aacbsfc, ofmt_ctx->streams[stream_index]->codec, NULL, &enc_pkt.data, &enc_pkt.size, enc_pkt.data, enc_pkt.size, 0);

            /* prepare packet for muxing */
            enc_pkt.stream_index = stream_index;
            ffmpeg.av_packet_rescale_ts(&enc_pkt,
                ofmt_ctx->streams[stream_index]->codec->time_base,
                ofmt_ctx->streams[stream_index]->time_base);

            if (ifmt_ctx->streams[stream_index]->codec->codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO)
            {
                enc_pkt.pts = enc_pkt.dts = a_total_duration;
                a_total_duration += ffmpeg.av_rescale_q(filt_frame->nb_samples, ofmt_ctx->streams[stream_index]->codec->time_base, ofmt_ctx->streams[stream_index]->time_base);
            }
            // printf("v_total_duration: %d, a_total_duration: %d\n", v_total_duration, a_total_duration);
            ffmpeg.av_log(null, ffmpeg.AV_LOG_DEBUG, "Muxing frame\n");
            /* mux encoded frame */
            ret = ffmpeg.av_interleaved_write_frame(ofmt_ctx, &enc_pkt);
            return ret;
        }

        static int FlushEncoder(int stream_index)
        {
            int ret;
            int got_frame;
            AVPacket enc_pkt;

            if ((ofmt_ctx->streams[stream_index]->codec->codec->capabilities & ffmpeg.AV_CODEC_CAP_DELAY) != 0)
                return 0;

            while (true)
            {
                enc_pkt.data = null;
                enc_pkt.size = 0;
                ffmpeg.av_init_packet(&enc_pkt);

                ffmpeg.av_log(null, ffmpeg.AV_LOG_INFO, "Flushing stream #%u encoder\n");
                ret = encode_write_frame(null, stream_index, &got_frame);
                if (ret < 0)
                    break;
                if (got_frame != 0)
                    return 0;
            }
            return ret;
        }

        static int dec_func(AVCodecContext* ctx, AVFrame* frame, AVPacket* act)
        {
            int ret = ffmpeg.avcodec_send_packet(ctx, act);
            if (ret < 0)
                return ret;

            ret = ffmpeg.avcodec_receive_frame(ctx, frame);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                return 0;

            return -1;
        }
    }
}
