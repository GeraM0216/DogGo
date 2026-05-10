using DogGo.Data;
using DogGo.DTOs.Paseos;
using DogGo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Services
{
    public class PaseoService
    {
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PaseoService(DogGoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<(bool Success, string Message, PaseoResponseDto? Data)> CrearAsync(int usuarioId, PaseoCreateRequestDto dto)
        {
            var validacion = ValidarCrearPaseo(dto);
            if (!validacion.Success)
            {
                return (false, validacion.Message, null);
            }

            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == dto.PerroId && p.DueñoId == usuarioId);

            if (perro == null)
            {
                return (false, "El perro no existe o no pertenece al usuario.", null);
            }

            var paseador = await _context.Paseadores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == dto.PaseadorId);

            if (paseador == null)
            {
                return (false, "Paseador no encontrado.", null);
            }

            var duracion = dto.DuracionMinutos > 0 ? dto.DuracionMinutos : 30;

            var precio = dto.Precio > 0
                ? dto.Precio
                : Math.Round(paseador.TarifaPorHora * duracion / 60m, 2);

            var esProgramado = dto.EsProgramado || dto.FechaProgramada.HasValue;

            var paseo = new Paseo
            {
                PerroId = dto.PerroId,
                PaseadorId = dto.PaseadorId,
                DuracionMinutos = duracion,
                EsProgramado = esProgramado,
                FechaProgramada = dto.FechaProgramada,
                Precio = precio,
                Estado = "Pendiente",
                FechaInicio = null,
                LatitudActual = 0,
                LongitudActual = 0,

                DireccionRecogida = PrimerTexto(dto.DireccionRecogida, dto.UbicacionTexto, dto.Direccion),
                ReferenciasRecogida = PrimerTexto(dto.ReferenciasRecogida, dto.Referencias, dto.Notas, dto.Observaciones),
                ZonaRecogida = TextoONull(dto.ZonaRecogida),
                LatitudRecogida = dto.LatitudRecogida,
                LongitudRecogida = dto.LongitudRecogida
            };

            _context.Paseos.Add(paseo);
            await _context.SaveChangesAsync();

            var creado = await QueryPaseosCompletos()
                .FirstAsync(p => p.Id == paseo.Id);

            return (true, "Paseo creado correctamente.", MapResponse(creado));
        }

        public async Task<List<PaseoResponseDto>> ObtenerMisPaseosAsync(int usuarioId, string rol)
        {
            var rolNormalizado = NormalizarRol(rol);
            IQueryable<Paseo> query = QueryPaseosCompletos();

            if (rolNormalizado == "Paseador")
            {
                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null)
                {
                    return new List<PaseoResponseDto>();
                }

                query = query.Where(p => p.PaseadorId == paseador.Id);
            }
            else if (rolNormalizado == "Admin")
            {
                // Admin puede ver todo.
            }
            else
            {
                query = query.Where(p => p.Perro.DueñoId == usuarioId);
            }

            var paseos = await query
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return paseos.Select(MapResponse).ToList();
        }

        public async Task<PaseoDetalleDto?> ObtenerPorIdAsync(int paseoId, int usuarioId, string rol)
        {
            var paseo = await QueryPaseosCompletos()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return null;
            }

            if (!await UsuarioPuedeVerPaseoAsync(paseo, usuarioId, rol))
            {
                return null;
            }

            return MapDetalle(paseo);
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

            if (string.IsNullOrWhiteSpace(paseo.FotoFinUrl))
            {
                return (false, "Para finalizar el paseo primero debes subir la foto de fin.");
            }

            paseo.Estado = "Finalizado";
            paseo.FechaFin = DateTime.UtcNow;

            if (paseo.FechaInicio.HasValue)
            {
                var minutos = (int)Math.Max(0, (paseo.FechaFin.Value - paseo.FechaInicio.Value).TotalMinutes);
                paseo.DuracionRealMinutos = Math.Min(minutos, 1440);
            }

            await _context.SaveChangesAsync();

            return (true, "Paseo finalizado correctamente.");
        }

        public async Task<(bool Success, string Message)> CancelarAsync(int paseoId, int usuarioId, string rol, string? motivo = null)
        {
            var paseo = await QueryPaseosCompletos()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.");
            }

            if (!await UsuarioPuedeVerPaseoAsync(paseo, usuarioId, rol))
            {
                return (false, "No tienes permiso para cancelar este paseo.");
            }

            if (paseo.Estado == "Finalizado" || paseo.Estado == "Cancelado")
            {
                return (false, "Ese paseo ya no se puede cancelar.");
            }

            var motivoLimpio = string.IsNullOrWhiteSpace(motivo)
                ? "Cancelado desde la app móvil"
                : motivo.Trim();

            if (motivoLimpio.Length > 300)
            {
                return (false, "El motivo de cancelación no puede superar 300 caracteres.");
            }

            paseo.Estado = "Cancelado";
            paseo.CanceladoPor = NormalizarRol(rol);
            paseo.FechaCancelacion = DateTime.UtcNow;
            paseo.MotivoCancelacion = motivoLimpio;

            await _context.SaveChangesAsync();

            return (true, "Paseo cancelado correctamente.");
        }

        public async Task<(bool Success, string Message, PaseoDetalleDto? Data)> SubirFotoAsync(
            int paseoId,
            int usuarioId,
            string rol,
            string tipo,
            IFormFile archivo)
        {
            var paseo = await QueryPaseosCompletos()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", null);
            }

            if (NormalizarRol(rol) != "Admin" && !await EsPaseadorDelPaseoAsync(paseo, usuarioId))
            {
                return (false, "Solo el paseador asignado puede subir evidencias.", null);
            }

            if (paseo.Estado == "Cancelado")
            {
                return (false, "No puedes subir evidencias de un paseo cancelado.", null);
            }

            var tipoNormalizado = tipo.Trim().ToLowerInvariant();

            if (tipoNormalizado != "inicio" && tipoNormalizado != "fin")
            {
                return (false, "Tipo de evidencia inválido.", null);
            }

            if (tipoNormalizado == "inicio" && paseo.Estado != "EnCurso")
            {
                return (false, "La foto de inicio solo se puede subir cuando el paseo está en curso.", null);
            }

            if (tipoNormalizado == "fin" && paseo.Estado != "EnCurso")
            {
                return (false, "La foto de fin solo se puede subir cuando el paseo está en curso.", null);
            }

            if (archivo == null || archivo.Length <= 0)
            {
                return (false, "El archivo está vacío.", null);
            }

            if (archivo.Length > 5_000_000)
            {
                return (false, "La imagen no puede superar 5 MB.", null);
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!extensionesPermitidas.Contains(extension))
            {
                return (false, "Formato de imagen no permitido. Usa jpg, jpeg, png o webp.", null);
            }

            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var carpeta = Path.Combine(webRoot, "uploads", "evidencias");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"paseo-{paseoId}-{tipoNormalizado}-{Guid.NewGuid():N}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            await using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            var urlPublica = $"/uploads/evidencias/{nombreArchivo}";

            if (tipoNormalizado == "inicio")
            {
                paseo.FotoInicioUrl = urlPublica;
            }
            else
            {
                paseo.FotoFinUrl = urlPublica;
            }

            await _context.SaveChangesAsync();

            var actualizado = await QueryPaseosCompletos()
                .FirstAsync(p => p.Id == paseoId);

            return (true, "Evidencia subida correctamente.", MapDetalle(actualizado));
        }

        public async Task<(bool Success, string Message, object? Data)> EnviarUbicacionAsync(
            int paseoId,
            int usuarioId,
            string rol,
            decimal latitud,
            decimal longitud)
        {
            var paseo = await QueryPaseosCompletos()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", null);
            }

            if (NormalizarRol(rol) != "Admin" && !await EsPaseadorDelPaseoAsync(paseo, usuarioId))
            {
                return (false, "Solo el paseador asignado puede enviar ubicación.", null);
            }

            if (paseo.Estado != "EnCurso")
            {
                return (false, "Solo se puede enviar ubicación de paseos en curso.", null);
            }

            if (latitud < -90 || latitud > 90 || longitud < -180 || longitud > 180)
            {
                return (false, "Coordenadas inválidas.", null);
            }

            paseo.LatitudActual = latitud;
            paseo.LongitudActual = longitud;

            var ubicacion = new Ubicacion
            {
                PaseoId = paseo.Id,
                Latitud = latitud,
                Longitud = longitud,
                Timestamp = DateTime.UtcNow
            };

            _context.Ubicaciones.Add(ubicacion);
            await _context.SaveChangesAsync();

            return (true, "Ubicación enviada correctamente.", new
            {
                paseoId = paseo.Id,
                latitud = ubicacion.Latitud,
                longitud = ubicacion.Longitud,
                timestamp = ubicacion.Timestamp,
                fecha = ubicacion.Timestamp,
                latitudActual = paseo.LatitudActual,
                longitudActual = paseo.LongitudActual
            });
        }

        public async Task<(bool Success, string Message, object? Data)> ObtenerUltimaUbicacionAsync(
            int paseoId,
            int usuarioId,
            string rol)
        {
            var paseo = await QueryPaseosCompletos()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", null);
            }

            if (!await UsuarioPuedeVerPaseoAsync(paseo, usuarioId, rol))
            {
                return (false, "No tienes permiso para ver la ubicación de este paseo.", null);
            }

            var ubicacion = await _context.Ubicaciones
                .Where(u => u.PaseoId == paseoId)
                .OrderByDescending(u => u.Timestamp)
                .FirstOrDefaultAsync();

            if (ubicacion != null)
            {
                return (true, "Última ubicación obtenida.", new
                {
                    paseoId = paseo.Id,
                    latitud = ubicacion.Latitud,
                    longitud = ubicacion.Longitud,
                    latitudActual = ubicacion.Latitud,
                    longitudActual = ubicacion.Longitud,
                    timestamp = ubicacion.Timestamp,
                    fecha = ubicacion.Timestamp
                });
            }

            if (paseo.LatitudActual != 0 || paseo.LongitudActual != 0)
            {
                return (true, "Última ubicación obtenida.", new
                {
                    paseoId = paseo.Id,
                    latitud = paseo.LatitudActual,
                    longitud = paseo.LongitudActual,
                    latitudActual = paseo.LatitudActual,
                    longitudActual = paseo.LongitudActual,
                    timestamp = paseo.FechaInicio,
                    fecha = paseo.FechaInicio
                });
            }

            return (true, "Todavía no hay ubicación GPS registrada.", new
            {
                paseoId = paseo.Id,
                latitud = (decimal?)null,
                longitud = (decimal?)null,
                latitudActual = (decimal?)null,
                longitudActual = (decimal?)null,
                timestamp = (DateTime?)null,
                fecha = (DateTime?)null
            });
        }

        public async Task<(bool Success, string Message, object? Data)> ObtenerHistorialUbicacionesAsync(
            int paseoId,
            int usuarioId,
            string rol)
        {
            var paseo = await QueryPaseosCompletos()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", null);
            }

            if (!await UsuarioPuedeVerPaseoAsync(paseo, usuarioId, rol))
            {
                return (false, "No tienes permiso para ver la ruta de este paseo.", null);
            }

            var ubicaciones = await _context.Ubicaciones
                .Where(u => u.PaseoId == paseoId)
                .OrderBy(u => u.Timestamp)
                .Select(u => new
                {
                    id = u.Id,
                    paseoId = u.PaseoId,
                    latitud = u.Latitud,
                    longitud = u.Longitud,
                    latitudActual = u.Latitud,
                    longitudActual = u.Longitud,
                    timestamp = u.Timestamp,
                    fecha = u.Timestamp
                })
                .ToListAsync();

            if (ubicaciones.Count > 0)
            {
                return (true, "Historial de ubicaciones obtenido.", ubicaciones);
            }

            if (paseo.LatitudActual != 0 || paseo.LongitudActual != 0)
            {
                var fallback = new[]
                {
                    new
                    {
                        id = 0,
                        paseoId = paseo.Id,
                        latitud = paseo.LatitudActual,
                        longitud = paseo.LongitudActual,
                        latitudActual = paseo.LatitudActual,
                        longitudActual = paseo.LongitudActual,
                        timestamp = paseo.FechaInicio,
                        fecha = paseo.FechaInicio
                    }
                };

                return (true, "Historial de ubicaciones obtenido.", fallback);
            }

            return (true, "Todavía no hay ubicaciones registradas para este paseo.", new List<object>());
        }

        private IQueryable<Paseo> QueryPaseosCompletos()
        {
            return _context.Paseos
                .Include(p => p.Perro)
                    .ThenInclude(pe => pe.Dueño)
                .Include(p => p.Paseador)
                    .ThenInclude(pa => pa.Usuario);
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

        private async Task<bool> UsuarioPuedeVerPaseoAsync(Paseo paseo, int usuarioId, string rol)
        {
            var rolNormalizado = NormalizarRol(rol);

            if (rolNormalizado == "Admin")
            {
                return true;
            }

            if (rolNormalizado == "Paseador")
            {
                return await EsPaseadorDelPaseoAsync(paseo, usuarioId);
            }

            return paseo.Perro.DueñoId == usuarioId;
        }

        private static (bool Success, string Message) ValidarCrearPaseo(PaseoCreateRequestDto dto)
        {
            if (dto == null)
            {
                return (false, "Datos inválidos.");
            }

            if (dto.PerroId <= 0)
            {
                return (false, "Selecciona un perro válido.");
            }

            if (dto.PaseadorId <= 0)
            {
                return (false, "Selecciona un paseador válido.");
            }

            if (dto.DuracionMinutos <= 0 || dto.DuracionMinutos > 1440)
            {
                return (false, "La duración debe estar entre 1 y 1440 minutos.");
            }

            if (dto.Precio < 0 || dto.Precio > 100000)
            {
                return (false, "El precio debe estar entre 0 y 100000.");
            }

            if (dto.LatitudRecogida.HasValue && (dto.LatitudRecogida.Value < -90 || dto.LatitudRecogida.Value > 90))
            {
                return (false, "La latitud de recogida es inválida.");
            }

            if (dto.LongitudRecogida.HasValue && (dto.LongitudRecogida.Value < -180 || dto.LongitudRecogida.Value > 180))
            {
                return (false, "La longitud de recogida es inválida.");
            }

            if (LargoMayor(dto.UbicacionTexto, 200) ||
                LargoMayor(dto.DireccionRecogida, 200) ||
                LargoMayor(dto.Direccion, 200))
            {
                return (false, "La dirección de recogida no puede superar 200 caracteres.");
            }

            if (LargoMayor(dto.ReferenciasRecogida, 300) ||
                LargoMayor(dto.Referencias, 300) ||
                LargoMayor(dto.Notas, 300) ||
                LargoMayor(dto.Observaciones, 300))
            {
                return (false, "Las referencias o notas no pueden superar 300 caracteres.");
            }

            if (LargoMayor(dto.ZonaRecogida, 100))
            {
                return (false, "La zona de recogida no puede superar 100 caracteres.");
            }

            return (true, "OK");
        }

        private static bool LargoMayor(string? texto, int max)
        {
            return !string.IsNullOrWhiteSpace(texto) && texto.Trim().Length > max;
        }

        private static string NormalizarRol(string? rol)
        {
            var r = (rol ?? string.Empty).Trim();

            if (r.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            if (r.Equals("Paseador", StringComparison.OrdinalIgnoreCase))
            {
                return "Paseador";
            }

            if (r.Equals("Dueño", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Duenio", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Dueno", StringComparison.OrdinalIgnoreCase))
            {
                return "Dueño";
            }

            return r;
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
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    return valor.Trim();
                }
            }

            return null;
        }

        private static PaseoResponseDto MapResponse(Paseo paseo)
        {
            var duenio = paseo.Perro.Dueño;
            var usuarioPaseador = paseo.Paseador.Usuario;
            var paseadorNombreCompleto = $"{usuarioPaseador.Nombre} {usuarioPaseador.Apellido}".Trim();
            var duenioNombreCompleto = $"{duenio.Nombre} {duenio.Apellido}".Trim();

            return new PaseoResponseDto
            {
                Id = paseo.Id,

                PerroId = paseo.PerroId,
                PerroNombre = paseo.Perro.Nombre,
                PerroFotoUrl = paseo.Perro.ImagenUrl,
                PerroImagenUrl = paseo.Perro.ImagenUrl,

                PaseadorId = paseo.PaseadorId,
                PaseadorNombre = usuarioPaseador.Nombre,
                PaseadorApellido = usuarioPaseador.Apellido,
                PaseadorNombreCompleto = paseadorNombreCompleto,
                PaseadorFotoUrl = paseo.Paseador.FotoUrl,

                DuenioId = duenio.Id,
                DuenioNombre = duenio.Nombre,
                DuenioApellido = duenio.Apellido,
                DuenioNombreCompleto = duenioNombreCompleto,

                Estado = paseo.Estado,

                DuracionMinutos = paseo.DuracionMinutos,
                DuracionRealMinutos = paseo.DuracionRealMinutos,

                EsProgramado = paseo.EsProgramado,
                FechaProgramada = paseo.FechaProgramada,
                FechaInicio = paseo.FechaInicio,
                FechaFin = paseo.FechaFin,

                Precio = paseo.Precio,

                DireccionRecogida = paseo.DireccionRecogida,
                UbicacionTexto = paseo.DireccionRecogida,
                ReferenciasRecogida = paseo.ReferenciasRecogida,
                ZonaRecogida = paseo.ZonaRecogida,
                LatitudRecogida = paseo.LatitudRecogida,
                LongitudRecogida = paseo.LongitudRecogida,

                LatitudActual = paseo.LatitudActual,
                LongitudActual = paseo.LongitudActual,

                MotivoCancelacion = paseo.MotivoCancelacion,
                CanceladoPor = paseo.CanceladoPor,
                FechaCancelacion = paseo.FechaCancelacion,

                FotoInicioUrl = paseo.FotoInicioUrl,
                FotoFinUrl = paseo.FotoFinUrl
            };
        }

        private static PaseoDetalleDto MapDetalle(Paseo paseo)
        {
            var duenio = paseo.Perro.Dueño;
            var usuarioPaseador = paseo.Paseador.Usuario;
            var paseadorNombreCompleto = $"{usuarioPaseador.Nombre} {usuarioPaseador.Apellido}".Trim();
            var duenioNombreCompleto = $"{duenio.Nombre} {duenio.Apellido}".Trim();

            return new PaseoDetalleDto
            {
                Id = paseo.Id,

                PerroId = paseo.PerroId,
                PerroNombre = paseo.Perro.Nombre,
                PerroFotoUrl = paseo.Perro.ImagenUrl,
                PerroImagenUrl = paseo.Perro.ImagenUrl,

                PaseadorId = paseo.PaseadorId,
                PaseadorNombre = usuarioPaseador.Nombre,
                PaseadorApellido = usuarioPaseador.Apellido,
                PaseadorNombreCompleto = paseadorNombreCompleto,
                PaseadorFotoUrl = paseo.Paseador.FotoUrl,

                DuenioId = duenio.Id,
                DuenioNombre = duenio.Nombre,
                DuenioApellido = duenio.Apellido,
                DuenioNombreCompleto = duenioNombreCompleto,

                Estado = paseo.Estado,

                DuracionMinutos = paseo.DuracionMinutos,
                DuracionRealMinutos = paseo.DuracionRealMinutos,

                EsProgramado = paseo.EsProgramado,
                FechaProgramada = paseo.FechaProgramada,
                FechaInicio = paseo.FechaInicio,
                FechaFin = paseo.FechaFin,

                Precio = paseo.Precio,

                DireccionRecogida = paseo.DireccionRecogida,
                UbicacionTexto = paseo.DireccionRecogida,
                ReferenciasRecogida = paseo.ReferenciasRecogida,
                ZonaRecogida = paseo.ZonaRecogida,
                LatitudRecogida = paseo.LatitudRecogida,
                LongitudRecogida = paseo.LongitudRecogida,

                LatitudActual = paseo.LatitudActual,
                LongitudActual = paseo.LongitudActual,

                MotivoCancelacion = paseo.MotivoCancelacion,
                CanceladoPor = paseo.CanceladoPor,
                FechaCancelacion = paseo.FechaCancelacion,

                FotoInicioUrl = paseo.FotoInicioUrl,
                FotoFinUrl = paseo.FotoFinUrl
            };
        }
    }
}