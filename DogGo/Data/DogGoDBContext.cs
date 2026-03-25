using DogGo.Models;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Data
{
    public class DogGoDbContext : DbContext
    {
        public DogGoDbContext(DbContextOptions<DogGoDbContext> options)
            : base(options) { }

        // ── DbSets ───────────────────────────────────────────────
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Perro> Perros { get; set; }
        public DbSet<Paseador> Paseadores { get; set; }
        public DbSet<Paseo> Paseos { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<Ubicacion> Ubicaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Usuario ──────────────────────────────────────────
            modelBuilder.Entity<Usuario>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Rol)
                 .HasConversion<string>()
                 .HasMaxLength(20);
            });

            // ── Perro → Dueño ────────────────────────────────────
            modelBuilder.Entity<Perro>()
                .HasOne(p => p.Dueño)
                .WithMany(u => u.Perros)
                .HasForeignKey(p => p.DueñoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Paseador → Usuario (1-a-1) ───────────────────────
            modelBuilder.Entity<Paseador>()
                .HasOne(p => p.Usuario)
                .WithOne(u => u.Paseador)
                .HasForeignKey<Paseador>(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Paseo → Paseador ─────────────────────────────────
            modelBuilder.Entity<Paseo>()
                .HasOne(p => p.Paseador)
                .WithMany(pa => pa.Paseos)
                .HasForeignKey(p => p.PaseadorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Paseo → Perro ─────────────────────────────────────
            modelBuilder.Entity<Paseo>()
                .HasOne(p => p.Perro)
                .WithMany(pe => pe.Paseos)
                .HasForeignKey(p => p.PerroId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Calificacion → Paseo (1-a-1) ─────────────────────
            modelBuilder.Entity<Calificacion>()
                .HasOne(c => c.Paseo)
                .WithOne(p => p.Calificacion)
                .HasForeignKey<Calificacion>(c => c.PaseoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Mensaje → Emisor / Receptor ───────────────────────
            // Dos FK al mismo modelo requieren deshabilitar
            // la eliminación en cascada para evitar ciclos
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Emisor)
                .WithMany(u => u.Enviados)
                .HasForeignKey(m => m.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Receptor)
                .WithMany(u => u.Recibidos)
                .HasForeignKey(m => m.ReceptorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Mensaje → Paseo ───────────────────────────────────
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Paseo)
                .WithMany(p => p.Mensajes)
                .HasForeignKey(m => m.PaseoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Ubicacion → Paseo ─────────────────────────────────
            modelBuilder.Entity<Ubicacion>()
                .HasOne(u => u.Paseo)
                .WithMany(p => p.Ubicaciones)
                .HasForeignKey(u => u.PaseoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Precisión decimales (precio, coordenadas) ─────────
            modelBuilder.Entity<Paseador>()
                .Property(p => p.TarifaPorHora)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Paseador>()
                .Property(p => p.CalificacionPromedio)
                .HasPrecision(3, 2);

            modelBuilder.Entity<Paseo>()
                .Property(p => p.Precio)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Paseo>()
                .Property(p => p.LatitudActual)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Paseo>()
                .Property(p => p.LongitudActual)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Ubicacion>()
                .Property(u => u.Latitud)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Ubicacion>()
                .Property(u => u.Longitud)
                .HasPrecision(10, 7);
        }
    }
}
