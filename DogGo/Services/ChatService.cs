using DogGo.Data;
using DogGo.DTOs.Chat;
using DogGo.Models;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Services
{
    public class ChatService
    {
        private readonly DogGoDbContext _context;

        public ChatService(DogGoDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, List<MensajeResponseDto> Data)> ObtenerMensajesAsync(
            int paseoId,
            int usuarioId,
            string rol)
        {
            var paseo = await QueryPaseoCompleto()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", new List<MensajeResponseDto>());
            }

            if (!await UsuarioPuedeAccederAlPaseoAsync(paseo, usuarioId, rol))
            {
                return (false, "No tienes permiso para ver este chat.", new List<MensajeResponseDto>());
            }

            var mensajes = await _context.Mensajes
                .Include(m => m.Emisor)
                .Include(m => m.Receptor)
                .Where(m => m.PaseoId == paseoId)
                .OrderBy(m => m.FechaEnvio)
                .ToListAsync();

            return (true, "Mensajes obtenidos correctamente.", mensajes.Select(m => MapMensaje(m, usuarioId)).ToList());
        }

        public async Task<(bool Success, string Message, MensajeResponseDto? Data)> EnviarMensajeAsync(
            int paseoId,
            int usuarioId,
            string rol,
            EnviarMensajeRequestDto dto)
        {
            if (dto == null)
            {
                return (false, "Datos inválidos.", null);
            }

            var contenido = dto.Contenido?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return (false, "El mensaje no puede estar vacío.", null);
            }

            if (contenido.Length > 1000)
            {
                return (false, "El mensaje no puede superar 1000 caracteres.", null);
            }

            var paseo = await QueryPaseoCompleto()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", null);
            }

            if (!await UsuarioPuedeAccederAlPaseoAsync(paseo, usuarioId, rol))
            {
                return (false, "No tienes permiso para enviar mensajes en este chat.", null);
            }

            var receptorId = ResolverReceptorId(paseo, usuarioId, rol, dto.ReceptorId);

            if (receptorId == null)
            {
                return (false, "No se pudo determinar el receptor del mensaje.", null);
            }

            if (receptorId.Value == usuarioId)
            {
                return (false, "No puedes enviarte un mensaje a ti mismo.", null);
            }

            var receptorExiste = await _context.Usuarios.AnyAsync(u => u.Id == receptorId.Value);

            if (!receptorExiste)
            {
                return (false, "El receptor no existe.", null);
            }

            var mensaje = new Mensaje
            {
                PaseoId = paseo.Id,
                EmisorId = usuarioId,
                ReceptorId = receptorId.Value,
                Contenido = contenido,
                FechaEnvio = DateTime.UtcNow,
                Leido = false
            };

            _context.Mensajes.Add(mensaje);
            await _context.SaveChangesAsync();

            var guardado = await _context.Mensajes
                .Include(m => m.Emisor)
                .Include(m => m.Receptor)
                .FirstAsync(m => m.Id == mensaje.Id);

            return (true, "Mensaje enviado correctamente.", MapMensaje(guardado, usuarioId));
        }

        public async Task<(bool Success, string Message)> MarcarComoLeidosAsync(
            int paseoId,
            int usuarioId,
            string rol)
        {
            var paseo = await QueryPaseoCompleto()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.");
            }

            if (!await UsuarioPuedeAccederAlPaseoAsync(paseo, usuarioId, rol))
            {
                return (false, "No tienes permiso para modificar este chat.");
            }

            var mensajes = await _context.Mensajes
                .Where(m => m.PaseoId == paseoId && m.ReceptorId == usuarioId && !m.Leido)
                .ToListAsync();

            foreach (var mensaje in mensajes)
            {
                mensaje.Leido = true;
            }

            await _context.SaveChangesAsync();

            return (true, "Mensajes marcados como leídos.");
        }

        public async Task<bool> PuedeUnirseAlPaseoAsync(int paseoId, int usuarioId, string rol)
        {
            var paseo = await QueryPaseoCompleto()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return false;
            }

            return await UsuarioPuedeAccederAlPaseoAsync(paseo, usuarioId, rol);
        }

        private IQueryable<Paseo> QueryPaseoCompleto()
        {
            return _context.Paseos
                .Include(p => p.Perro)
                    .ThenInclude(perro => perro.Dueño)
                .Include(p => p.Paseador)
                    .ThenInclude(paseador => paseador.Usuario);
        }

        private async Task<bool> UsuarioPuedeAccederAlPaseoAsync(Paseo paseo, int usuarioId, string rol)
        {
            var rolNormalizado = NormalizarRol(rol);

            if (rolNormalizado == "Admin")
            {
                return true;
            }

            if (paseo.Perro.DueñoId == usuarioId)
            {
                return true;
            }

            if (paseo.Paseador.UsuarioId == usuarioId)
            {
                return true;
            }

            if (rolNormalizado == "Paseador")
            {
                return await _context.Paseadores
                    .AnyAsync(p => p.UsuarioId == usuarioId && p.Id == paseo.PaseadorId);
            }

            return false;
        }

        private int? ResolverReceptorId(Paseo paseo, int usuarioId, string rol, int? receptorIdSolicitado)
        {
            var duenioId = paseo.Perro.DueñoId;
            var paseadorUsuarioId = paseo.Paseador.UsuarioId;
            var rolNormalizado = NormalizarRol(rol);

            if (rolNormalizado == "Admin" && receptorIdSolicitado.HasValue)
            {
                return receptorIdSolicitado.Value;
            }

            if (usuarioId == duenioId)
            {
                return paseadorUsuarioId;
            }

            if (usuarioId == paseadorUsuarioId)
            {
                return duenioId;
            }

            if (receptorIdSolicitado.HasValue &&
                (receptorIdSolicitado.Value == duenioId || receptorIdSolicitado.Value == paseadorUsuarioId))
            {
                return receptorIdSolicitado.Value;
            }

            return null;
        }

        private static MensajeResponseDto MapMensaje(Mensaje mensaje, int usuarioActualId)
        {
            var emisorNombreCompleto = $"{mensaje.Emisor.Nombre} {mensaje.Emisor.Apellido}".Trim();
            var receptorNombreCompleto = $"{mensaje.Receptor.Nombre} {mensaje.Receptor.Apellido}".Trim();

            return new MensajeResponseDto
            {
                Id = mensaje.Id,
                PaseoId = mensaje.PaseoId,

                EmisorId = mensaje.EmisorId,
                EmisorNombre = mensaje.Emisor.Nombre,
                EmisorApellido = mensaje.Emisor.Apellido,
                EmisorNombreCompleto = emisorNombreCompleto,
                EmisorRol = mensaje.Emisor.Rol,

                ReceptorId = mensaje.ReceptorId,
                ReceptorNombre = mensaje.Receptor.Nombre,
                ReceptorApellido = mensaje.Receptor.Apellido,
                ReceptorNombreCompleto = receptorNombreCompleto,
                ReceptorRol = mensaje.Receptor.Rol,

                Contenido = mensaje.Contenido,
                FechaEnvio = mensaje.FechaEnvio,
                Leido = mensaje.Leido,
                EsMio = mensaje.EmisorId == usuarioActualId
            };
        }

        private static string NormalizarRol(string? rol)
        {
            var r = (rol ?? string.Empty).Trim();

            if (r.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
            if (r.Equals("Paseador", StringComparison.OrdinalIgnoreCase)) return "Paseador";

            if (r.Equals("Dueño", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Duenio", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Dueno", StringComparison.OrdinalIgnoreCase))
            {
                return "Duenio";
            }

            return r;
        }
    }
}


