namespace Veterinaria.Api.Domain.Dtos;

public record CategoriaDto(
    long Id,
    string Nombre,
    int Orden
);

public record ProductoDto(
    long Id,
    long CategoriaId,
    string Categoria,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    string? ImagenUrl,
    bool Disponible
);

public record ResumenContadoresDto(
    int Total,
    int Disponibles,
    int NoDisponibles
);

public record ProductosResumenDto(
    ResumenContadoresDto Resumen,
    List<CategoriaDto> Categorias,
    List<ProductoDto> Productos
);
