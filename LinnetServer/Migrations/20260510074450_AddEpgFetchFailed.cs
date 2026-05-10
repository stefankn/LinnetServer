using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinnetServer.Migrations
{
    /// <inheritdoc />
    public partial class AddEpgFetchFailed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EpgFetchFailed",
                table: "ChannelGroupItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EpgFetchFailed",
                table: "ChannelGroupItems");
        }
    }
}
