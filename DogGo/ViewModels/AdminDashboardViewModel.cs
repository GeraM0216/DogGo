namespace DogGo.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Totales generales
        public int TotalUsuarios { get; set; }
        public int TotalDuenios { get; set; }
        public int TotalPaseadoresUsuarios { get; set; }
        public int TotalAdmins { get; set; }

        public int TotalPerros { get; set; }
        public int TotalPaseadores { get; set; }
        public int TotalPaseos { get; set; }
        public int PaseosPendientes { get; set; }
        public int PaseosEnCurso { get; set; }
        public int PaseosFinalizados { get; set; }
        public int PaseosCancelados { get; set; }

        // Filtros usuarios
        public string? BusquedaUsuario { get; set; }
        public string? RolFiltro { get; set; }
        public string? EmailConfirmadoFiltro { get; set; }

        // Filtros perros
        public string? BusquedaPerro { get; set; }
        public string? TamanioFiltro { get; set; }

        // Filtros paseadores
        public string? BusquedaPaseador { get; set; }
        public string? DisponibleFiltro { get; set; }
        public string? OrdenPaseador { get; set; }

        // Filtros paseos
        public string? BusquedaPaseo { get; set; }
        public string? EstadoFiltro { get; set; }
        public int? PaseadorIdFiltro { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }

        // Listas
        public List<AdminUsuarioItemViewModel> Usuarios { get; set; } = new();
        public List<AdminPerroItemViewModel> Perros { get; set; } = new();
        public List<AdminPaseadorItemViewModel> Paseadores { get; set; } = new();
        public List<AdminPaseoItemViewModel> Paseos { get; set; } = new();
        public List<AdminPaseosPorPaseadorItemViewModel> PaseosPorPaseador { get; set; } = new();

        public List<AdminPaseadorSelectViewModel> PaseadoresFiltro { get; set; } = new();
    }

    public class AdminUsuarioItemViewModel
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Rol { get; set; } = string.Empty;
        public bool EmailConfirmado { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class AdminPerroItemViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Raza { get; set; }
        public int Edad { get; set; }
        public string? Tamanio { get; set; }
        public string DuenioNombre { get; set; } = string.Empty;
        public string DuenioEmail { get; set; } = string.Empty;
    }

    public class AdminPaseadorItemViewModel
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TarifaPorHora { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public bool Disponible { get; set; }
        public string? ZonaServicio { get; set; }
        public int? ExperienciaAnios { get; set; }
        public int TotalPaseos { get; set; }
    }

    public class AdminPaseoItemViewModel
    {
        public int Id { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string PerroNombre { get; set; } = string.Empty;
        public string DuenioNombre { get; set; } = string.Empty;
        public string PaseadorNombre { get; set; } = string.Empty;
        public int PaseadorId { get; set; }
        public decimal Precio { get; set; }
        public int DuracionMinutos { get; set; }
        public bool EsProgramado { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? ZonaRecoleccion { get; set; }
    }

    public class AdminPaseosPorPaseadorItemViewModel
    {
        public int PaseadorId { get; set; }
        public string PaseadorNombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalPaseos { get; set; }
        public int Pendientes { get; set; }
        public int EnCurso { get; set; }
        public int Finalizados { get; set; }
        public int Cancelados { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal CalificacionPromedio { get; set; }
    }

    public class AdminPaseadorSelectViewModel
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }
}