using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Veterinaria.Api.Auth;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;
using Veterinaria.Api.Domain.Enums;

namespace Veterinaria.Api.Controllers;

[Authorize(Roles = "ADMIN,VETERINARIO")]
[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly VeterinariaDbContext _context;

    public PedidosController(VeterinariaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPedidos(
        [FromQuery] string? busqueda,
        [FromQuery] EstadoPedido? estado,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        var paginaActual = pagina.GetValueOrDefault(1);
        var tamanoActual = tamano.GetValueOrDefault(10);

        if (paginaActual < 1) paginaActual = 1;
        if (tamanoActual < 1) tamanoActual = 10;

        var query = _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Items)
            .ThenInclude(i => i.Producto)
            .AsQueryable();

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(p => (p.Notas != null && p.Notas.Contains(busqueda)) || (p.Cliente != null && p.Cliente.Nombre.Contains(busqueda)));
        }
        
        var total = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(p => p.FechaPedido)
            .Skip((paginaActual - 1) * tamanoActual)
            .Take(tamanoActual)
            .ToListAsync();

        var hayMas = ((paginaActual - 1) * tamanoActual) + items.Count < total;

        var paginaResponse = new
        {
            pagina = paginaActual,
            tamano = tamanoActual,
            total,
            hay_mas = hayMas
        };

        return Ok(new
        {
            pedidos = items,
            items = items,
            pagina = paginaResponse
        });
    }

    [HttpPost]
    public async Task<IActionResult> CrearPedido([FromBody] CrearPedidoDto dto)
    {
        if (dto.ClienteId <= 0)
        {
            return BadRequest(new { codigo = "cliente_requerido", mensaje = "Debe especificar un cliente válido." });
        }

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId && c.Activo);
        if (cliente == null)
        {
            return NotFound(new { codigo = "cliente_no_encontrado", mensaje = "El cliente especificado no existe o está inactivo." });
        }

        var esEntrega = string.Equals(dto.Tipo, "entrega", StringComparison.OrdinalIgnoreCase);
        if (esEntrega && string.IsNullOrWhiteSpace(cliente.Direccion))
        {
            return BadRequest(new
            {
                codigo = "direccion_requerida",
                mensaje = "El cliente debe tener una dirección registrada para solicitar un pedido con entrega a domicilio."
            });
        }

        var usuarioCreadorId = User.UsuarioId();

        decimal total = 0;
        var itemsEntities = new List<PedidoItem>();

        if (dto.Items != null && dto.Items.Any())
        {
            var productoIds = dto.Items.Select(i => i.ProductoId).Distinct().ToList();
            var productos = await _context.Productos
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var itemDto in dto.Items)
            {
                if (itemDto.Cantidad <= 0) continue;
                if (!productos.TryGetValue(itemDto.ProductoId, out var prod))
                {
                    return BadRequest(new { codigo = "producto_no_encontrado", mensaje = $"Producto #{itemDto.ProductoId} no encontrado." });
                }

                var precioUnitario = itemDto.PrecioUnitario.HasValue && itemDto.PrecioUnitario > 0
                    ? itemDto.PrecioUnitario.Value
                    : prod.Precio;

                var subtotal = precioUnitario * itemDto.Cantidad;
                total += subtotal;

                itemsEntities.Add(new PedidoItem
                {
                    ProductoId = prod.Id,
                    Cantidad = itemDto.Cantidad,
                    PrecioUnitario = precioUnitario,
                    Subtotal = subtotal,
                    Activo = true,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                });
            }
        }

        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            Tipo = esEntrega ? "entrega" : "mostrador",
            UsuarioCreadorId = usuarioCreadorId,
            FechaPedido = DateTime.UtcNow,
            Estado = EstadoPedido.Confirmado,
            Notas = dto.Notas,
            Total = total,
            Items = itemsEntities,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        _context.Pedidos.Add(pedido);

        if (esEntrega)
        {
            var entrega = new Entrega
            {
                Pedido = pedido,
                DireccionLinea = cliente.Direccion,
                DireccionReferencia = null,
                DireccionLatitud = cliente.DireccionLatitud,
                DireccionLongitud = cliente.DireccionLongitud,
                Estado = EstadoEntrega.Pendiente,
                Activo = true,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };
            _context.Entregas.Add(entrega);
        }

        await _context.SaveChangesAsync();

        var codigoTexto = $"PED-{pedido.Id:D4}";

        return CreatedAtAction(nameof(GetPedidos), new { id = pedido.Id }, new
        {
            id = pedido.Id,
            codigo = codigoTexto,
            tipo = pedido.Tipo,
            cliente_id = pedido.ClienteId,
            cliente_nombre = cliente.Nombre,
            total = pedido.Total,
            estado = pedido.Estado.ToString().ToLowerInvariant(),
            mensaje = "Pedido registrado correctamente."
        });
    }

    [HttpPatch("{id}/estado")]
    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(long id, [FromBody] CambiarEstadoDto dto)
    {
        var nuevoEstado = dto?.ObtenerEstadoEnum();
        if (!nuevoEstado.HasValue)
        {
            return BadRequest(new
            {
                codigo = "estado_invalido",
                mensaje = "El estado especificado no es válido."
            });
        }

        var pedido = await _context.Pedidos
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null)
        {
            return NotFound(new
            {
                codigo = "pedido_no_encontrado",
                mensaje = "El pedido no fue encontrado."
            });
        }

        var estadoAnterior = pedido.Estado;
        pedido.Estado = nuevoEstado.Value;
        pedido.ActualizadoEn = DateTime.UtcNow;

        var usuarioActualId = User.UsuarioId();
        var destinatarioId = pedido.UsuarioCreadorId ?? usuarioActualId;

        if (destinatarioId.HasValue)
        {
            string nombreOperador = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombreOperador) && usuarioActualId.HasValue)
            {
                var op = await _context.Usuarios.FindAsync(usuarioActualId.Value);
                if (op != null && !string.IsNullOrWhiteSpace(op.Email))
                {
                    nombreOperador = op.Email;
                }
            }

            if (string.IsNullOrWhiteSpace(nombreOperador))
            {
                nombreOperador = "Un operador";
            }

            var notificacion = new Notificacion
            {
                UsuarioId = destinatarioId.Value,
                Titulo = $"Pedido PED-{pedido.Id:D4} actualizado",
                Mensaje = $"{nombreOperador} cambió el pedido a {nuevoEstado.Value}.",
                Tipo = TipoNotificacion.Pedido,
                Activo = true,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };
            _context.Notificaciones.Add(notificacion);
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            id = pedido.Id,
            estado = pedido.Estado.ToString().ToLowerInvariant(),
            mensaje = $"Estado actualizado a {nuevoEstado.Value}."
        });
    }

    [HttpGet("{id}/comprobante")]
    public async Task<IActionResult> ObtenerComprobantePdf(long id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Items)
            .ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
        {
            return NotFound(new
            {
                codigo = "pedido_no_encontrado",
                mensaje = "El pedido especificado no existe."
            });
        }

        // Validación estricta: Solo pedidos confirmados o superiores pueden imprimirse
        if (pedido.Estado == EstadoPedido.Borrador)
        {
            return Conflict(new
            {
                codigo = "pedido_no_confirmado",
                mensaje = "Solo se imprime el comprobante de un pedido confirmado o superior."
            });
        }

        var codigoTexto = $"PED-{id:D4}";
        var qrBytes = GenerarQrBytes(codigoTexto);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text("Veterinaria San Roque").Bold().FontSize(20).FontColor(Colors.Blue.Darken3);
                            titleCol.Item().Text("Comprobante de Pedido").FontSize(13).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(150).AlignRight().Column(metaCol =>
                        {
                            metaCol.Item().Text(codigoTexto).Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                            metaCol.Item().Text($"Fecha: {pedido.FechaPedido:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Cliente: {pedido.Cliente?.Nombre ?? "Cliente general"}").Bold().FontSize(11);
                            if (!string.IsNullOrWhiteSpace(pedido.Cliente?.Telefono))
                            {
                                c.Item().Text($"Teléfono: {pedido.Cliente.Telefono}").FontSize(9);
                            }
                            if (!string.IsNullOrWhiteSpace(pedido.Cliente?.Direccion))
                            {
                                c.Item().Text($"Dirección: {pedido.Cliente.Direccion}").FontSize(9);
                            }
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Estado: {pedido.Estado}").Bold().FontSize(11).FontColor(Colors.Blue.Darken1);
                        });
                    });

                    col.Item().PaddingTop(10);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);    // Producto
                            columns.RelativeColumn(1);    // Cantidad
                            columns.RelativeColumn(1.5f); // Precio Unitario
                            columns.RelativeColumn(1.5f); // Subtotal
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Producto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text("Cant.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("P. Unit.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Subtotal").Bold();
                        });

                        foreach (var item in pedido.Items)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(item.Producto?.Nombre ?? $"Producto #{item.ProductoId}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .AlignCenter().Text(item.Cantidad.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .AlignRight().Text($"${item.PrecioUnitario:N2}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .AlignRight().Text($"${item.Subtotal:N2}");
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Row(row =>
                    {
                        row.AutoItem().Text("Total General: ").FontSize(13).Bold();
                        row.AutoItem().Text($"${pedido.Total:N2}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                    });

                    if (!string.IsNullOrWhiteSpace(pedido.Notas))
                    {
                        col.Item().PaddingTop(12).Column(notCol =>
                        {
                            notCol.Item().Text("Notas / Observaciones:").Bold().FontSize(9);
                            notCol.Item().Text(pedido.Notas).Italic().FontSize(9);
                        });
                    }
                });

                page.Footer().Column(foot =>
                {
                    foot.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    foot.Item().PaddingTop(8).AlignCenter().Column(c =>
                    {
                        c.Item().AlignCenter().Width(80).Image(qrBytes);
                        c.Item().PaddingTop(4).AlignCenter().Text(codigoTexto).Bold().FontSize(11);
                        c.Item().AlignCenter().Text("Veterinaria San Roque - Sistema de Gestión").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"{codigoTexto}.pdf");
    }

    [AllowAnonymous]
    [HttpGet("{id}/qr")]
    public async Task<IActionResult> ObtenerQrPng(long id)
    {
        var existe = await _context.Pedidos.AnyAsync(p => p.Id == id);
        if (!existe)
        {
            return NotFound(new
            {
                codigo = "pedido_no_encontrado",
                mensaje = "El pedido especificado no existe."
            });
        }

        var codigoTexto = $"PED-{id:D4}";
        var qrBytes = GenerarQrBytes(codigoTexto);
        return File(qrBytes, "image/png", $"{codigoTexto}-qr.png");
    }

    private static byte[] GenerarQrBytes(string texto)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }
}

public class CambiarEstadoDto
{
    public JsonElement? Estado { get; set; }

    public EstadoPedido? ObtenerEstadoEnum()
    {
        if (!Estado.HasValue) return null;
        if (Estado.Value.ValueKind == JsonValueKind.Number && Estado.Value.TryGetInt32(out var intVal))
        {
            if (Enum.IsDefined(typeof(EstadoPedido), intVal))
                return (EstadoPedido)intVal;
        }
        if (Estado.Value.ValueKind == JsonValueKind.String)
        {
            var str = Estado.Value.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(str)) return null;
            var limpio = str.Replace("_", "");
            if (Enum.TryParse<EstadoPedido>(limpio, true, out var res))
                return res;
            if (Enum.TryParse<EstadoPedido>(str, true, out var res2))
                return res2;
        }
        return null;
    }
}

public class CrearPedidoDto
{
    public long ClienteId { get; set; }
    public string? Tipo { get; set; } = "mostrador";
    public string? Notas { get; set; }
    public List<CrearPedidoItemDto> Items { get; set; } = new();
}

public class CrearPedidoItemDto
{
    public long ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal? PrecioUnitario { get; set; }
}
