using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaseAgreements_Rooms_RoomId",
                table: "LeaseAgreements");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Rooms_RoomId",
                table: "Tenants");

            // 1. Add UserId as nullable
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            // 2. Create a default user for orphan tenants (if needed)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'legacy_tenant')
                BEGIN
                    INSERT INTO Users (Username, PasswordHash, Role)
                    VALUES ('legacy_tenant', '', 'Tenant')
                END
            ");

            // 3. Assign all existing tenants to the default user
            migrationBuilder.Sql(@"
                UPDATE Tenants
                SET UserId = (SELECT TOP 1 UserId FROM Users WHERE Username = 'legacy_tenant')
                WHERE UserId IS NULL
            ");

            // 4. Alter column to be non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Tenants",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // 5. Create index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_UserId",
                table: "Tenants",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaseAgreements_Rooms_RoomId",
                table: "LeaseAgreements",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments",
                column: "LeaseAgreementId",
                principalTable: "LeaseAgreements",
                principalColumn: "LeaseAgreementId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Rooms_RoomId",
                table: "Tenants",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Users_UserId",
                table: "Tenants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaseAgreements_Rooms_RoomId",
                table: "LeaseAgreements");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Rooms_RoomId",
                table: "Tenants");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Users_UserId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_UserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tenants");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaseAgreements_Rooms_RoomId",
                table: "LeaseAgreements",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_LeaseAgreements_LeaseAgreementId",
                table: "Payments",
                column: "LeaseAgreementId",
                principalTable: "LeaseAgreements",
                principalColumn: "LeaseAgreementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Rooms_RoomId",
                table: "Tenants",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
