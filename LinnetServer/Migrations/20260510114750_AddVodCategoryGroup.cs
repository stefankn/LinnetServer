using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinnetServer.Migrations
{
    /// <inheritdoc />
    public partial class AddVodCategoryGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ChannelGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VodCategoryId",
                table: "ChannelGroups",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "ChannelGroups");

            migrationBuilder.DropColumn(
                name: "VodCategoryId",
                table: "ChannelGroups");
        }
    }
}
