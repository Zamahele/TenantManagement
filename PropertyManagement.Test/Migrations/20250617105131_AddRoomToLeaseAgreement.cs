using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomToLeaseAgreement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add as nullable
            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "LeaseAgreements",
                type: "int",
                nullable: true);

            // 2. Set RoomId for existing rows (use the first available RoomId)
            migrationBuilder.Sql(
                @"UPDATE LeaseAgreements SET RoomId = (
                    SELECT TOP 1 RoomId FROM Rooms ORDER BY RoomId
                  ) WHERE RoomId IS NULL"
            );

            // 3. Alter to non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "RoomId",
                table: "LeaseAgreements",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // 4. Create index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreements_RoomId",
                table: "LeaseAgreements",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaseAgreements_Rooms_RoomId",
                table: "LeaseAgreements",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaseAgreements_Rooms_RoomId",
                table: "LeaseAgreements");

            migrationBuilder.DropIndex(
                name: "IX_LeaseAgreements_RoomId",
                table: "LeaseAgreements");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "LeaseAgreements");
        }
    }
}
