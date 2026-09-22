using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using Veterinaria.Api.Domain.Entities;

#nullable disable

namespace Veterinaria.Api.Migrations
{
    /// <inheritdoc />
    public partial class DatosDeIdentidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

            // Sembrar Roles
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "nombre", "codigo", "creado_en", "actualizado_en", "activo" },
                values: new object[,]
                {
                    { 1L, "Administrador", "ADMIN", now, now, true },
                    { 2L, "Veterinario", "VETERINARIO", now, now, true }
                }
            );

            // Hashear contraseñas usando PasswordHasher<Usuario>
            var hasher = new PasswordHasher<Usuario>();
            var uAdmin = new Usuario { Id = 1L, Email = "admin@veterinaria.local" };
            var uVet = new Usuario { Id = 2L, Email = "veterinario@veterinaria.local" };
            var uSinRol = new Usuario { Id = 3L, Email = "sinrol@veterinaria.local" };

            var hashAdmin = hasher.HashPassword(uAdmin, "Admin123!");
            var hashVet = hasher.HashPassword(uVet, "Vet123!");
            var hashSinRol = hasher.HashPassword(uSinRol, "User123!");

            // Sembrar Usuarios
            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "email", "password_hash", "rol_id", "creado_en", "actualizado_en", "activo" },
                values: new object[,]
                {
                    { 1L, "admin@veterinaria.local", hashAdmin, 1L, now, now, true },
                    { 2L, "veterinario@veterinaria.local", hashVet, 2L, now, now, true },
                    { 3L, "sinrol@veterinaria.local", hashSinRol, null!, now, now, true }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM usuarios WHERE email IN ('admin@veterinaria.local', 'veterinario@veterinaria.local', 'sinrol@veterinaria.local')");
            migrationBuilder.Sql("DELETE FROM roles WHERE codigo IN ('ADMIN', 'VETERINARIO')");
        }
    }
}
