using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karly.Application.Migrations
{
    /// <inheritdoc />
    public partial class PriceFloatToDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Cars",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "float");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Price",
                table: "Cars",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
