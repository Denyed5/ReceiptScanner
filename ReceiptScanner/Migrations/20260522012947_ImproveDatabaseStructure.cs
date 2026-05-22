using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptScanner.Migrations
{
    /// <inheritdoc />
    public partial class ImproveDatabaseStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptItems_Receipts_ReceiptId",
                table: "ReceiptItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ReceiptItems");

            migrationBuilder.AlterColumn<string>(
                name: "VendorName",
                table: "Receipts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalEUR",
                table: "Receipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalBGN",
                table: "Receipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Receipts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Receipts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptId",
                table: "ReceiptItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "ReceiptItems",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptItems_CategoryId",
                table: "ReceiptItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptItems_Categories_CategoryId",
                table: "ReceiptItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptItems_Receipts_ReceiptId",
                table: "ReceiptItems",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "ReceiptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptItems_Categories_CategoryId",
                table: "ReceiptItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptItems_Receipts_ReceiptId",
                table: "ReceiptItems");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptItems_CategoryId",
                table: "ReceiptItems");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ReceiptItems");

            migrationBuilder.AlterColumn<string>(
                name: "VendorName",
                table: "Receipts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalEUR",
                table: "Receipts",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalBGN",
                table: "Receipts",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Receipts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptId",
                table: "ReceiptItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ReceiptItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptItems_Receipts_ReceiptId",
                table: "ReceiptItems",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "ReceiptId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
