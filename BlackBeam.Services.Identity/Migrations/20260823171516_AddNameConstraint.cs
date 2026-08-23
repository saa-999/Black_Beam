using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackBeam.Services.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddNameConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Name_NotEmpty",
                table: "UsersDB",
                sql: "LENGTH(TRIM(Name)) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Name_NotEmpty",
                table: "UsersDB");
        }
    }
}
