using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DogGo.Hubs
{
    [Authorize]
    public class PaseoHub : Hub
    {
        private readonly DogGoDbContext _context;

        public PaseoHub(DogGoDbContext context)
        {
            _context = context;
        }

        // El paseador envía su ubicación cada X segundos
        public async Task EnviarUbicacion(int paseoId, decimal latitud, decimal longitud)
        {
            var usuarioIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return;

            if (latitud < -90 || latitud > 90 || longitud < -180 || longitud > 180)
                return;

            var usuarioId = int.Parse(usuarioIdClaim);

            var paseo = await _context.Paseos.FindAsync(paseoId);
            if (paseo == null)
                return;

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador == null)
                return;

            // Solo el paseador asignado puede enviar ubicación
            if (paseo.PaseadorId != paseador.Id)
                return;

            // Solo se permite enviar ubicación si el paseo está en curso
            if (paseo.Estado != "EnCurso")
                return;

            var ubicacion = new Ubicacion
            {
                PaseoId = paseoId,
                Latitud = latitud,
                Longitud = longitud,
                Timestamp = DateTime.UtcNow
            };

            _context.Ubicaciones.Add(ubicacion);

            paseo.LatitudActual = latitud;
            paseo.LongitudActual = longitud;

            await _context.SaveChangesAsync();

            await Clients.Group(paseoId.ToString())
                .SendAsync("RecibirUbicacion", latitud, longitud);
        }

        // Cambiar estado del paseo
        public async Task CambiarEstado(int paseoId, string nuevoEstado)
        {
            var usuarioIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return;

            var usuarioId = int.Parse(usuarioIdClaim);

            var paseo = await _context.Paseos.FindAsync(paseoId);
            if (paseo == null)
                return;

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador == null)
                return;

            // Solo el paseador asignado puede cambiar el estado
            if (paseo.PaseadorId != paseador.Id)
                return;

            if (paseo.Estado == "Cancelado" || paseo.Estado == "Finalizado")
                return;

            nuevoEstado = nuevoEstado?.Trim() ?? string.Empty;

            var transicionValida =
                (paseo.Estado == "Pendiente" && nuevoEstado == "EnCurso") ||
                (paseo.Estado == "EnCurso" && nuevoEstado == "Finalizado");

            if (!transicionValida)
                return;

            // Si es programado, solo puede iniciar 15 min antes de la hora programada
            if (paseo.Estado == "Pendiente" &&
                nuevoEstado == "EnCurso" &&
                paseo.EsProgramado &&
                paseo.FechaProgramada.HasValue)
            {
                var ahoraUtc = DateTime.UtcNow;
                var fechaProgramadaUtc = paseo.FechaProgramada.Value;

                if (ahoraUtc < fechaProgramadaUtc.AddMinutes(-15))
                    return;
            }

            paseo.Estado = nuevoEstado;

            if (nuevoEstado == "EnCurso")
            {
                paseo.FechaInicio = DateTime.UtcNow;
            }
            else if (nuevoEstado == "Finalizado")
            {
                paseo.FechaFin = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await Clients.Group(paseoId.ToString())
                .SendAsync("EstadoCambiado", nuevoEstado);
        }

        // Unirse al grupo del paseo al conectarse
        public async Task UnirseAPaseo(int paseoId)
        {
            var usuarioIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return;

            var usuarioId = int.Parse(usuarioIdClaim);

            var paseo = await _context.Paseos
                .Include(p => p.Perro)
                .Include(p => p.Paseador)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null || paseo.Perro == null || paseo.Paseador == null)
                return;

            var esDuenio = paseo.Perro.DueñoId == usuarioId;
            var esPaseador = false;

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador != null)
                esPaseador = paseo.PaseadorId == paseador.Id;

            if (!esDuenio && !esPaseador)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, paseoId.ToString());
        }
    }
}