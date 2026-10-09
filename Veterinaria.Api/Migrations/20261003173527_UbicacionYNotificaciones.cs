using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class UbicacionYNotificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "pedidos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "mostrador");

            migrationBuilder.AddColumn<long>(
                name: "usuario_creador_id",
                table: "pedidos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "direccion_latitud",
                table: "clientes",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "direccion_longitud",
                table: "clientes",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "entregas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pedido_id = table.Column<long>(type: "bigint", nullable: false),
                    repartidor_id = table.Column<long>(type: "bigint", nullable: true),
                    direccion_linea = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    direccion_referencia = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    direccion_latitud = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    direccion_longitud = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    asignada_en = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    entregada_en = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entregas", x => x.id);
                    table.ForeignKey(
                        name: "fk_entregas_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_entregas_usuarios_repartidor_id",
                        column: x => x.repartidor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    leida_en = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_notificaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_usuario_creador_id",
                table: "pedidos",
                column: "usuario_creador_id");

            migrationBuilder.CreateIndex(
                name: "ix_entregas_pedido_id",
                table: "entregas",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_entregas_repartidor_id",
                table: "entregas",
                column: "repartidor_id");

            migrationBuilder.CreateIndex(
                name: "ix_notificaciones_usuario_id",
                table: "notificaciones",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pedidos_usuarios_usuario_creador_id",
                table: "pedidos",
                column: "usuario_creador_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedidos_usuarios_usuario_creador_id",
                table: "pedidos");

            migrationBuilder.DropTable(
                name: "entregas");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropIndex(
                name: "ix_pedidos_usuario_creador_id",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "usuario_creador_id",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "direccion_latitud",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "direccion_longitud",
                table: "clientes");
        }
    }
}
