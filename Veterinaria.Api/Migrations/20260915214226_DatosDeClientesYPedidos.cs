using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class DatosDeClientesYPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

            // Clientes (12)
            var clientes = new object[12, 8];
            for (int i = 1; i <= 12; i++)
            {
                clientes[i - 1, 0] = (long)i;
                clientes[i - 1, 1] = $"Cliente {i}";
                clientes[i - 1, 2] = $"cliente{i}@mail.com";
                clientes[i - 1, 3] = $"555-00{i:D2}";
                clientes[i - 1, 4] = $"Calle Falsa {i}";
                clientes[i - 1, 5] = now;
                clientes[i - 1, 6] = now;
                clientes[i - 1, 7] = true;
            }

            migrationBuilder.InsertData(
                table: "clientes",
                columns: new[] { "id", "nombre", "email", "telefono", "direccion", "creado_en", "actualizado_en", "activo" },
                values: clientes
            );

            // Pedidos (8) con los estados solicitados: Borrador, Confirmado, EnPreparacion, Listo, Entregado, Cancelado
            var estados = new[] { 0, 1, 2, 3, 4, 6, 0, 1 }; // Borrador, Confirmado, EnPreparacion, Listo, Entregado, Cancelado, Borrador, Confirmado
            var totales = new[] { 12000m, 20500m, 15700m, 45000m, 18000m, 10800m, 24000m, 52200m };
            var notas = new[] {
                "Chequeo de rutina de mascota",
                "Vacunación y consulta anual",
                "Tratamiento antipulgas y vacuna",
                "Compra de alimento especial 15kg",
                "Ecografía realizada exitosamente",
                "Cancelado por reprogramación del cliente",
                "Presupuesto de cirugía menor",
                "Alimento balanceado y pipeta"
            };

            var pedidos = new object[8, 9];
            for (int i = 1; i <= 8; i++)
            {
                pedidos[i - 1, 0] = (long)i;
                pedidos[i - 1, 1] = (long)i; // cliente_id (1 a 8)
                pedidos[i - 1, 2] = now.AddDays(-i); // fecha_pedido
                pedidos[i - 1, 3] = totales[i - 1]; // total
                pedidos[i - 1, 4] = estados[i - 1]; // estado
                pedidos[i - 1, 5] = notas[i - 1]; // notas
                pedidos[i - 1, 6] = now;
                pedidos[i - 1, 7] = now;
                pedidos[i - 1, 8] = true;
            }

            migrationBuilder.InsertData(
                table: "pedidos",
                columns: new[] { "id", "cliente_id", "fecha_pedido", "total", "estado", "notas", "creado_en", "actualizado_en", "activo" },
                values: pedidos
            );

            // PedidoItems (10 ítems vinculados a los 8 pedidos y productos 1-6)
            var itemsData = new (long id, long pedido_id, long producto_id, int cantidad, decimal precio, decimal subtotal)[]
            {
                (1L, 1L, 1L, 1, 12000m, 12000m), // Pedido 1: Consulta
                (2L, 2L, 1L, 1, 12000m, 12000m), // Pedido 2: Consulta
                (3L, 2L, 2L, 1, 8500m, 8500m),   // Pedido 2: Vacuna
                (4L, 3L, 2L, 1, 8500m, 8500m),   // Pedido 3: Vacuna
                (5L, 3L, 3L, 1, 7200m, 7200m),   // Pedido 3: Pipeta
                (6L, 4L, 4L, 1, 45000m, 45000m), // Pedido 4: Alimento 15kg
                (7L, 5L, 5L, 1, 18000m, 18000m), // Pedido 5: Ecografía
                (8L, 6L, 6L, 2, 5400m, 10800m),  // Pedido 6: 2x Desparasitante
                (9L, 7L, 1L, 2, 12000m, 24000m), // Pedido 7: 2x Consulta
                (10L, 8L, 4L, 1, 45000m, 45000m) // Pedido 8: Alimento
            };

            var pedidoItems = new object[itemsData.Length, 9];
            for (int i = 0; i < itemsData.Length; i++)
            {
                pedidoItems[i, 0] = itemsData[i].id;
                pedidoItems[i, 1] = itemsData[i].pedido_id;
                pedidoItems[i, 2] = itemsData[i].producto_id;
                pedidoItems[i, 3] = itemsData[i].cantidad;
                pedidoItems[i, 4] = itemsData[i].precio;
                pedidoItems[i, 5] = itemsData[i].subtotal;
                pedidoItems[i, 6] = now;
                pedidoItems[i, 7] = now;
                pedidoItems[i, 8] = true;
            }

            migrationBuilder.InsertData(
                table: "pedido_items",
                columns: new[] { "id", "pedido_id", "producto_id", "cantidad", "precio_unitario", "subtotal", "creado_en", "actualizado_en", "activo" },
                values: pedidoItems
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM pedido_items WHERE id <= 8");
            migrationBuilder.Sql("DELETE FROM pedidos WHERE id <= 8");
            migrationBuilder.Sql("DELETE FROM clientes WHERE id <= 12");
        }
    }
}
