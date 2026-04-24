using DogGo.Data;
using DogGo.DTOs.Paseos;
using DogGo.Models;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Services
{
    public class PaseoService
    {
        private readonly DogGoDbContext _context;

        public PaseoService(DogGoDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, PaseoResponseDto? Data)> CrearAsync(int usuarioId, PaseoCreateRequestDto dto)
        {
            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == dto.PerroId && p.DueñoId == usuarioId);

            if (perro == null)
            {
                return (false, "El perro no existe o no pertenece al usuario.", null);
            }

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.Id == dto.PaseadorId);

            if (paseador == null)
            {
                return (false, "Paseador no encontrado.", null);
            }

            var paseo = new Paseo
            {
                PerroId = dto.PerroId,
                PaseadorId = dto.PaseadorId,
                DuracionMinutos = dto.DuracionMinutos,
                EsProgramado = dto.EsProgramado,
                FechaProgramada = dto.FechaProgramada,
                Precio = dto.Precio,
                Estado = "Pendiente",
                FechaInicio = dto.EsProgramado ? null : DateTime.UtcNow,
                LatitudActual = 0,
                LongitudActual = 0
            };

            _context.Paseos.Add(paseo);
            await _context.SaveChangesAsync();

            var response = new PaseoResponseDto
            {
                Id = paseo.Id,
                PerroId = paseo.PerroId,
                PaseadorId = paseo.PaseadorId,
                Estado = paseo.Estado,
                DuracionMinutos = paseo.DuracionMinutos,
                EsProgramado = paseo.EsProgramado,
                FechaProgramada = paseo.FechaProgramada,
                FechaInicio = paseo.FechaInicio,
                FechaFin = paseo.FechaFin,
                Precio = paseo.Precio
            };

            return (true, "Paseo creado correctamente.", response);
        }

        public async Task<List<PaseoResponseDto>> ObtenerMisPaseosAsync(int usuarioId, string rol)
        {
            IQueryable<Paseo> query = _context.Paseos;

            if (rol == "Paseador")
            {
                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null)
                {
                    return new List<PaseoResponseDto>();
                }

                query = query.Where(p => p.PaseadorId == paseador.Id);
            }
            else
            {
                query = query.Where(p => p.Perro.DueñoId == usuarioId);
            }

            return await query
                .OrderByDescending(p => p.Id)
                .Select(p => new PaseoResponseDto
                {
                    Id = p.Id,
                    PerroId = p.PerroId,
                    PaseadorId = p.PaseadorId,
                    Estado = p.Estado,
                    DuracionMinutos = p.DuracionMinutos,
                    EsProgramado = p.EsProgramado,
                    FechaProgramada = p.FechaProgramada,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    Precio = p.Precio
                })
                .ToListAsync();
        }

        public async Task<PaseoDetalleDto?> ObtenerPorIdAsync(int paseoId, int usuarioId, string rol)
        {
            var query = _context.Paseos
                .Include(p => p.Perro)
                    .ThenInclude(pe => pe.Dueño)
                .Include(p => p.Paseador)
                    .ThenInclude(pa => pa.Usuario)
                .AsQueryable();

            if (rol == "Paseador")
            {
                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null)
                {
                    return null;
                }

                query = query.Where(p => p.Id == paseoId && p.PaseadorId == paseador.Id);
            }
            else
            {
                query = query.Where(p => p.Id == paseoId && p.Perro.DueñoId == usuarioId);
            }

            var paseo = await query.FirstOrDefaultAsync();

            if (paseo == null)
            {
                return null;
            }

            return new PaseoDetalleDto
            {
                Id = paseo.Id,
                PerroId = paseo.PerroId,
                PerroNombre = paseo.Perro.Nombre,
                PaseadorId = paseo.PaseadorId,
                PaseadorNombre = $"{paseo.Paseador.Usuario.Nombre} {paseo.Paseador.Usuario.Apellido}",
                Estado = paseo.Estado,
                DuracionMinutos = paseo.DuracionMinutos,
                EsProgramado = paseo.EsProgramado,
                FechaProgramada = paseo.FechaProgramada,
                FechaInicio = paseo.FechaInicio,
                FechaFin = paseo.FechaFin,
                Precio = paseo.Precio,
                MotivoCancelacion = paseo.MotivoCancelacion,
                CanceladoPor = paseo.CanceladoPor,
                FechaCancelacion = paseo.FechaCancelacion,
                FotoInicioUrl = paseo.FotoInicioUrl,
                FotoFinUrl = paseo.FotoFinUrl
            };
        }

        public async Task<(bool Success, string Message)> AceptarAsync(int paseoId, int usuarioId)
        {
            var paseo = await ObtenerPaseoComoPaseadorAsync(paseoId, usuarioId);
            if (paseo == null) return (false, "Paseo no encontrado.");

            if (paseo.Estado != "Pendiente")
            {
                return (false, "Solo se pueden aceptar paseos pendientes.");
            }

            paseo.Estado = "Aceptado";
            await _context.SaveChangesAsync();

            return (true, "Paseo aceptado correctamente.");
        }

        public async Task<(bool Success, string Message)> RechazarAsync(int paseoId, int usuarioId)
        {
            var paseo = await ObtenerPaseoComoPaseadorAsync(paseoId, usuarioId);
            if (paseo == null) return (false, "Paseo no encontrado.");

            if (paseo.Estado != "Pendiente")
            {
                return (false, "Solo se pueden rechazar paseos pendientes.");
            }

            paseo.Estado = "Cancelado";
            paseo.CanceladoPor = "Paseador";
            paseo.FechaCancelacion = DateTime.UtcNow;
            paseo.MotivoCancelacion = "Rechazado por el paseador";

            await _context.SaveChangesAsync();

            return (true, "Paseo rechazado correctamente.");
        }

        public async Task<(bool Success, string Message)> IniciarAsync(int paseoId, int usuarioId)
        {
            var paseo = await ObtenerPaseoComoPaseadorAsync(paseoId, usuarioId);
            if (paseo == null) return (false, "Paseo no encontrado.");

            if (paseo.Estado != "Aceptado" && paseo.Estado != "Pendiente")
            {
                return (false, "Solo se pueden iniciar paseos aceptados o pendientes.");
            }

            paseo.Estado = "EnCurso";
            paseo.FechaInicio = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Paseo iniciado correctamente.");
        }

        public async Task<(bool Success, string Message)> FinalizarAsync(int paseoId, int usuarioId)
        {
            var paseo = await ObtenerPaseoComoPaseadorAsync(paseoId, usuarioId);
            if (paseo == null) return (false, "Paseo no encontrado.");

            if (paseo.Estado != "EnCurso")
            {
                return (false, "Solo se pueden finalizar paseos en curso.");
            }

            paseo.Estado = "Finalizado";
            paseo.FechaFin = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Paseo finalizado correctamente.");
        }

        public async Task<(bool Success, string Message)> CancelarAsync(int paseoId, int usuarioId, string rol, string? motivo = null)
        {
            var paseo = await _context.Paseos
                .Include(p => p.Perro)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.");
            }

            var tienePermiso = rol == "Paseador"
                ? await EsPaseadorDelPaseoAsync(paseo, usuarioId)
                : paseo.Perro.DueñoId == usuarioId;

            if (!tienePermiso)
            {
                return (false, "No tienes permiso para cancelar este paseo.");
            }

            if (paseo.Estado == "Finalizado" || paseo.Estado == "Cancelado")
            {
                return (false, "Ese paseo ya no se puede cancelar.");
            }

            paseo.Estado = "Cancelado";
            paseo.CanceladoPor = rol;
            paseo.FechaCancelacion = DateTime.UtcNow;
            paseo.MotivoCancelacion = motivo;

            await _context.SaveChangesAsync();

            return (true, "Paseo cancelado correctamente.");
        }

        private async Task<Paseo?> ObtenerPaseoComoPaseadorAsync(int paseoId, int usuarioId)
        {
            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador == null)
            {
                return null;
            }

            return await _context.Paseos
                .FirstOrDefaultAsync(p => p.Id == paseoId && p.PaseadorId == paseador.Id);
        }

        private async Task<bool> EsPaseadorDelPaseoAsync(Paseo paseo, int usuarioId)
        {
            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            return paseador != null && paseo.PaseadorId == paseador.Id;
        }
    }
}