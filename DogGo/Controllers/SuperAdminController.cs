using System.Security.Claims;
using DogGo.Data;
using DogGo.Models;
using DogGo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string> RolesPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Duenio",
            "Paseador",
            "Admin",
            "SuperAdmin"
        };

        public SuperAdminController(
            DogGoDbContext context,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var ahora = DateTime.Now;
            var hace7Dias = ahora.AddDays(-7);
            var hace6Horas = ahora.AddHours(-6);

            var totalUsuarios = await _context.Usuarios.CountAsync();

            var totalUsuariosActivos = await _context.Usuarios
                .CountAsync(u => u.Activo);

            var totalUsuariosDesactivados = await _context.Usuarios
                .CountAsync(u => !u.Activo);

            var totalDuenios = await _context.Usuarios
                .CountAsync(u => u.Rol == "Duenio" || u.Rol == "Dueño");

            var totalPaseadoresUsuarios = await _context.Usuarios
                .CountAsync(u => u.Rol == "Paseador");

            var totalAdmins = await _context.Usuarios
                .CountAsync(u => u.Rol == "Admin");

            var totalSuperAdmins = await _context.Usuarios
                .CountAsync(u => u.Rol == "SuperAdmin");

            var totalPerros = await _context.Perros.CountAsync();
            var totalPaseadores = await _context.Paseadores.CountAsync();
            var totalPaseos = await _context.Paseos.CountAsync();

            var paseosPendientes = await _context.Paseos
                .CountAsync(p => p.Estado == "Pendiente");

            var paseosEnCurso = await _context.Paseos
                .CountAsync(p => p.Estado == "EnCurso");

            var paseosFinalizados = await _context.Paseos
                .CountAsync(p => p.Estado == "Finalizado");

            var paseosCancelados = await _context.Paseos
                .CountAsync(p => p.Estado == "Cancelado");

            var ingresosFinalizados = await _context.Paseos
                .Where(p => p.Estado == "Finalizado")
                .SumAsync(p => (decimal?)p.Precio) ?? 0m;

            var ticketPromedio = paseosFinalizados > 0
                ? ingresosFinalizados / paseosFinalizados
                : 0m;

            var usuariosNuevosSemana = await _context.Usuarios
                .CountAsync(u => ((DateTime?)u.FechaRegistro) >= hace7Dias);

            var usuariosSinConfirmar = await _context.Usuarios
                .CountAsync(u => !u.EmailConfirmado);

            var codigosConfirmacionActivos = await _context.Usuarios
                .CountAsync(u =>
                    u.CodigoConfirmacion != null &&
                    u.CodigoExpiraEn != null &&
                    u.CodigoExpiraEn > ahora);

            var codigosConfirmacionExpirados = await _context.Usuarios
                .CountAsync(u =>
                    u.CodigoConfirmacion != null &&
                    u.CodigoExpiraEn != null &&
                    u.CodigoExpiraEn <= ahora);

            var recuperacionesPasswordActivas = await _context.Usuarios
                .CountAsync(u =>
                    u.CodigoRecuperacion != null &&
                    u.CodigoRecuperacionExpiraEn != null &&
                    u.CodigoRecuperacionExpiraEn > ahora);

            var recuperacionesPasswordExpiradas = await _context.Usuarios
                .CountAsync(u =>
                    u.CodigoRecuperacion != null &&
                    u.CodigoRecuperacionExpiraEn != null &&
                    u.CodigoRecuperacionExpiraEn <= ahora);

            var paseosEnCursoAtorados = await _context.Paseos
                .CountAsync(p =>
                    p.Estado == "EnCurso" &&
                    ((DateTime?)p.FechaInicio) <= hace6Horas);

            var canceladosUltimos7Dias = await _context.Paseos
                .CountAsync(p =>
                    p.Estado == "Cancelado" &&
                    ((DateTime?)p.FechaInicio) >= hace7Dias);

            var usuariosSinTelefono = await _context.Usuarios
                .CountAsync(u => u.Telefono == null || u.Telefono == "");

            var perrosSinImagen = await _context.Perros
                .CountAsync(p => p.ImagenUrl == null || p.ImagenUrl == "");

            var paseadoresSinFoto = await _context.Paseadores
                .CountAsync(p => p.FotoUrl == null || p.FotoUrl == "");

            var paseadoresSinZona = await _context.Paseadores
                .CountAsync(p => p.ZonaServicio == null || p.ZonaServicio == "");

            var actividadReciente = await ConstruirActividadReciente(ahora);
            var usuariosAdministrativos = await ObtenerUsuariosAdministrativos(ahora);
            var usuariosSistema = await ObtenerUsuariosSistema(ahora);

            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            var alertas = new List<SuperAdminAlertViewModel>
            {
                new()
                {
                    Titulo = "Paseos en curso atorados",
                    Descripcion = "Paseos que llevan más de 6 horas en estado EnCurso.",
                    Valor = paseosEnCursoAtorados,
                    Tipo = paseosEnCursoAtorados > 0 ? "danger" : "success",
                    Icono = "⏱️"
                },
                new()
                {
                    Titulo = "Usuarios sin confirmar",
                    Descripcion = "Cuentas registradas que todavía no validan su correo.",
                    Valor = usuariosSinConfirmar,
                    Tipo = usuariosSinConfirmar > 0 ? "warning" : "success",
                    Icono = "📩"
                },
                new()
                {
                    Titulo = "Códigos expirados",
                    Descripcion = "Códigos de confirmación vencidos que podrían requerir limpieza.",
                    Valor = codigosConfirmacionExpirados,
                    Tipo = codigosConfirmacionExpirados > 0 ? "warning" : "success",
                    Icono = "⌛"
                },
                new()
                {
                    Titulo = "Recuperaciones expiradas",
                    Descripcion = "Códigos de recuperación vencidos pendientes de limpieza.",
                    Valor = recuperacionesPasswordExpiradas,
                    Tipo = recuperacionesPasswordExpiradas > 0 ? "warning" : "success",
                    Icono = "🔑"
                },
                new()
                {
                    Titulo = "Usuarios desactivados",
                    Descripcion = "Cuentas bloqueadas por el SuperAdmin.",
                    Valor = totalUsuariosDesactivados,
                    Tipo = totalUsuariosDesactivados > 0 ? "warning" : "success",
                    Icono = "🚷"
                }
            };

            var calidadDatos = new List<SuperAdminDataQualityViewModel>
            {
                new()
                {
                    Titulo = "Usuarios sin teléfono",
                    Descripcion = "Usuarios con información de contacto incompleta.",
                    Valor = usuariosSinTelefono,
                    Tipo = usuariosSinTelefono > 0 ? "warning" : "success",
                    Icono = "☎️"
                },
                new()
                {
                    Titulo = "Perros sin imagen",
                    Descripcion = "Mascotas registradas sin fotografía en el sistema.",
                    Valor = perrosSinImagen,
                    Tipo = perrosSinImagen > 0 ? "warning" : "success",
                    Icono = "🐶"
                },
                new()
                {
                    Titulo = "Paseadores sin foto",
                    Descripcion = "Perfiles de paseadores que aún no tienen imagen.",
                    Valor = paseadoresSinFoto,
                    Tipo = paseadoresSinFoto > 0 ? "warning" : "success",
                    Icono = "🧍"
                },
                new()
                {
                    Titulo = "Paseadores sin zona",
                    Descripcion = "Paseadores sin zona de servicio configurada.",
                    Valor = paseadoresSinZona,
                    Tipo = paseadoresSinZona > 0 ? "warning" : "success",
                    Icono = "📍"
                }
            };

            var distribucionRoles = new List<SuperAdminRoleDistributionViewModel>
            {
                new()
                {
                    Rol = "Dueños",
                    Total = totalDuenios,
                    Icono = "👤",
                    Tipo = "blue"
                },
                new()
                {
                    Rol = "Paseadores",
                    Total = totalPaseadoresUsuarios,
                    Icono = "🐕‍🦺",
                    Tipo = "green"
                },
                new()
                {
                    Rol = "Admins negocio",
                    Total = totalAdmins,
                    Icono = "🛡️",
                    Tipo = "purple"
                },
                new()
                {
                    Rol = "SuperAdmins",
                    Total = totalSuperAdmins,
                    Icono = "⚙️",
                    Tipo = "dark"
                }
            };

            var model = new SuperAdminDashboardViewModel
            {
                FechaVista = ahora,

                Ambiente = _environment.EnvironmentName,
                EstadoServidor = "Operativo",
                EstadoBaseDatos = string.IsNullOrWhiteSpace(connectionString)
                    ? "Sin cadena configurada"
                    : "Conectada",

                TotalUsuarios = totalUsuarios,
                TotalUsuariosActivos = totalUsuariosActivos,
                TotalUsuariosDesactivados = totalUsuariosDesactivados,

                TotalDuenios = totalDuenios,
                TotalPaseadoresUsuarios = totalPaseadoresUsuarios,
                TotalAdmins = totalAdmins,
                TotalSuperAdmins = totalSuperAdmins,

                TotalPerros = totalPerros,
                TotalPaseadores = totalPaseadores,
                TotalPaseos = totalPaseos,

                PaseosPendientes = paseosPendientes,
                PaseosEnCurso = paseosEnCurso,
                PaseosFinalizados = paseosFinalizados,
                PaseosCancelados = paseosCancelados,

                IngresosFinalizados = ingresosFinalizados,
                TicketPromedio = ticketPromedio,

                UsuariosNuevosSemana = usuariosNuevosSemana,
                UsuariosSinConfirmar = usuariosSinConfirmar,
                CodigosConfirmacionActivos = codigosConfirmacionActivos,
                CodigosConfirmacionExpirados = codigosConfirmacionExpirados,
                RecuperacionesPasswordActivas = recuperacionesPasswordActivas,

                PaseosEnCursoAtorados = paseosEnCursoAtorados,
                CanceladosUltimos7Dias = canceladosUltimos7Dias,

                UsuariosSinTelefono = usuariosSinTelefono,
                PerrosSinImagen = perrosSinImagen,
                PaseadoresSinFoto = paseadoresSinFoto,
                PaseadoresSinZona = paseadoresSinZona,

                Alertas = alertas,
                CalidadDatos = calidadDatos,
                DistribucionRoles = distribucionRoles,
                UsuariosAdministrativos = usuariosAdministrativos,
                UsuariosSistema = usuariosSistema,
                ActividadReciente = actividadReciente,

                Modulos = new List<SuperAdminModuleStatusViewModel>
                {
                    new()
                    {
                        Nombre = "Autenticación",
                        Descripcion = "Login, cookies, roles y restricción de accesos.",
                        Estado = "Activo",
                        Tipo = "ok",
                        Icono = "🔐"
                    },
                    new()
                    {
                        Nombre = "Base de datos",
                        Descripcion = "Conexión principal de DogGo con MySQL.",
                        Estado = string.IsNullOrWhiteSpace(connectionString) ? "Revisar" : "Conectada",
                        Tipo = string.IsNullOrWhiteSpace(connectionString) ? "warning" : "ok",
                        Icono = "🗄️"
                    },
                    new()
                    {
                        Nombre = "API Flutter",
                        Descripcion = "Controladores API usados por la aplicación móvil.",
                        Estado = "Disponible",
                        Tipo = "ok",
                        Icono = "📱"
                    },
                    new()
                    {
                        Nombre = "SignalR Tracking",
                        Descripcion = "Actualización de ubicación en tiempo real durante paseos.",
                        Estado = "Configurado",
                        Tipo = "ok",
                        Icono = "📍"
                    },
                    new()
                    {
                        Nombre = "Correo SMTP",
                        Descripcion = "Confirmación de correo y recuperación de contraseña.",
                        Estado = "Configurado",
                        Tipo = "ok",
                        Icono = "✉️"
                    },
                    new()
                    {
                        Nombre = "Cloudflare Tunnel",
                        Descripcion = "Exposición temporal del backend hacia internet.",
                        Estado = "Manual",
                        Tipo = "warning",
                        Icono = "🌐"
                    },
                    new()
                    {
                        Nombre = "Sistema de roles",
                        Descripcion = "Dueño, Paseador, Admin y SuperAdmin.",
                        Estado = "Activo",
                        Tipo = "ok",
                        Icono = "🛡️"
                    },
                    new()
                    {
                        Nombre = "Almacenamiento de imágenes",
                        Descripcion = "Fotos de perros, paseadores, perfiles y evidencias.",
                        Estado = "En revisión",
                        Tipo = "warning",
                        Icono = "🖼️"
                    },
                    new()
                    {
                        Nombre = "Recuperación de contraseña",
                        Descripcion = "Códigos temporales para restablecer acceso.",
                        Estado = "Activo",
                        Tipo = "ok",
                        Icono = "🔑"
                    },
                    new()
                    {
                        Nombre = "Panel Admin negocio",
                        Descripcion = "Métricas operativas, usuarios, perros, paseos e ingresos.",
                        Estado = "Disponible",
                        Tipo = "ok",
                        Icono = "📊"
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(int usuarioId, string nuevoRol)
        {
            nuevoRol = NormalizarRol(nuevoRol);

            if (!RolesPermitidos.Contains(nuevoRol))
            {
                TempData["Error"] = "Rol no válido.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            if (EsUsuarioActual(usuario) && !string.Equals(usuario.Rol, nuevoRol, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "No puedes cambiar tu propio rol desde esta pantalla para evitar bloquear tu acceso.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var totalSuperAdminsActivos = await _context.Usuarios
                .CountAsync(u => u.Rol == "SuperAdmin" && u.Activo);

            if (usuario.Rol == "SuperAdmin" &&
                nuevoRol != "SuperAdmin" &&
                usuario.Activo &&
                totalSuperAdminsActivos <= 1)
            {
                TempData["Error"] = "No puedes quitar el último SuperAdmin activo del sistema.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var rolAnterior = usuario.Rol;
            usuario.Rol = nuevoRol;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Rol actualizado: {usuario.Email} pasó de {rolAnterior} a {nuevoRol}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(
            int usuarioId,
            string nombre,
            string apellido,
            string email,
            string telefono)
        {
            nombre = (nombre ?? string.Empty).Trim();
            apellido = (apellido ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim().ToLower();
            telefono = (telefono ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre del usuario es obligatorio.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            if (string.IsNullOrWhiteSpace(apellido))
            {
                TempData["Error"] = "El apellido del usuario es obligatorio.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                TempData["Error"] = "El correo del usuario no es válido.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var existeCorreo = await _context.Usuarios
                .AnyAsync(u => u.Id != usuarioId && u.Email == email);

            if (existeCorreo)
            {
                TempData["Error"] = "Ya existe otro usuario con ese correo.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            usuario.Nombre = nombre;
            usuario.Apellido = apellido;
            usuario.Email = email;
            usuario.Telefono = telefono;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Usuario actualizado correctamente: {usuario.Nombre} {usuario.Apellido}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesactivarUsuario(int usuarioId, string motivo)
        {
            motivo = (motivo ?? string.Empty).Trim();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            if (EsUsuarioActual(usuario))
            {
                TempData["Error"] = "No puedes desactivar tu propia cuenta.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            if (!usuario.Activo)
            {
                TempData["Error"] = "Este usuario ya está desactivado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var totalSuperAdminsActivos = await _context.Usuarios
                .CountAsync(u => u.Rol == "SuperAdmin" && u.Activo);

            if (usuario.Rol == "SuperAdmin" && totalSuperAdminsActivos <= 1)
            {
                TempData["Error"] = "No puedes desactivar el último SuperAdmin activo del sistema.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            usuario.Activo = false;
            usuario.FechaDesactivacion = DateTime.UtcNow;
            usuario.MotivoDesactivacion = string.IsNullOrWhiteSpace(motivo)
                ? "Desactivado por SuperAdmin"
                : motivo;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Usuario desactivado correctamente: {usuario.Email}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivarUsuario(int usuarioId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            if (usuario.Activo)
            {
                TempData["Error"] = "Este usuario ya está activo.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            usuario.Activo = true;
            usuario.FechaDesactivacion = null;
            usuario.MotivoDesactivacion = null;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Usuario reactivado correctamente: {usuario.Email}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(int usuarioId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            if (EsUsuarioActual(usuario))
            {
                TempData["Error"] = "No puedes eliminar tu propia cuenta.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var totalSuperAdmins = await _context.Usuarios
                .CountAsync(u => u.Rol == "SuperAdmin");

            if (usuario.Rol == "SuperAdmin" && totalSuperAdmins <= 1)
            {
                TempData["Error"] = "No puedes eliminar el último SuperAdmin del sistema.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            var bloqueos = await ObtenerBloqueosEliminacion(usuario);

            if (bloqueos.Any())
            {
                TempData["Error"] = $"No se puede eliminar a {usuario.Email} porque tiene datos relacionados: {string.Join(", ", bloqueos)}. Usa Desactivar usuario.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Usuario eliminado correctamente: {usuario.Email}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCorreo(int usuarioId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            usuario.EmailConfirmado = true;
            usuario.CodigoConfirmacion = null;
            usuario.CodigoExpiraEn = null;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Correo confirmado manualmente para {usuario.Email}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimpiarCodigoConfirmacionUsuario(int usuarioId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            usuario.CodigoConfirmacion = null;
            usuario.CodigoExpiraEn = null;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Código de confirmación limpiado para {usuario.Email}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimpiarRecuperacionUsuario(int usuarioId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index), null, null, "usuarios");
            }

            usuario.CodigoRecuperacion = null;
            usuario.CodigoRecuperacionExpiraEn = null;

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Código de recuperación limpiado para {usuario.Email}.";
            return RedirectToAction(nameof(Index), null, null, "usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimpiarCodigosExpirados()
        {
            var ahora = DateTime.Now;

            var usuarios = await _context.Usuarios
                .Where(u =>
                    u.CodigoConfirmacion != null &&
                    u.CodigoExpiraEn != null &&
                    u.CodigoExpiraEn <= ahora)
                .ToListAsync();

            foreach (var usuario in usuarios)
            {
                usuario.CodigoConfirmacion = null;
                usuario.CodigoExpiraEn = null;
            }

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Se limpiaron {usuarios.Count} códigos de confirmación expirados.";
            return RedirectToAction(nameof(Index), null, null, "alertas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimpiarRecuperacionesExpiradas()
        {
            var ahora = DateTime.Now;

            var usuarios = await _context.Usuarios
                .Where(u =>
                    u.CodigoRecuperacion != null &&
                    u.CodigoRecuperacionExpiraEn != null &&
                    u.CodigoRecuperacionExpiraEn <= ahora)
                .ToListAsync();

            foreach (var usuario in usuarios)
            {
                usuario.CodigoRecuperacion = null;
                usuario.CodigoRecuperacionExpiraEn = null;
            }

            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Se limpiaron {usuarios.Count} códigos de recuperación expirados.";
            return RedirectToAction(nameof(Index), null, null, "alertas");
        }

        private async Task<List<SuperAdminActivityViewModel>> ConstruirActividadReciente(DateTime ahora)
        {
            var actividadReciente = new List<SuperAdminActivityViewModel>();

            var usuariosRecientes = await _context.Usuarios
                .AsNoTracking()
                .OrderByDescending(u => (DateTime?)u.FechaRegistro)
                .Take(5)
                .Select(u => new
                {
                    u.Nombre,
                    u.Apellido,
                    u.Email,
                    u.Rol,
                    u.Activo,
                    FechaRegistro = (DateTime?)u.FechaRegistro
                })
                .ToListAsync();

            foreach (var usuario in usuariosRecientes)
            {
                actividadReciente.Add(new SuperAdminActivityViewModel
                {
                    Titulo = "Usuario registrado",
                    Detalle = $"{usuario.Nombre} {usuario.Apellido} · {usuario.Rol} · {usuario.Email} · {(usuario.Activo ? "Activo" : "Desactivado")}",
                    Fecha = usuario.FechaRegistro ?? ahora,
                    Tipo = usuario.Activo ? "info" : "danger",
                    Icono = usuario.Activo ? "👤" : "🚷"
                });
            }

            var paseosRecientes = await _context.Paseos
                .AsNoTracking()
                .Include(p => p.Perro)
                .OrderByDescending(p => (DateTime?)p.FechaInicio)
                .Take(7)
                .Select(p => new
                {
                    p.Id,
                    p.Estado,
                    FechaInicio = (DateTime?)p.FechaInicio,
                    PerroNombre = p.Perro != null ? p.Perro.Nombre : "Sin perro"
                })
                .ToListAsync();

            foreach (var paseo in paseosRecientes)
            {
                actividadReciente.Add(new SuperAdminActivityViewModel
                {
                    Titulo = $"Paseo {paseo.Estado}",
                    Detalle = $"Paseo #{paseo.Id} · {paseo.PerroNombre}",
                    Fecha = paseo.FechaInicio ?? ahora,
                    Tipo = paseo.Estado == "Cancelado" ? "danger" :
                           paseo.Estado == "Finalizado" ? "success" :
                           paseo.Estado == "EnCurso" ? "active" : "warning",
                    Icono = paseo.Estado == "Cancelado" ? "✕" :
                            paseo.Estado == "Finalizado" ? "✓" :
                            paseo.Estado == "EnCurso" ? "▶" : "⏳"
                });
            }

            return actividadReciente
                .OrderByDescending(a => a.Fecha)
                .Take(12)
                .ToList();
        }

        private async Task<List<SuperAdminUserRoleViewModel>> ObtenerUsuariosAdministrativos(DateTime ahora)
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == "Admin" || u.Rol == "SuperAdmin")
                .OrderBy(u => u.Rol)
                .ThenBy(u => u.Nombre)
                .Select(u => new
                {
                    u.Nombre,
                    u.Apellido,
                    u.Email,
                    u.Rol,
                    u.EmailConfirmado,
                    u.Activo,
                    FechaRegistro = (DateTime?)u.FechaRegistro
                })
                .ToListAsync();

            return usuarios.Select(u => new SuperAdminUserRoleViewModel
            {
                NombreCompleto = $"{u.Nombre} {u.Apellido}".Trim(),
                Email = u.Email,
                Rol = u.Rol,
                FechaRegistro = u.FechaRegistro ?? ahora,
                Estado = u.Activo
                    ? (u.EmailConfirmado ? "Confirmado" : "Sin confirmar")
                    : "Desactivado"
            }).ToList();
        }

        private async Task<List<SuperAdminUserManagementViewModel>> ObtenerUsuariosSistema(DateTime ahora)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var usuarioActualEmail = User.FindFirstValue(ClaimTypes.Email);

            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .OrderByDescending(u => u.Rol == "SuperAdmin")
                .ThenByDescending(u => u.Rol == "Admin")
                .ThenBy(u => u.Activo)
                .ThenByDescending(u => (DateTime?)u.FechaRegistro)
                .Take(100)
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.Email,
                    u.Telefono,
                    u.Rol,
                    u.EmailConfirmado,
                    u.Activo,
                    u.FechaDesactivacion,
                    u.MotivoDesactivacion,
                    u.CodigoConfirmacion,
                    u.CodigoExpiraEn,
                    u.CodigoRecuperacion,
                    u.CodigoRecuperacionExpiraEn,
                    FechaRegistro = (DateTime?)u.FechaRegistro
                })
                .ToListAsync();

            return usuarios.Select(u =>
            {
                var esUsuarioActual =
                    (usuarioActualId.HasValue && u.Id == usuarioActualId.Value) ||
                    (!string.IsNullOrWhiteSpace(usuarioActualEmail) &&
                     string.Equals(u.Email, usuarioActualEmail, StringComparison.OrdinalIgnoreCase));

                return new SuperAdminUserManagementViewModel
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    NombreCompleto = $"{u.Nombre} {u.Apellido}".Trim(),
                    Email = u.Email,
                    Telefono = string.IsNullOrWhiteSpace(u.Telefono) ? "Sin teléfono" : u.Telefono,
                    Rol = u.Rol,
                    EmailConfirmado = u.EmailConfirmado,
                    FechaRegistro = u.FechaRegistro ?? ahora,
                    Activo = u.Activo,
                    FechaDesactivacion = u.FechaDesactivacion,
                    MotivoDesactivacion = u.MotivoDesactivacion,

                    TieneCodigoConfirmacionActivo =
                        !string.IsNullOrWhiteSpace(u.CodigoConfirmacion) &&
                        u.CodigoExpiraEn != null &&
                        u.CodigoExpiraEn > ahora,

                    TieneCodigoConfirmacionExpirado =
                        !string.IsNullOrWhiteSpace(u.CodigoConfirmacion) &&
                        u.CodigoExpiraEn != null &&
                        u.CodigoExpiraEn <= ahora,

                    TieneRecuperacionActiva =
                        !string.IsNullOrWhiteSpace(u.CodigoRecuperacion) &&
                        u.CodigoRecuperacionExpiraEn != null &&
                        u.CodigoRecuperacionExpiraEn > ahora,

                    TieneRecuperacionExpirada =
                        !string.IsNullOrWhiteSpace(u.CodigoRecuperacion) &&
                        u.CodigoRecuperacionExpiraEn != null &&
                        u.CodigoRecuperacionExpiraEn <= ahora,

                    EsUsuarioActual = esUsuarioActual
                };
            }).ToList();
        }

        private async Task<List<string>> ObtenerBloqueosEliminacion(Usuario usuario)
        {
            var bloqueos = new List<string>();

            var tienePerros = await _context.Entry(usuario)
                .Collection(u => u.Perros)
                .Query()
                .AnyAsync();

            if (tienePerros)
            {
                bloqueos.Add("perros registrados");
            }

            var tienePerfilPaseador = await _context.Entry(usuario)
                .Reference(u => u.Paseador)
                .Query()
                .AnyAsync();

            if (tienePerfilPaseador)
            {
                bloqueos.Add("perfil de paseador");
            }

            var tienePerfilDuenio = await _context.Entry(usuario)
                .Reference(u => u.DuenioPerfil)
                .Query()
                .AnyAsync();

            if (tienePerfilDuenio)
            {
                bloqueos.Add("perfil de dueño");
            }

            var tieneMensajesEnviados = await _context.Entry(usuario)
                .Collection(u => u.Enviados)
                .Query()
                .AnyAsync();

            var tieneMensajesRecibidos = await _context.Entry(usuario)
                .Collection(u => u.Recibidos)
                .Query()
                .AnyAsync();

            if (tieneMensajesEnviados || tieneMensajesRecibidos)
            {
                bloqueos.Add("mensajes");
            }

            return bloqueos.Distinct().ToList();
        }

        private int? ObtenerUsuarioActualId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(idClaim, out var id))
            {
                return id;
            }

            return null;
        }

        private bool EsUsuarioActual(Usuario usuario)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var usuarioActualEmail = User.FindFirstValue(ClaimTypes.Email);

            if (usuarioActualId.HasValue && usuario.Id == usuarioActualId.Value)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(usuarioActualEmail) &&
                string.Equals(usuario.Email, usuarioActualEmail, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private string NormalizarRol(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
            {
                return string.Empty;
            }

            rol = rol.Trim();

            if (rol.Equals("Dueño", StringComparison.OrdinalIgnoreCase))
            {
                return "Duenio";
            }

            return rol;
        }
    }
}