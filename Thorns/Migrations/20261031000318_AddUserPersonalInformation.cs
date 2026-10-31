using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thorns.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPersonalInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cep",
                table: "users",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cpf",
                table: "users",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_cpf",
                table: "users",
                column: "cpf",
                unique: true,
                filter: "\"cpf\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_cpf",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cep",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cpf",
                table: "users");
        }
    }
}
