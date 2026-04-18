using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinnetServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomLogoPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomLogoPath",
                table: "ChannelGroupItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomLogoPath",
                table: "ChannelGroupItems");
        }
    }
}
