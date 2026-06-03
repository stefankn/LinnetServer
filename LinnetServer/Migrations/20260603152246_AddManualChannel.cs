using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinnetServer.Migrations
{
    /// <inheritdoc />
    public partial class AddManualChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StreamId",
                table: "ChannelGroupItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "IsManual",
                table: "ChannelGroupItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManualStreamUrl",
                table: "ChannelGroupItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManual",
                table: "ChannelGroupItems");

            migrationBuilder.DropColumn(
                name: "ManualStreamUrl",
                table: "ChannelGroupItems");

            migrationBuilder.AlterColumn<int>(
                name: "StreamId",
                table: "ChannelGroupItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
