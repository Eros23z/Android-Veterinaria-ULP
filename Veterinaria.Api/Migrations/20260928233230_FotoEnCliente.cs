using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class FotoEnCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "foto_url",
                table: "clientes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "foto_url",
                table: "clientes");
        }
    }
}
