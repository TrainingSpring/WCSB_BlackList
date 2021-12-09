using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WCSB_Black_LIst_API.Migrations
{
    public partial class blacklist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "BlackList",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "BlackList");
        }
    }
}
