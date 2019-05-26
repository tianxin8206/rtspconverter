using System;

namespace RtspConverter.Application.Models
{
    public class Channel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ChannelName { get; set; }

        public bool IsEnable { get; set; }

        public string RtspUrl { get; set; }

        public Transport Transport { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public bool IsDelete { get; set; }
    }
}
