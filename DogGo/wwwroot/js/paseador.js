// ═══════════════════════════════════════════════
// DogGo — paseador.js
// Modal solicitar paseo, precio calculado, zonas, preview foto y mapa de recolección
// ═══════════════════════════════════════════════

let tarifaActualPaseador = 0;
let mapaRecogida = null;
let marcadorRecogida = null;

const centroDefault = {
    lat: 25.6866,
    lng: -100.3161
};

// ── Modal solicitar paseo ───────────────────────
function abrirModal(paseadorId, nombre, tarifa) {
    tarifaActualPaseador = Number(String(tarifa).replace(',', '.')) || 0;

    const inputPaseadorId = document.getElementById('modal-paseador-id');
    const modalSub = document.getElementById('modal-sub');
    const duracionSelect = document.getElementById('modal-duracion');
    const overlay = document.getElementById('modal-overlay');

    if (!overlay) {
        console.error('No se encontró #modal-overlay. Revisa que el modal exista en Directorio.cshtml.');
        return;
    }

    if (inputPaseadorId) {
        inputPaseadorId.value = paseadorId;
    }

    if (modalSub) {
        modalSub.textContent = 'Con ' + nombre + ' · $' + tarifaActualPaseador.toFixed(2) + '/hora';
    }

    if (duracionSelect && !duracionSelect.value) {
        duracionSelect.value = '30';
    }

    recalcularPrecioEstimado();

    overlay.style.display = 'flex';
    document.body.style.overflow = 'hidden';

    setTimeout(function () {
        inicializarMapaRecogida();
    }, 150);
}

function cerrarModal() {
    const overlay = document.getElementById('modal-overlay');

    if (overlay) {
        overlay.style.display = 'none';
    }

    document.body.style.overflow = '';
}

function cerrarModalFuera(e) {
    const overlay = document.getElementById('modal-overlay');

    if (overlay && e.target === overlay) {
        cerrarModal();
    }
}

function recalcularPrecioEstimado() {
    const precioInput = document.getElementById('modal-precio');
    const duracionSelect = document.getElementById('modal-duracion');
    const ayudaPrecio = document.getElementById('modal-precio-help');

    if (!precioInput || !duracionSelect) return;

    const duracion = Number(duracionSelect.value || 0);
    const precio = tarifaActualPaseador * (duracion / 60);

    if (duracion <= 0 || tarifaActualPaseador <= 0) {
        precioInput.value = '$0.00';

        if (ayudaPrecio) {
            ayudaPrecio.textContent = 'Selecciona duración para calcular el precio.';
        }

        return;
    }

    precioInput.value = '$' + precio.toFixed(2);

    if (ayudaPrecio) {
        ayudaPrecio.textContent =
            'Tarifa: $' + tarifaActualPaseador.toFixed(2) +
            '/hora · Duración: ' + duracion +
            ' min · Total: $' + precio.toFixed(2);
    }
}

// ── Botones que abren modal ─────────────────────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.js-abrir-modal').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const paseadorId = btn.dataset.paseadorId;
            const nombre = btn.dataset.nombre || 'Paseador';
            const tarifa = btn.dataset.tarifa || '0';

            abrirModal(paseadorId, nombre, tarifa);
        });
    });
});

// ── Tipo de paseo: mostrar/ocultar fecha ────────
document.addEventListener('DOMContentLoaded', function () {
    const radios = document.querySelectorAll('.tipo-radio');
    const fechaWrap = document.getElementById('fecha-wrap');
    const inputFecha = document.getElementById('input-fecha');

    if (!radios.length) return;

    radios.forEach(function (r) {
        r.addEventListener('change', function () {
            const prog = document.querySelector('.tipo-radio[value="true"]');

            if (prog && prog.checked) {
                if (fechaWrap) fechaWrap.style.display = 'block';
                if (inputFecha) inputFecha.required = true;
            } else {
                if (fechaWrap) fechaWrap.style.display = 'none';

                if (inputFecha) {
                    inputFecha.required = false;
                    inputFecha.value = '';
                }
            }
        });
    });
});

// ── Precio estimado por duración ─────────────────
document.addEventListener('DOMContentLoaded', function () {
    const duracionSelect = document.getElementById('modal-duracion');

    if (duracionSelect) {
        duracionSelect.addEventListener('change', recalcularPrecioEstimado);
    }
});

// ── Selección de perros ──────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const checks = document.querySelectorAll('.perro-check');
    const resumen = document.getElementById('perros-seleccionados');

    if (!checks.length || !resumen) return;

    function actualizarPerrosSeleccionados() {
        const seleccionados = Array.from(document.querySelectorAll('.perro-check:checked'));

        const nombres = seleccionados.map(function (chk) {
            const card = chk.closest('.perro-option');
            const nombre = card ? card.querySelector('.perro-option-name') : null;
            return nombre ? nombre.textContent.trim() : '';
        }).filter(Boolean);

        if (!nombres.length) {
            resumen.textContent = 'Ningún perro seleccionado.';
        } else if (nombres.length === 1) {
            resumen.textContent = 'Seleccionado: ' + nombres[0];
        } else {
            resumen.textContent = 'Seleccionados: ' + nombres.join(', ');
        }
    }

    checks.forEach(function (chk) {
        chk.addEventListener('change', actualizarPerrosSeleccionados);
    });

    actualizarPerrosSeleccionados();
});

