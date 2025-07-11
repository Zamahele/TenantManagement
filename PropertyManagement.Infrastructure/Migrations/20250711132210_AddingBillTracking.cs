using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingBillTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UtilityBills",
                columns: table => new
                {
                    UtilityBillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    BillingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaterUsage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ElectricityUsage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityBills", x => x.UtilityBillId);
                    table.ForeignKey(
                        name: "FK_UtilityBills_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_RoomId",
                table: "UtilityBills",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UtilityBills");
        }
    }
}
