using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RtspConverter.Application.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_Channels",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    ChannelName = table.Column<string>(maxLength: 50, nullable: false),
                    IsEnable = table.Column<bool>(nullable: false),
                    RtspUrl = table.Column<string>(maxLength: 255, nullable: false),
                    Transport = table.Column<string>(maxLength: 1, nullable: false),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    IsDelete = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Channels", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_Channels");
        }
    }
}
