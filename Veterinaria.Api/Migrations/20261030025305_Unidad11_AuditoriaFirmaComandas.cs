using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class Unidad11_AuditoriaFirmaComandas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedido_items_pedidos_pedido_id",
                table: "pedido_items");

            migrationBuilder.DropForeignKey(
                name: "fk_pedido_items_productos_producto_id",
                table: "pedido_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_pedido_items",
                table: "pedido_items");

            migrationBuilder.RenameTable(
                name: "pedido_items",
                newName: "pedido_item");

            migrationBuilder.RenameIndex(
                name: "ix_pedido_items_producto_id",
                table: "pedido_item",
                newName: "ix_pedido_item_producto_id");

            migrationBuilder.RenameIndex(
                name: "ix_pedido_items_pedido_id",
                table: "pedido_item",
                newName: "ix_pedido_item_pedido_id");

            migrationBuilder.AddColumn<string>(
                name: "firma_url",
                table: "entregas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "pedido_item",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "pk_pedido_item",
                table: "pedido_item",
                column: "id");

            migrationBuilder.CreateTable(
                name: "historiales",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pedido_id = table.Column<long>(type: "bigint", nullable: true),
                    entrega_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    estado_anterior = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    estado_nuevo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historiales", x => x.id);
                    table.ForeignKey(
                        name: "fk_historiales_entregas_entrega_id",
                        column: x => x.entrega_id,
                        principalTable: "entregas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historiales_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historiales_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_historiales_entrega_id",
                table: "historiales",
                column: "entrega_id");

            migrationBuilder.CreateIndex(
                name: "ix_historiales_pedido_id",
                table: "historiales",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_historiales_usuario_id",
                table: "historiales",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_item_pedidos_pedido_id",
                table: "pedido_item",
                column: "pedido_id",
                principalTable: "pedidos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_item_productos_producto_id",
                table: "pedido_item",
                column: "producto_id",
                principalTable: "productos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedido_item_pedidos_pedido_id",
                table: "pedido_item");

            migrationBuilder.DropForeignKey(
                name: "fk_pedido_item_productos_producto_id",
                table: "pedido_item");

            migrationBuilder.DropTable(
                name: "historiales");

            migrationBuilder.DropPrimaryKey(
                name: "pk_pedido_item",
                table: "pedido_item");

            migrationBuilder.DropColumn(
                name: "firma_url",
                table: "entregas");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "pedido_item");

            migrationBuilder.RenameTable(
                name: "pedido_item",
                newName: "pedido_items");

            migrationBuilder.RenameIndex(
                name: "ix_pedido_item_producto_id",
                table: "pedido_items",
                newName: "ix_pedido_items_producto_id");

            migrationBuilder.RenameIndex(
                name: "ix_pedido_item_pedido_id",
                table: "pedido_items",
                newName: "ix_pedido_items_pedido_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_pedido_items",
                table: "pedido_items",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_items_pedidos_pedido_id",
                table: "pedido_items",
                column: "pedido_id",
                principalTable: "pedidos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_items_productos_producto_id",
                table: "pedido_items",
                column: "producto_id",
                principalTable: "productos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
