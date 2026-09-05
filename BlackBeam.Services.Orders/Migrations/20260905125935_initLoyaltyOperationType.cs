using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackBeam.Services.Orders.Migrations
{
    /// <inheritdoc />
    public partial class initLoyaltyOperationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "order",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "order");
        }
    }
}
