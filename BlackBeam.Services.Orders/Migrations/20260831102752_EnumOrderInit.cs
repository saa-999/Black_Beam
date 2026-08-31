using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackBeam.Services.Orders.Migrations
{
    /// <inheritdoc />
    public partial class EnumOrderInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "order",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "order");
        }
    }
}
