using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackBeam.Services.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddStrictDbConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_User_PhoneNumber",
                table: "UsersDB",
                sql: "LENGTH(PhoneNumber) = 10 AND PhoneNumber LIKE '05%'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Role",
                table: "UsersDB",
                sql: "Role IN ('Admin', 'Cashier', 'Customer')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_PhoneNumber",
                table: "UsersDB");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Role",
                table: "UsersDB");
        }
    }
}
