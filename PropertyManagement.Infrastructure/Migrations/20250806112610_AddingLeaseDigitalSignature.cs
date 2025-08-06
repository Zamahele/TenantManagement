using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingLeaseDigitalSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "LeaseAgreements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedHtmlContent",
                table: "LeaseAgreements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedPdfPath",
                table: "LeaseAgreements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDigitallySigned",
                table: "LeaseAgreements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "LeaseAgreements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeaseTemplateId",
                table: "LeaseAgreements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDigitalSignature",
                table: "LeaseAgreements",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToTenantAt",
                table: "LeaseAgreements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "LeaseAgreements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LeaseAgreements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LeaseTemplate",
                columns: table => new
                {
                    LeaseTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TemplateVariables = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseTemplate", x => x.LeaseTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "DigitalSignature",
                columns: table => new
                {
                    DigitalSignatureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaseAgreementId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SignatureImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignerIPAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignerUserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SigningNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalSignature", x => x.DigitalSignatureId);
                    table.ForeignKey(
                        name: "FK_DigitalSignature_LeaseAgreements_LeaseAgreementId",
                        column: x => x.LeaseAgreementId,
                        principalTable: "LeaseAgreements",
                        principalColumn: "LeaseAgreementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DigitalSignature_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreements_LeaseTemplateId",
                table: "LeaseAgreements",
                column: "LeaseTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignature_LeaseAgreementId",
                table: "DigitalSignature",
                column: "LeaseAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignature_TenantId",
                table: "DigitalSignature",
                column: "TenantId");

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
                name: "FK_LeaseAgreements_LeaseTemplate_LeaseTemplateId",
                table: "LeaseAgreements");

            migrationBuilder.DropTable(
                name: "DigitalSignature");

            migrationBuilder.DropTable(
                name: "LeaseTemplate");

            migrationBuilder.DropIndex(
                name: "IX_LeaseAgreements_LeaseTemplateId",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "GeneratedHtmlContent",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "GeneratedPdfPath",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "IsDigitallySigned",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "LeaseTemplateId",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "RequiresDigitalSignature",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "SentToTenantAt",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LeaseAgreements");
        }
    }
}