// ── Validación del formulario de solicitud ───────
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('modal-form');

    if (!form) return;

    form.addEventListener('submit', function (e) {
        const perrosSeleccionados = document.querySelectorAll('.perro-check:checked');
        const lat = document.getElementById('latitud-recogida');
        const lng = document.getElementById('longitud-recogida');
        const duracion = document.getElementById('modal-duracion');

        if (!perrosSeleccionados.length) {
            e.preventDefault();
            alert('Selecciona al menos un perro para el paseo.');
            return;
        }

        if (!duracion || !duracion.value) {
            e.preventDefault();
            alert('Selecciona la duración del paseo.');
            return;
        }

        if (!lat || !lng || !lat.value || !lng.value) {
            e.preventDefault();
            alert('Marca el punto de recolección en el mapa.');
            return;
        }
    });
});

// ── Mapa de recolección ──────────────────────────
function inicializarMapaRecogida() {
    const mapDiv = document.getElementById('mapa-recogida');

    if (!mapDiv) return;

    if (typeof L === 'undefined') {
        console.error('Leaflet no está cargado. Revisa el script de Leaflet en Directorio.cshtml.');
        return;
    }

    if (!mapaRecogida) {
        mapaRecogida = L.map('mapa-recogida').setView([centroDefault.lat, centroDefault.lng], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(mapaRecogida);

        mapaRecogida.on('click', function (e) {
            colocarPuntoRecogida(e.latlng.lat, e.latlng.lng, true);
        });
    }

    setTimeout(function () {
        mapaRecogida.invalidateSize();
    }, 200);
}

function colocarPuntoRecogida(lat, lng, centrarMapa) {
    const latInput = document.getElementById('latitud-recogida');
    const lngInput = document.getElementById('longitud-recogida');
    const texto = document.getElementById('texto-ubicacion-recogida');

    lat = Number(lat);
    lng = Number(lng);

    if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;

    if (latInput) latInput.value = lat.toFixed(7);
    if (lngInput) lngInput.value = lng.toFixed(7);

    if (mapaRecogida) {
        if (!marcadorRecogida) {
            marcadorRecogida = L.marker([lat, lng], { draggable: true }).addTo(mapaRecogida);

            marcadorRecogida.on('dragend', function () {
                const pos = marcadorRecogida.getLatLng();
                colocarPuntoRecogida(pos.lat, pos.lng, false);
            });
        } else {
            marcadorRecogida.setLatLng([lat, lng]);
        }

        if (centrarMapa) {
            mapaRecogida.setView([lat, lng], 16);
        }
    }

    if (texto) {
        texto.textContent = 'Punto de recolección marcado correctamente.';
    }
}

function usarUbicacionActual() {
    inicializarMapaRecogida();

    if (!navigator.geolocation) {
        alert('Tu navegador no permite obtener la ubicación actual.');
        return;
    }

    navigator.geolocation.getCurrentPosition(
        function (pos) {
            const lat = pos.coords.latitude;
            const lng = pos.coords.longitude;

            colocarPuntoRecogida(lat, lng, true);
        },
        function () {
            alert('No se pudo obtener tu ubicación. Puedes marcar el punto manualmente en el mapa.');
        },
        {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 0
        }
    );
}

function usarUbicacionPredeterminada(btn) {
    if (!btn) return;

    inicializarMapaRecogida();

    const direccion = btn.dataset.direccion || '';
    const referencias = btn.dataset.referencias || '';
    const zona = btn.dataset.zona || '';
    const lat = btn.dataset.lat;
    const lng = btn.dataset.lng;

    const direccionInput = document.getElementById('direccion-recogida');
    const referenciasInput = document.getElementById('referencias-recogida');
    const zonaSelect = document.getElementById('zona-recogida');

    if (direccionInput) direccionInput.value = direccion;
    if (referenciasInput) referenciasInput.value = referencias;
    if (zonaSelect) zonaSelect.value = zona;

    colocarPuntoRecogida(lat, lng, true);
}

// ── Zonas de servicio: sincronizar hidden input ──
function actualizarZonas() {
    const checks = document.querySelectorAll('.zona-check:checked');
    const valores = Array.from(checks).map(function (c) { return c.value; });
    const hidden = document.getElementById('zonaServicio');

    if (hidden) hidden.value = valores.join(', ');
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.zona-check').forEach(function (chk) {
        chk.addEventListener('change', actualizarZonas);
    });

    actualizarZonas();
});

// ── Preview foto paseador ───────────────────────
function previewFotoPaseador(input) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();

        reader.onload = function (e) {
            let img = document.querySelector('.foto-preview-img');
            let ph = document.querySelector('.foto-placeholder');

            if (!img) {
                img = document.createElement('img');
                img.className = 'foto-preview-img';

                if (ph) {
                    ph.replaceWith(img);
                } else {
                    const formSection = document.querySelector('.form-section');
                    if (formSection) formSection.prepend(img);
                }
            }

            img.src = e.target.result;
        };

        reader.readAsDataURL(input.files[0]);
    }
}

// ── Exponer funciones globales para botones inline ─
window.abrirModal = abrirModal;
window.cerrarModal = cerrarModal;
window.cerrarModalFuera = cerrarModalFuera;
window.usarUbicacionActual = usarUbicacionActual;
window.usarUbicacionPredeterminada = usarUbicacionPredeterminada;
window.previewFotoPaseador = previewFotoPaseador;