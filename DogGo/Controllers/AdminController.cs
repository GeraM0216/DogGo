using System.Security.Claims;
using DogGo.Data;
using DogGo.Models;
using DogGo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly DogGoDbContext _context;

        public AdminController(DogGoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? busquedaUsuario,
            string? rolFiltro,
            string? emailConfirmadoFiltro,
            string? busquedaPerro,
            string? tamanioFiltro,
            string? busquedaPaseador,
            string? disponibleFiltro,
            string? ordenPaseador,
            string? busquedaPaseo,
            string? estadoFiltro,
            int? paseadorIdFiltro,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            var hoyUtc = DateTime.UtcNow;
            var hace7DiasUtc = hoyUtc.AddDays(-7);

            var usuariosBase = await _context.Usuarios
                .AsNoTracking()
                .ToListAsync();

            var perrosBase = await _context.Perros
                .AsNoTracking()
                .Include(p => p.Dueño)
                .ToListAsync();

            var paseadoresBase = await _context.Paseadores
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Paseos)
                .ToListAsync();

            var paseosBase = await _context.Paseos
                .AsNoTracking()
                .Include(p => p.Perro)
                    .ThenInclude(perro => perro.Dueño)
                .Include(p => p.Paseador)
                    .ThenInclude(pa => pa.Usuario)
                .Include(p => p.PaseoPerros)
                    .ThenInclude(pp => pp.Perro)
                .ToListAsync();

            var usuarios = usuariosBase.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busquedaUsuario))
            {
                var b = busquedaUsuario.Trim().ToLower();

                usuarios = usuarios.Where(u =>
                    $"{u.Nombre} {u.Apellido}".ToLower().Contains(b) ||
                    u.Email.ToLower().Contains(b) ||
                    (!string.IsNullOrWhiteSpace(u.Telefono) && u.Telefono.ToLower().Contains(b)));
            }

            if (!string.IsNullOrWhiteSpace(rolFiltro))
            {
                usuarios = usuarios.Where(u => u.Rol == rolFiltro);
            }

            if (emailConfirmadoFiltro == "true")
            {
                usuarios = usuarios.Where(u => u.EmailConfirmado);
            }
            else if (emailConfirmadoFiltro == "false")
            {
                usuarios = usuarios.Where(u => !u.EmailConfirmado);
            }

            var usuariosVm = usuarios
                .OrderByDescending(u => u.FechaRegistro)
                .Select(u => new AdminUsuarioItemViewModel
                {
                    Id = u.Id,
                    NombreCompleto = $"{u.Nombre} {u.Apellido}".Trim(),
                    Email = u.Email,
                    Telefono = u.Telefono,
                    Rol = u.Rol,
                    EmailConfirmado = u.EmailConfirmado,
                    FechaRegistro = u.FechaRegistro
                })
                .ToList();

            var perros = perrosBase.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busquedaPerro))
            {
                var b = busquedaPerro.Trim().ToLower();

                perros = perros.Where(p =>
                    p.Nombre.ToLower().Contains(b) ||
                    (!string.IsNullOrWhiteSpace(p.Raza) && p.Raza.ToLower().Contains(b)) ||
                    $"{p.Dueño.Nombre} {p.Dueño.Apellido}".ToLower().Contains(b) ||
                    p.Dueño.Email.ToLower().Contains(b));
            }

            if (!string.IsNullOrWhiteSpace(tamanioFiltro))
            {
                perros = perros.Where(p => p.Tamaño == tamanioFiltro);
            }

            var perrosVm = perros
                .OrderBy(p => p.Nombre)
                .Select(p => new AdminPerroItemViewModel
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Raza = p.Raza,
                    Edad = p.Edad,
                    Tamanio = p.Tamaño,
                    DuenioNombre = $"{p.Dueño.Nombre} {p.Dueño.Apellido}".Trim(),
                    DuenioEmail = p.Dueño.Email
                })
                .ToList();

            var paseadores = paseadoresBase.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busquedaPaseador))
            {
                var b = busquedaPaseador.Trim().ToLower();

                paseadores = paseadores.Where(p =>
                    $"{p.Usuario.Nombre} {p.Usuario.Apellido}".ToLower().Contains(b) ||
                    p.Usuario.Email.ToLower().Contains(b) ||
                    (!string.IsNullOrWhiteSpace(p.ZonaServicio) && p.ZonaServicio.ToLower().Contains(b)));
            }

            if (disponibleFiltro == "true")
            {
                paseadores = paseadores.Where(p => p.Disponible);
            }
            else if (disponibleFiltro == "false")
            {
                paseadores = paseadores.Where(p => !p.Disponible);
            }

            paseadores = ordenPaseador switch
            {
                "calificacion_desc" => paseadores.OrderByDescending(p => p.CalificacionPromedio),
                "tarifa_asc" => paseadores.OrderBy(p => p.TarifaPorHora),
                "tarifa_desc" => paseadores.OrderByDescending(p => p.TarifaPorHora),
                "experiencia_desc" => paseadores.OrderByDescending(p => p.ExperienciaAnios ?? 0),
                "paseos_desc" => paseadores.OrderByDescending(p => p.Paseos.Count(x => x.Estado == "Finalizado")),
                _ => paseadores.OrderBy(p => p.Usuario.Nombre)
            };

            var paseadoresVm = paseadores
                .Select(p =>
                {
                    var paseosFinalizados = p.Paseos.Where(x => x.Estado == "Finalizado").ToList();
                    var ingresos = paseosFinalizados.Sum(x => x.Precio);

                    return new AdminPaseadorItemViewModel
                    {
                        Id = p.Id,
                        UsuarioId = p.UsuarioId,
                        NombreCompleto = $"{p.Usuario.Nombre} {p.Usuario.Apellido}".Trim(),
                        Email = p.Usuario.Email,
                        TarifaPorHora = p.TarifaPorHora,
                        CalificacionPromedio = p.CalificacionPromedio,
                        Disponible = p.Disponible,
                        ZonaServicio = p.ZonaServicio,
                        ExperienciaAnios = p.ExperienciaAnios,
                        TotalPaseos = paseosFinalizados.Count,
                        IngresosFinalizados = ingresos
                    };
                })
                .ToList();

            var paseos = paseosBase.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busquedaPaseo))
            {
                var b = busquedaPaseo.Trim().ToLower();

                paseos = paseos.Where(p =>
                    ObtenerNombresPerros(p).ToLower().Contains(b) ||
                    $"{p.Paseador.Usuario.Nombre} {p.Paseador.Usuario.Apellido}".ToLower().Contains(b) ||
                    $"{p.Perro.Dueño.Nombre} {p.Perro.Dueño.Apellido}".ToLower().Contains(b) ||
                    (!string.IsNullOrWhiteSpace(p.ZonaRecogida) && p.ZonaRecogida.ToLower().Contains(b)));
            }

            if (!string.IsNullOrWhiteSpace(estadoFiltro))
            {
                paseos = paseos.Where(p => p.Estado == estadoFiltro);
            }

            if (paseadorIdFiltro.HasValue)
            {
                paseos = paseos.Where(p => p.PaseadorId == paseadorIdFiltro.Value);
            }

            if (fechaDesde.HasValue)
            {
                paseos = paseos.Where(p => ObtenerFechaReferencia(p) >= fechaDesde.Value.Date);
            }

            if (fechaHasta.HasValue)
            {
                var hasta = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                paseos = paseos.Where(p => ObtenerFechaReferencia(p) <= hasta);
            }

            var paseosVm = paseos
                .OrderByDescending(p => ObtenerFechaReferencia(p))
                .Select(p => new AdminPaseoItemViewModel
                {
                    Id = p.Id,
                    Estado = p.Estado,
                    PerroNombre = ObtenerNombresPerros(p),
                    DuenioNombre = $"{p.Perro.Dueño.Nombre} {p.Perro.Dueño.Apellido}".Trim(),
                    PaseadorNombre = $"{p.Paseador.Usuario.Nombre} {p.Paseador.Usuario.Apellido}".Trim(),
                    PaseadorId = p.PaseadorId,
                    Precio = p.Precio > 0 ? p.Precio : CalcularPrecioPaseo(p.Paseador.TarifaPorHora, p.DuracionMinutos),
                    DuracionMinutos = p.DuracionMinutos,
                    DuracionRealMinutos = p.DuracionRealMinutos,
                    EsProgramado = p.EsProgramado,
                    FechaProgramada = p.FechaProgramada,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    FechaCancelacion = p.FechaCancelacion,
                    ZonaRecoleccion = p.ZonaRecogida,
                    FinalizacionAnticipadaSolicitada = p.FinalizacionAnticipadaSolicitada,
                    FinalizacionAnticipadaAprobada = p.FinalizacionAnticipadaAprobada
                })
                .ToList();

            var paseosPorPaseador = paseadoresBase
                .Select(pa =>
                {
                    var paseosDePaseador = paseosBase
                        .Where(p => p.PaseadorId == pa.Id)
                        .ToList();

                    var paseosFinalizados = paseosDePaseador
                        .Where(p => p.Estado == "Finalizado")
                        .ToList();

                    var ingresos = paseosFinalizados.Sum(p =>
                        p.Precio > 0 ? p.Precio : CalcularPrecioPaseo(pa.TarifaPorHora, p.DuracionMinutos));

                    return new AdminPaseosPorPaseadorItemViewModel
                    {
                        PaseadorId = pa.Id,
                        PaseadorNombre = $"{pa.Usuario.Nombre} {pa.Usuario.Apellido}".Trim(),
                        Email = pa.Usuario.Email,
                        TotalPaseos = paseosDePaseador.Count,
                        Pendientes = paseosDePaseador.Count(p => p.Estado == "Pendiente"),
                        EnCurso = paseosDePaseador.Count(p => p.Estado == "EnCurso"),
                        Finalizados = paseosFinalizados.Count,
                        Cancelados = paseosDePaseador.Count(p => p.Estado == "Cancelado"),
                        TotalIngresos = ingresos,
                        TicketPromedio = paseosFinalizados.Any()
                            ? Math.Round(ingresos / paseosFinalizados.Count, 2, MidpointRounding.AwayFromZero)
                            : 0,
                        CalificacionPromedio = pa.CalificacionPromedio
                    };
                })
                .OrderByDescending(p => p.Finalizados)
                .ToList();

            var paseosFinalizadosGlobal = paseosBase
                .Where(p => p.Estado == "Finalizado")
                .ToList();

            var ingresosFinalizados = paseosFinalizadosGlobal.Sum(p =>
                p.Precio > 0
                    ? p.Precio
                    : CalcularPrecioPaseo(p.Paseador.TarifaPorHora, p.DuracionMinutos));

            var actividadReciente = paseosBase
                .Select(p => new AdminActividadItemViewModel
                {
                    PaseoId = p.Id,
                    Estado = p.Estado,
                    Accion = ObtenerAccionActividad(p),
                    PerroNombre = ObtenerNombresPerros(p),
                    DuenioNombre = p.Perro?.Dueño != null
                        ? $"{p.Perro.Dueño.Nombre} {p.Perro.Dueño.Apellido}".Trim()
                        : "—",
                    PaseadorNombre = p.Paseador?.Usuario != null
                        ? $"{p.Paseador.Usuario.Nombre} {p.Paseador.Usuario.Apellido}".Trim()
                        : "—",
                    Fecha = ObtenerFechaReferencia(p),
                    Precio = p.Precio
                })
                .Where(a => a.Fecha != DateTime.MinValue)
                .OrderByDescending(a => a.Fecha)
                .Take(8)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalUsuarios = usuariosBase.Count,
                TotalDuenios = usuariosBase.Count(u => u.Rol == "Duenio"),
                TotalPaseadoresUsuarios = usuariosBase.Count(u => u.Rol == "Paseador"),
                TotalAdmins = usuariosBase.Count(u => u.Rol == "Admin"),

                TotalPerros = perrosBase.Count,
                TotalPaseadores = paseadoresBase.Count,
                TotalPaseos = paseosBase.Count,
                PaseosPendientes = paseosBase.Count(p => p.Estado == "Pendiente"),
                PaseosEnCurso = paseosBase.Count(p => p.Estado == "EnCurso"),
                PaseosFinalizados = paseosBase.Count(p => p.Estado == "Finalizado"),
                PaseosCancelados = paseosBase.Count(p => p.Estado == "Cancelado"),

                IngresosFinalizados = ingresosFinalizados,
                TicketPromedioFinalizados = paseosFinalizadosGlobal.Any()
                    ? Math.Round(ingresosFinalizados / paseosFinalizadosGlobal.Count, 2, MidpointRounding.AwayFromZero)
                    : 0,
                PorcentajeCancelacion = paseosBase.Any()
                    ? Math.Round((decimal)paseosBase.Count(p => p.Estado == "Cancelado") * 100 / paseosBase.Count, 1)
                    : 0,
                UsuariosNuevosUltimos7Dias = usuariosBase.Count(u => u.FechaRegistro >= hace7DiasUtc),
                PaseosUltimos7Dias = paseosBase.Count(p => ObtenerFechaReferencia(p) >= hace7DiasUtc),
                SolicitudesFinalizacionPendientes = paseosBase.Count(p =>
                    p.Estado == "EnCurso" &&
                    p.FinalizacionAnticipadaSolicitada &&
                    p.FinalizacionAnticipadaAprobada == null),

                BusquedaUsuario = busquedaUsuario,
                RolFiltro = rolFiltro,
                EmailConfirmadoFiltro = emailConfirmadoFiltro,

                BusquedaPerro = busquedaPerro,
                TamanioFiltro = tamanioFiltro,

                BusquedaPaseador = busquedaPaseador,
                DisponibleFiltro = disponibleFiltro,
                OrdenPaseador = ordenPaseador,

                BusquedaPaseo = busquedaPaseo,
                EstadoFiltro = estadoFiltro,
                PaseadorIdFiltro = paseadorIdFiltro,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,

                Usuarios = usuariosVm,
                Perros = perrosVm,
                Paseadores = paseadoresVm,
                Paseos = paseosVm,
                PaseosPorPaseador = paseosPorPaseador,
                ActividadReciente = actividadReciente,

                PaseadoresFiltro = paseadoresBase
                    .OrderBy(p => p.Usuario.Nombre)
                    .Select(p => new AdminPaseadorSelectViewModel
                    {
                        Id = p.Id,
                        NombreCompleto = $"{p.Usuario.Nombre} {p.Usuario.Apellido}".Trim()
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(int usuarioId, string nuevoRol)
        {
            var rolesPermitidos = new[] { "Admin", "Duenio", "Paseador" };

            if (!rolesPermitidos.Contains(nuevoRol))
            {
                TempData["Error"] = "Rol no válido.";
                return RedirectToAction("Index");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            var usuarioActualIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(usuarioActualIdClaim) &&
                int.TryParse(usuarioActualIdClaim, out var usuarioActualId) &&
                usuarioActualId == usuarioId &&
                nuevoRol != "Admin")
            {
                TempData["Error"] = "No puedes quitarte el rol Admin a ti mismo.";
                return RedirectToAction("Index");
            }

            usuario.Rol = nuevoRol;
            usuario.EmailConfirmado = true;

            if (nuevoRol == "Paseador")
            {
                var existePaseador = await _context.Paseadores
                    .AnyAsync(p => p.UsuarioId == usuario.Id);

                if (!existePaseador)
                {
                    _context.Paseadores.Add(new Paseador
                    {
                        UsuarioId = usuario.Id,
                        Descripcion = string.Empty,
                        TarifaPorHora = 0,
                        CalificacionPromedio = 0,
                        Disponible = true,
                        FotoUrl = null,
                        ZonaServicio = null,
                        ExperienciaAnios = null
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Rol actualizado correctamente para {usuario.Nombre} {usuario.Apellido}.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCorreo(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            usuario.EmailConfirmado = true;
            usuario.CodigoConfirmacion = null;
            usuario.CodigoExpiraEn = null;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Correo confirmado para {usuario.Nombre} {usuario.Apellido}.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarDisponibilidadPaseador(int paseadorId)
        {
            var paseador = await _context.Paseadores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == paseadorId);

            if (paseador == null)
            {
                TempData["Error"] = "Paseador no encontrado.";
                return RedirectToAction("Index");
            }

            paseador.Disponible = !paseador.Disponible;

            await _context.SaveChangesAsync();

            TempData["Exito"] = paseador.Disponible
                ? $"{paseador.Usuario.Nombre} ahora aparece como disponible."
                : $"{paseador.Usuario.Nombre} ahora aparece como no disponible.";

            return RedirectToAction("Index");
        }

        private static DateTime ObtenerFechaReferencia(Paseo paseo)
        {
            return paseo.FechaCancelacion
                ?? paseo.FechaFin
                ?? paseo.FechaInicio
                ?? paseo.FechaProgramada
                ?? DateTime.MinValue;
        }

        private static string ObtenerAccionActividad(Paseo paseo)
        {
            if (paseo.Estado == "Finalizado")
                return paseo.FinalizacionAnticipadaAprobada == true
                    ? "Finalizado anticipadamente"
                    : "Paseo finalizado";

            if (paseo.Estado == "Cancelado")
                return "Paseo cancelado";

            if (paseo.Estado == "EnCurso")
            {
                if (paseo.FinalizacionAnticipadaSolicitada &&
                    paseo.FinalizacionAnticipadaAprobada == null)
                    return "Solicitud de finalización";

                return "Paseo en curso";
            }

            if (paseo.Estado == "Pendiente")
                return paseo.EsProgramado ? "Paseo programado" : "Paseo pendiente";

            return paseo.Estado;
        }

        private static string ObtenerNombresPerros(Paseo paseo)
        {
            if (paseo.PaseoPerros != null && paseo.PaseoPerros.Any(pp => pp.Perro != null))
            {
                var nombres = paseo.PaseoPerros
                    .Where(pp => pp.Perro != null)
                    .Select(pp => pp.Perro!.Nombre)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                if (nombres.Any())
                    return string.Join(", ", nombres);
            }

            return paseo.Perro?.Nombre ?? "Perro";
        }

        private static decimal CalcularPrecioPaseo(decimal tarifaPorHora, int duracionMinutos)
        {
            return Math.Round(
                tarifaPorHora * (duracionMinutos / 60m),
                2,
                MidpointRounding.AwayFromZero
            );
        }
    }
}