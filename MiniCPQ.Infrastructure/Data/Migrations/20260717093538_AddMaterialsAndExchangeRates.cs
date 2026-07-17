using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniCPQ.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialsAndExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CnyPerUnit",
                table: "Quotes",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Quotes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "CNY");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyName",
                table: "Quotes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "人民币");

            migrationBuilder.AddColumn<Guid>(
                name: "ExchangeRateId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CurrencyName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CnyPerUnit = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.CheckConstraint("CK_ExchangeRates_CnyPerUnit", "\"CnyPerUnit\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ExchangeRateId",
                table: "Quotes",
                column: "ExchangeRateId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quotes_CnyPerUnit",
                table: "Quotes",
                sql: "\"CnyPerUnit\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyCode",
                table: "ExchangeRates",
                column: "CurrencyCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_ExchangeRates_ExchangeRateId",
                table: "Quotes",
                column: "ExchangeRateId",
                principalTable: "ExchangeRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_ExchangeRates_ExchangeRateId",
                table: "Quotes");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_ExchangeRateId",
                table: "Quotes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Quotes_CnyPerUnit",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CnyPerUnit",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CurrencyName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ExchangeRateId",
                table: "Quotes");
        }
    }
}
