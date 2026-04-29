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

            var paseo = await _context.Paseos
                .Include(p => p.Paseador)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null || paseo.Paseador == null)
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
                {
                    await Clients.Caller.SendAsync(
                        "AccionPaseoRechazada",
                        "Este paseo programado solo puede iniciarse 15 minutos antes de la hora programada."
                    );

                    return;
                }
            }

            if (nuevoEstado == "EnCurso")
            {
                if (string.IsNullOrWhiteSpace(paseo.FotoInicioUrl))
                {
                    await Clients.Caller.SendAsync(
                        "AccionPaseoRechazada",
                        "Debes subir una foto de inicio antes de iniciar el paseo."
                    );

                    return;
                }

                paseo.Estado = "EnCurso";
                paseo.FechaInicio = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await Clients.Group(paseoId.ToString())
                    .SendAsync("EstadoCambiado", "EnCurso");

                return;
            }

            if (nuevoEstado == "Finalizado")
            {
                if (string.IsNullOrWhiteSpace(paseo.FotoFinUrl))
                {
                    await Clients.Caller.SendAsync(
                        "AccionPaseoRechazada",
                        "Debes subir una foto final antes de finalizar el paseo."
                    );

                    return;
                }

                if (paseo.FechaInicio == null)
                {
                    await Clients.Caller.SendAsync(
                        "AccionPaseoRechazada",
                        "No se puede finalizar un paseo que no tiene hora de inicio."
                    );

                    return;
                }

                var minutosReales = CalcularMinutosReales(paseo.FechaInicio.Value, DateTime.UtcNow);

                if (minutosReales < paseo.DuracionMinutos)
                {
                    var precioEstimado = CalcularPrecioAnticipado(
                        paseo.Paseador.TarifaPorHora,
                        minutosReales,
                        paseo.DuracionMinutos
                    );

                    await Clients.Caller.SendAsync(
                        "FinalizacionAnticipadaRequerida",
                        minutosReales,
                        paseo.DuracionMinutos,
                        precioEstimado
                    );

                    return;
                }

                FinalizarPaseoNormal(paseo);

                await _context.SaveChangesAsync();

                await Clients.Group(paseoId.ToString())
                    .SendAsync("EstadoCambiado", "Finalizado");

                return;
            }
        }

        // El paseador solicita terminar antes del tiempo acordado
        public async Task SolicitarFinalizacionAnticipada(int paseoId, string motivo)
        {
            var usuarioIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return;

            var usuarioId = int.Parse(usuarioIdClaim);

            motivo = string.IsNullOrWhiteSpace(motivo) ? string.Empty : motivo.Trim();

            if (motivo.Length < 5)
            {
                await Clients.Caller.SendAsync(
                    "AccionPaseoRechazada",
                    "Debes escribir un motivo válido para solicitar la finalización anticipada."
                );

                return;
            }

            var paseo = await _context.Paseos
                .Include(p => p.Paseador)
                .Include(p => p.Perro)
                .Include(p => p.PaseoPerros)
                    .ThenInclude(pp => pp.Perro)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null || paseo.Paseador == null || paseo.Perro == null)
                return;

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador == null || paseo.PaseadorId != paseador.Id)
                return;

            if (paseo.Estado != "EnCurso")
            {
                await Clients.Caller.SendAsync(
                    "AccionPaseoRechazada",
                    "Solo puedes solicitar finalización anticipada si el paseo está en curso."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(paseo.FotoFinUrl))
            {
                await Clients.Caller.SendAsync(
                    "AccionPaseoRechazada",
                    "Primero sube la foto final antes de solicitar la finalización anticipada."
                );

                return;
            }

            if (paseo.FechaInicio == null)
            {
                await Clients.Caller.SendAsync(
                    "AccionPaseoRechazada",
                    "No se puede solicitar finalización porque el paseo no tiene hora de inicio."
                );

                return;
            }

            var minutosReales = CalcularMinutosReales(paseo.FechaInicio.Value, DateTime.UtcNow);

            if (minutosReales >= paseo.DuracionMinutos)
            {
                FinalizarPaseoNormal(paseo);
                await _context.SaveChangesAsync();

                await Clients.Group(paseoId.ToString())
                    .SendAsync("EstadoCambiado", "Finalizado");

                return;
            }

            var precioEstimado = CalcularPrecioAnticipado(
                paseo.Paseador.TarifaPorHora,
                minutosReales,
                paseo.DuracionMinutos
            );

            paseo.FinalizacionAnticipadaSolicitada = true;
            paseo.MotivoFinalizacionAnticipada = motivo;
            paseo.FechaSolicitudFinalizacionAnticipada = DateTime.UtcNow;
            paseo.FinalizacionAnticipadaAprobada = null;
            paseo.FechaRespuestaFinalizacionAnticipada = null;

            await _context.SaveChangesAsync();

            await Clients.Group(paseoId.ToString())
                .SendAsync(
                    "SolicitudFinalizacionAnticipada",
                    minutosReales,
                    paseo.DuracionMinutos,
                    precioEstimado,
                    motivo
                );
        }

        // El dueño acepta o rechaza la solicitud de terminar antes
        public async Task ResponderFinalizacionAnticipada(int paseoId, bool aprobar)
        {
            var usuarioIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return;

            var usuarioId = int.Parse(usuarioIdClaim);

            var paseo = await _context.Paseos
                .Include(p => p.Paseador)
                .Include(p => p.Perro)
                .Include(p => p.PaseoPerros)
                    .ThenInclude(pp => pp.Perro)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null || paseo.Paseador == null || paseo.Perro == null)
                return;

            var esDuenio = paseo.Perro.DueñoId == usuarioId ||
                           paseo.PaseoPerros.Any(pp => pp.Perro != null && pp.Perro.DueñoId == usuarioId);

            if (!esDuenio)
                return;

            if (paseo.Estado != "EnCurso")
                return;

            if (!paseo.FinalizacionAnticipadaSolicitada)
                return;

            paseo.FinalizacionAnticipadaAprobada = aprobar;
            paseo.FechaRespuestaFinalizacionAnticipada = DateTime.UtcNow;

            if (aprobar)
            {
                FinalizarPaseoAnticipado(paseo);

                await _context.SaveChangesAsync();

                await Clients.Group(paseoId.ToString())
                    .SendAsync("FinalizacionAnticipadaRespondida", true);

                await Clients.Group(paseoId.ToString())
                    .SendAsync("EstadoCambiado", "Finalizado");

                return;
            }

            paseo.FinalizacionAnticipadaSolicitada = false;

            await _context.SaveChangesAsync();

            await Clients.Group(paseoId.ToString())
                .SendAsync("FinalizacionAnticipadaRespondida", false);
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
                .Include(p => p.PaseoPerros)
                    .ThenInclude(pp => pp.Perro)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null || paseo.Perro == null || paseo.Paseador == null)
                return;

            var esDuenio = paseo.Perro.DueñoId == usuarioId ||
                           paseo.PaseoPerros.Any(pp => pp.Perro != null && pp.Perro.DueñoId == usuarioId);

            var esPaseador = false;

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador != null)
                esPaseador = paseo.PaseadorId == paseador.Id;

            if (!esDuenio && !esPaseador)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, paseoId.ToString());
        }

        private static void FinalizarPaseoNormal(Paseo paseo)
        {
            var ahoraUtc = DateTime.UtcNow;
            var minutosReales = paseo.FechaInicio.HasValue
                ? CalcularMinutosReales(paseo.FechaInicio.Value, ahoraUtc)
                : paseo.DuracionMinutos;

            paseo.Estado = "Finalizado";
            paseo.FechaFin = ahoraUtc;
            paseo.DuracionRealMinutos = minutosReales;

            // Si cumplió o superó la duración solicitada, se cobra lo acordado.
            paseo.Precio = CalcularPrecio(paseo.Paseador.TarifaPorHora, paseo.DuracionMinutos);

            paseo.FinalizacionAnticipadaSolicitada = false;
            paseo.FinalizacionAnticipadaAprobada = null;
            paseo.FechaRespuestaFinalizacionAnticipada = null;
        }

        private static void FinalizarPaseoAnticipado(Paseo paseo)
        {
            var ahoraUtc = DateTime.UtcNow;
            var minutosReales = paseo.FechaInicio.HasValue
                ? CalcularMinutosReales(paseo.FechaInicio.Value, ahoraUtc)
                : paseo.DuracionMinutos;

            paseo.Estado = "Finalizado";
            paseo.FechaFin = ahoraUtc;
            paseo.DuracionRealMinutos = minutosReales;

            // Si termina antes, se recalcula con la duración real.
            // Mínimo se cobran 15 minutos para evitar precios absurdamente bajos.
            var minutosCobrados = Math.Min(minutosReales, paseo.DuracionMinutos);
            minutosCobrados = Math.Max(minutosCobrados, 15);

            paseo.Precio = CalcularPrecio(paseo.Paseador.TarifaPorHora, minutosCobrados);
        }

        private static int CalcularMinutosReales(DateTime fechaInicioUtc, DateTime fechaFinUtc)
        {
            var minutos = (int)Math.Ceiling((fechaFinUtc - fechaInicioUtc).TotalMinutes);
            return Math.Max(minutos, 1);
        }

        private static decimal CalcularPrecioAnticipado(decimal tarifaPorHora, int minutosReales, int duracionSolicitada)
        {
            var minutosCobrados = Math.Min(minutosReales, duracionSolicitada);
            minutosCobrados = Math.Max(minutosCobrados, 15);

            return CalcularPrecio(tarifaPorHora, minutosCobrados);
        }

        private static decimal CalcularPrecio(decimal tarifaPorHora, int minutos)
        {
            return Math.Round(
                tarifaPorHora * (minutos / 60m),
                2,
                MidpointRounding.AwayFromZero
            );
        }
    }
}