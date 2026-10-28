using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class DispositivosPush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dispositivos_push",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispositivos_push", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispositivos_push_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dispositivos_push_token",
                table: "dispositivos_push",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dispositivos_push_usuario_id",
                table: "dispositivos_push",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dispositivos_push");
        }
    }
}
