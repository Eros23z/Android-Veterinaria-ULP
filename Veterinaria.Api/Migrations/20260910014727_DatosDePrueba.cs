using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class DatosDePrueba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "categorias",
                columns: new[] { "id", "nombre", "orden", "creado_en", "actualizado_en", "activo" },
                values: new object[,]
                {
                    { 1L, "Consultas y Atención", 10, fecha, fecha, true },
                    { 2L, "Vacunación e Inmunización", 20, fecha, fecha, true },
                    { 3L, "Farmacia Veterinaria", 30, fecha, fecha, true },
                    { 4L, "Nutrición y Alimentos", 40, fecha, fecha, true },
                    { 5L, "Estudios y Diagnóstico", 50, fecha, fecha, true }
                });

            migrationBuilder.InsertData(
                table: "productos",
                columns: new[] { "id", "categoria_id", "nombre", "descripcion", "precio", "imagen_url", "disponible", "creado_en", "actualizado_en", "activo" },
                values: new object[,]
                {
                    {
                        1L,
                        1L,
                        "Consulta Veterinaria General",
                        "Revisión clínica completa, control de constantes y diagnóstico inicial",
                        12000.00m,
                        "/seed/consulta.svg",
                        true,
                        fecha,
                        fecha,
                        true
                    },
                    {
                        2L,
                        2L,
                        "Vacuna Antirrábica Canina/Felina",
                        "Dosis anual obligatoria con certificado oficial de inmunización",
                        8500.00m,
                        "/seed/vacunacion.svg",
                        true,
                        fecha,
                        fecha,
                        true
                    },
                    {
                        3L,
                        3L,
                        "Pipeta Antipulgas y Garrapatas (10-20kg)",
                        "Protección dérmica mensual contra parásitos externos",
                        7200.00m,
                        "/seed/farmacia.svg",
                        true,
                        fecha,
                        fecha,
                        true
                    },
                    {
                        4L,
                        4L,
                        "Alimento Balanceado Premium Perro Adulto 15kg",
                        "Fórmula con proteínas de alta digestibilidad y omegas 3 y 6",
                        45000.00m,
                        "/seed/nutricion.svg",
                        false,
                        fecha,
                        fecha,
                        true
                    },
                    {
                        5L,
                        5L,
                        "Ecografía Abdominal Diagnóstica",
                        "Estudio de imagenología de alta resolución para órganos internos",
                        18000.00m,
                        "/seed/estudios.svg",
                        true,
                        fecha,
                        fecha,
                        true
                    },
                    {
                        6L,
                        3L,
                        "Desparasitante Interno Oral Felino",
                        "Comprimido palatable de amplio espectro para nemátodos y cestodos",
                        5400.00m,
                        "/seed/farmacia.svg",
                        true,
                        fecha,
                        fecha,
                        true
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "productos",
                keyColumn: "id",
                keyValues: new object[] { 1L, 2L, 3L, 4L, 5L, 6L });

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValues: new object[] { 1L, 2L, 3L, 4L, 5L });
        }
    }
}
