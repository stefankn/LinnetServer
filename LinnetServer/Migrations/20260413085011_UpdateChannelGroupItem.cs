using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinnetServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChannelGroupItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChannelId",
                table: "ChannelGroupItems",
                newName: "StreamIcon");

            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "ChannelGroupItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EpgChannelId",
                table: "ChannelGroupItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdult",
                table: "ChannelGroupItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StreamId",
                table: "ChannelGroupItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ChannelGroupItems");

            migrationBuilder.DropColumn(
                name: "EpgChannelId",
                table: "ChannelGroupItems");

            migrationBuilder.DropColumn(
                name: "IsAdult",
                table: "ChannelGroupItems");

            migrationBuilder.DropColumn(
                name: "StreamId",
                table: "ChannelGroupItems");

            migrationBuilder.RenameColumn(
                name: "StreamIcon",
                table: "ChannelGroupItems",
                newName: "ChannelId");
        }
    }
}
