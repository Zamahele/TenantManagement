using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SpecifyDecimalTypeForPaymentAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_LeaseAgreements_LeaseAgreementId",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "payments");

            migrationBuilder.RenameTable(
                name: "payments",
                newName: "Payments");

            migrationBuilder.RenameIndex(
                name: "IX_payments_LeaseAgreementId",
                table: "Payments",
                newName: "IX_Payments_LeaseAgreementId");

            migrationBuilder.AddColumn<decimal>(
                name: "RentAmount",
                table: "LeaseAgreements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments",
                column: "LeaseAgreementId",
                principalTable: "LeaseAgreements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RentAmount",
                table: "LeaseAgreements");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "payments");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_LeaseAgreementId",
                table: "payments",
                newName: "IX_payments_LeaseAgreementId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_LeaseAgreements_LeaseAgreementId",
                table: "payments",
                column: "LeaseAgreementId",
                principalTable: "LeaseAgreements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
