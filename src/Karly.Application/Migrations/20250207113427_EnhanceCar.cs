using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karly.Application.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceCar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAutomaticTransmission",
                table: "Cars",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsElectric",
                table: "Cars",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "Cars",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Make",
                table: "Cars",
                type: "text",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Mileage",
                table: "Cars",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAutomaticTransmission",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "IsElectric",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Make",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Mileage",
                table: "Cars");
        }
    }
}
