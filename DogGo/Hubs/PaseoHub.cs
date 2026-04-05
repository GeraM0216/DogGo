using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
            var usuarioIdClaim = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            var usuarioIdClaim = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
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

            // Solo el paseador asignado a este paseo puede cambiar su estado
            if (paseo.PaseadorId != paseador.Id)
                return;

            // Validar transiciones permitidas
            var estadoActual = paseo.Estado;

            var transicionValida =
                (estadoActual == "Pendiente" && nuevoEstado == "EnCurso") ||
                (estadoActual == "EnCurso" && nuevoEstado == "Finalizado");

            if (!transicionValida)
                return;

            paseo.Estado = nuevoEstado;

            if (nuevoEstado == "EnCurso")
                paseo.FechaInicio = DateTime.UtcNow;
            else if (nuevoEstado == "Finalizado")
                paseo.FechaFin = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await Clients.Group(paseoId.ToString())
                .SendAsync("EstadoCambiado", nuevoEstado);
        }






        // Unirse al grupo del paseo al conectarse
        public async Task UnirseAPaseo(int paseoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, paseoId.ToString());
        }
    }
}