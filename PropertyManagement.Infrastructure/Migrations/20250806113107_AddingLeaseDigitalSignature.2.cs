using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingLeaseDigitalSignature2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DigitalSignature_Tenants_TenantId",
                table: "DigitalSignature");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaseAgreements_LeaseTemplate_LeaseTemplateId",
                table: "LeaseAgreements");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalSignature_Tenants_TenantId",
                table: "DigitalSignature",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "TenantId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaseAgreements_LeaseTemplate_LeaseTemplateId",
                table: "LeaseAgreements",
                column: "LeaseTemplateId",
                principalTable: "LeaseTemplate",
                principalColumn: "LeaseTemplateId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DigitalSignature_Tenants_TenantId",
                table: "DigitalSignature");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaseAgreements_LeaseTemplate_LeaseTemplateId",
                table: "LeaseAgreements");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalSignature_Tenants_TenantId",
                table: "DigitalSignature",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "TenantId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaseAgreements_LeaseTemplate_LeaseTemplateId",
                table: "LeaseAgreements",
                column: "LeaseTemplateId",
                principalTable: "LeaseTemplate",
                principalColumn: "LeaseTemplateId");
        }
    }
}
