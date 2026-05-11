namespace DogGo.ViewModels
{
    public class SuperAdminDashboardViewModel
    {
        public DateTime FechaVista { get; set; } = DateTime.Now;

        public string Ambiente { get; set; } = string.Empty;
        public string EstadoBaseDatos { get; set; } = string.Empty;
        public string EstadoServidor { get; set; } = string.Empty;

        public int TotalUsuarios { get; set; }
        public int TotalUsuariosActivos { get; set; }
        public int TotalUsuariosDesactivados { get; set; }

        public int TotalDuenios { get; set; }
        public int TotalPaseadoresUsuarios { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalSuperAdmins { get; set; }

        public int TotalPerros { get; set; }
        public int TotalPaseadores { get; set; }
        public int TotalPaseos { get; set; }

        public int PaseosPendientes { get; set; }
        public int PaseosEnCurso { get; set; }
        public int PaseosFinalizados { get; set; }
        public int PaseosCancelados { get; set; }

        public decimal IngresosFinalizados { get; set; }
        public decimal TicketPromedio { get; set; }

        public int UsuariosNuevosSemana { get; set; }
        public int UsuariosSinConfirmar { get; set; }
        public int CodigosConfirmacionActivos { get; set; }
        public int CodigosConfirmacionExpirados { get; set; }
        public int RecuperacionesPasswordActivas { get; set; }

        public int PaseosEnCursoAtorados { get; set; }
        public int CanceladosUltimos7Dias { get; set; }

        public int UsuariosSinTelefono { get; set; }
        public int PerrosSinImagen { get; set; }
        public int PaseadoresSinFoto { get; set; }
        public int PaseadoresSinZona { get; set; }

        public List<SuperAdminModuleStatusViewModel> Modulos { get; set; } = new();
        public List<SuperAdminActivityViewModel> ActividadReciente { get; set; } = new();
        public List<SuperAdminAlertViewModel> Alertas { get; set; } = new();
        public List<SuperAdminDataQualityViewModel> CalidadDatos { get; set; } = new();
        public List<SuperAdminRoleDistributionViewModel> DistribucionRoles { get; set; } = new();
        public List<SuperAdminUserRoleViewModel> UsuariosAdministrativos { get; set; } = new();
        public List<SuperAdminUserManagementViewModel> UsuariosSistema { get; set; } = new();
    }

    public class SuperAdminModuleStatusViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Tipo { get; set; } = "ok";
        public string Icono { get; set; } = "🧩";
    }

    public class SuperAdminActivityViewModel
    {
        public string Titulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = "info";
        public string Icono { get; set; } = "•";
    }

    public class SuperAdminAlertViewModel
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Valor { get; set; }
        public string Tipo { get; set; } = "info";
        public string Icono { get; set; } = "⚠️";
    }

    public class SuperAdminDataQualityViewModel
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Valor { get; set; }
        public string Tipo { get; set; } = "warning";
        public string Icono { get; set; } = "🧹";
    }

    public class SuperAdminRoleDistributionViewModel
    {
        public string Rol { get; set; } = string.Empty;
        public int Total { get; set; }
        public string Icono { get; set; } = "👤";
        public string Tipo { get; set; } = "info";
    }

    public class SuperAdminUserRoleViewModel
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class SuperAdminUserManagementViewModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool EmailConfirmado { get; set; }
        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; }
        public DateTime? FechaDesactivacion { get; set; }
        public string? MotivoDesactivacion { get; set; }

        public bool TieneCodigoConfirmacionActivo { get; set; }
        public bool TieneCodigoConfirmacionExpirado { get; set; }
        public bool TieneRecuperacionActiva { get; set; }
        public bool TieneRecuperacionExpirada { get; set; }

        public bool EsUsuarioActual { get; set; }
    }
}