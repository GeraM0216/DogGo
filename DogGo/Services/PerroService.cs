using DogGo.Data;
using DogGo.DTOs.Perros;
using DogGo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Services
{
    public class PerroService
    {
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PerroService(DogGoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<PerroResponseDto>> ObtenerMisPerrosAsync(int usuarioId)
        {
            return await _context.Perros
                .Where(p => p.DueñoId == usuarioId)
                .OrderByDescending(p => p.Id)
                .Select(p => MapResponse(p))
                .ToListAsync();
        }

        public async Task<PerroResponseDto?> ObtenerPorIdAsync(int perroId, int usuarioId)
        {
            return await _context.Perros
                .Where(p => p.Id == perroId && p.DueñoId == usuarioId)
                .Select(p => MapResponse(p))
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, PerroResponseDto? Data)> CrearAsync(
            int usuarioId,
            PerroCreateRequestDto dto)
        {
            var validacion = ValidarDatosPerro(
                dto.Nombre,
                dto.Raza,
                dto.Edad,
                PrimerTexto(dto.Tamaño, dto.Tamano, dto.Tamanio),
                PrimerTexto(dto.Notas, dto.Observaciones),
                PrimerTexto(dto.ImagenUrl, dto.FotoUrl));

            if (!validacion.Success)
            {
                return (false, validacion.Message, null);
            }

            var imagenUrl = PrimerTexto(dto.ImagenUrl, dto.FotoUrl);

            if (EsBase64(imagenUrl))
            {
                return (false, "No se permite guardar imágenes en base64. Sube la foto como archivo.", null);
            }

            var perro = new Perro
            {
                Nombre = dto.Nombre.Trim(),
                Raza = TextoONull(dto.Raza),
                Edad = dto.Edad,
                Tamaño = NormalizarTamano(PrimerTexto(dto.Tamaño, dto.Tamano, dto.Tamanio)),
                Notas = PrimerTexto(dto.Notas, dto.Observaciones),
                ImagenUrl = TextoONull(imagenUrl),
                DueñoId = usuarioId
            };

            _context.Perros.Add(perro);
            await _context.SaveChangesAsync();

            return (true, "Perro creado correctamente.", MapResponse(perro));
        }

        public async Task<(bool Success, string Message, PerroResponseDto? Data)> ActualizarAsync(
            int perroId,
            int usuarioId,
            PerroUpdateRequestDto dto)
        {
            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == perroId && p.DueñoId == usuarioId);

            if (perro == null)
            {
                return (false, "Perro no encontrado.", null);
            }

            var validacion = ValidarDatosPerro(
                dto.Nombre,
                dto.Raza,
                dto.Edad,
                PrimerTexto(dto.Tamaño, dto.Tamano, dto.Tamanio),
                PrimerTexto(dto.Notas, dto.Observaciones),
                PrimerTexto(dto.ImagenUrl, dto.FotoUrl));

            if (!validacion.Success)
            {
                return (false, validacion.Message, null);
            }

            var imagenUrl = PrimerTexto(dto.ImagenUrl, dto.FotoUrl);

            if (EsBase64(imagenUrl))
            {
                return (false, "No se permite guardar imágenes en base64. Sube la foto como archivo.", null);
            }

            perro.Nombre = dto.Nombre.Trim();
            perro.Raza = TextoONull(dto.Raza);
            perro.Edad = dto.Edad;
            perro.Tamaño = NormalizarTamano(PrimerTexto(dto.Tamaño, dto.Tamano, dto.Tamanio));
            perro.Notas = PrimerTexto(dto.Notas, dto.Observaciones);

            if (!string.IsNullOrWhiteSpace(imagenUrl))
            {
                perro.ImagenUrl = imagenUrl.Trim();
            }

            await _context.SaveChangesAsync();

            return (true, "Perro actualizado correctamente.", MapResponse(perro));
        }

        public async Task<(bool Success, string Message)> EliminarAsync(int perroId, int usuarioId)
        {
            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == perroId && p.DueñoId == usuarioId);

            if (perro == null)
            {
                return (false, "Perro no encontrado.");
            }

            var tienePaseos = await _context.Paseos.AnyAsync(p => p.PerroId == perroId)
                || await _context.PaseoPerros.AnyAsync(pp => pp.PerroId == perroId);

            if (tienePaseos)
            {
                return (false, "No se puede eliminar este perro porque tiene paseos asociados.");
            }

            _context.Perros.Remove(perro);
            await _context.SaveChangesAsync();

            return (true, "Perro eliminado correctamente.");
        }

        public async Task<(bool Success, string Message, PerroResponseDto? Data)> SubirFotoAsync(
            int perroId,
            int usuarioId,
            IFormFile archivo)
        {
            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == perroId && p.DueñoId == usuarioId);

            if (perro == null)
            {
                return (false, "Perro no encontrado.", null);
            }

            if (archivo == null || archivo.Length <= 0)
            {
                return (false, "No se recibió una imagen válida.", null);
            }

            if (archivo.Length > 5_000_000)
            {
                return (false, "La imagen no puede superar 5 MB.", null);
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!extensionesPermitidas.Contains(extension))
            {
                return (false, "Formato no permitido. Usa jpg, jpeg, png o webp.", null);
            }

            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var carpeta = Path.Combine(webRoot, "uploads", "perros");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"perro-{perroId}-{Guid.NewGuid():N}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            await using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            var rutaPublica = $"/uploads/perros/{nombreArchivo}";

            perro.ImagenUrl = rutaPublica;
            await _context.SaveChangesAsync();

            return (true, "Foto del perro actualizada correctamente.", MapResponse(perro));
        }

        private static PerroResponseDto MapResponse(Perro perro)
        {
            return new PerroResponseDto
            {
                Id = perro.Id,
                Nombre = perro.Nombre,
                Raza = perro.Raza ?? string.Empty,
                Edad = perro.Edad,
                Tamano = perro.Tamaño,
                Tamanio = perro.Tamaño,
                Tamaño = perro.Tamaño,
                Peso = null,
                Notas = perro.Notas,
                FotoUrl = perro.ImagenUrl,
                ImagenUrl = perro.ImagenUrl
            };
        }

        private static (bool Success, string Message) ValidarDatosPerro(
            string? nombre,
            string? raza,
            int edad,
            string? tamano,
            string? notas,
            string? imagenUrl)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return (false, "El nombre del perro es obligatorio.");
            if (nombre.Trim().Length > 50) return (false, "El nombre no puede superar 50 caracteres.");
            if (!string.IsNullOrWhiteSpace(raza) && raza.Trim().Length > 50) return (false, "La raza no puede superar 50 caracteres.");
            if (edad < 0 || edad > 30) return (false, "La edad debe estar entre 0 y 30 años.");
            if (!string.IsNullOrWhiteSpace(tamano) && tamano.Trim().Length > 20) return (false, "El tamaño no puede superar 20 caracteres.");
            if (!string.IsNullOrWhiteSpace(notas) && notas.Trim().Length > 300) return (false, "Las notas no pueden superar 300 caracteres.");
            if (!string.IsNullOrWhiteSpace(imagenUrl) && imagenUrl.Trim().Length > 500) return (false, "La URL de imagen no puede superar 500 caracteres.");
            return (true, "OK");
        }

        private static string? TextoONull(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            return texto.Trim();
        }

        private static string? PrimerTexto(params string?[] valores)
        {
            foreach (var valor in valores)
            {
                if (!string.IsNullOrWhiteSpace(valor)) return valor.Trim();
            }

            return null;
        }

        private static string? NormalizarTamano(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var texto = valor.Trim();

            if (texto.Equals("Pequeño", StringComparison.OrdinalIgnoreCase) ||
                texto.Equals("Pequeno", StringComparison.OrdinalIgnoreCase))
            {
                return "Pequeño";
            }

            if (texto.Equals("Mediano", StringComparison.OrdinalIgnoreCase)) return "Mediano";
            if (texto.Equals("Grande", StringComparison.OrdinalIgnoreCase)) return "Grande";

            return texto;
        }

        private static bool EsBase64(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return false;

            var texto = valor.Trim();

            return texto.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
                || texto.Length > 2000;
        }
    }
}


