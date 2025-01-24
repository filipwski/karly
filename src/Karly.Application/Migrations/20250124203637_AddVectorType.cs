using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Karly.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddVectorType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarEmbedding_Cars_CarId",
                table: "CarEmbedding");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarEmbedding",
                table: "CarEmbedding");

            migrationBuilder.RenameTable(
                name: "CarEmbedding",
                newName: "CarEmbeddings");

            migrationBuilder.RenameIndex(
                name: "IX_CarEmbedding_CarId",
                table: "CarEmbeddings",
                newName: "IX_CarEmbeddings_CarId");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "CarEmbeddings",
                type: "vector(1536)",
                nullable: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarEmbeddings",
                table: "CarEmbeddings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CarEmbeddings_Cars_CarId",
                table: "CarEmbeddings",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarEmbeddings_Cars_CarId",
                table: "CarEmbeddings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarEmbeddings",
                table: "CarEmbeddings");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "CarEmbeddings");

            migrationBuilder.RenameTable(
                name: "CarEmbeddings",
                newName: "CarEmbedding");

            migrationBuilder.RenameIndex(
                name: "IX_CarEmbeddings_CarId",
                table: "CarEmbedding",
                newName: "IX_CarEmbedding_CarId");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarEmbedding",
                table: "CarEmbedding",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CarEmbedding_Cars_CarId",
                table: "CarEmbedding",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
