using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EntitiesIdUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Rooms",
                newName: "RoomId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Payments",
                newName: "PaymentId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "MaintenanceRequests",
                newName: "MaintenanceRequestId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LeaseAgreements",
                newName: "LeaseAgreementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "Rooms",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "Payments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "MaintenanceRequestId",
                table: "MaintenanceRequests",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "LeaseAgreementId",
                table: "LeaseAgreements",
                newName: "Id");
        }
    }
}
