using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "LeaseAgreementId",
                table: "Payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments",
                column: "LeaseAgreementId",
                principalTable: "LeaseAgreements",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "LeaseAgreementId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments",
                column: "LeaseAgreementId",
                principalTable: "LeaseAgreements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
