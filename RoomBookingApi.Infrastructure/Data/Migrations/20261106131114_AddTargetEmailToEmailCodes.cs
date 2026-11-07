using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomBookingApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetEmailToEmailCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetEmail",
                table: "EmailCodes",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetEmail",
                table: "EmailCodes");
        }
    }
}
