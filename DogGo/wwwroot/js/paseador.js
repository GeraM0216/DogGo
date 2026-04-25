// ═══════════════════════════════════════════════
// DogGo — paseador.js
// Modal solicitar paseo, ubicación de recolección, zonas checkbox, preview foto
// ═══════════════════════════════════════════════

let mapaRecogida = null;
let marcadorRecogida = null;

const ubicacionDefault = {
    lat: 25.6866,
    lng: -100.3161
};

// ── Modal solicitar paseo ───────────────────────
function abrirModal(paseadorId, nombre, tarifa) {
    const inputPaseador = document.getElementById('modal-paseador-id');
    const modalSub = document.getElementById('modal-sub');
    const inputPrecio = document.getElementById('modal-precio');
    const overlay = document.getElementById('modal-overlay');

    if (!inputPaseador || !modalSub || !inputPrecio || !overlay) {
        console.error('No se encontraron elementos del modal.');
        return;
    }

    inputPaseador.value = paseadorId;
    modalSub.textContent = 'Con ' + nombre + ' · $' + tarifa + '/hora';
    inputPrecio.value = tarifa;

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
    if (e.target === document.getElementById('modal-overlay')) {
        cerrarModal();
    }
}

// ── Mapa de recolección ─────────────────────────
function inicializarMapaRecogida() {
    const mapaDiv = document.getElementById('mapa-recogida');

    if (!mapaDiv) return;

    if (typeof L === 'undefined') {
        console.warn('Leaflet no está cargado. Revisa los scripts en Directorio.cshtml.');
        return;
    }

    if (!mapaRecogida) {
        mapaRecogida = L.map('mapa-recogida').setView([ubicacionDefault.lat, ubicacionDefault.lng], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap'
        }).addTo(mapaRecogida);

        mapaRecogida.on('click', function (e) {
            establecerPuntoRecogida(e.latlng.lat, e.latlng.lng);
        });
    }

    setTimeout(function () {
        mapaRecogida.invalidateSize();
    }, 200);
}

function establecerPuntoRecogida(lat, lng) {
    const latInput = document.getElementById('latitud-recogida');
    const lngInput = document.getElementById('longitud-recogida');
    const textoUbicacion = document.getElementById('texto-ubicacion-recogida');

    lat = Number(lat);
    lng = Number(lng);

    if (isNaN(lat) || isNaN(lng)) {
        console.error('Latitud o longitud no válida.');
        return;
    }

    if (latInput) latInput.value = lat.toFixed(8);
    if (lngInput) lngInput.value = lng.toFixed(8);

    if (mapaRecogida) {
        if (!marcadorRecogida) {
            marcadorRecogida = L.marker([lat, lng], {
                draggable: true
            }).addTo(mapaRecogida);

            marcadorRecogida.on('dragend', function () {
                const pos = marcadorRecogida.getLatLng();
                establecerPuntoRecogida(pos.lat, pos.lng);
            });
        } else {
            marcadorRecogida.setLatLng([lat, lng]);
        }

        mapaRecogida.setView([lat, lng], 16);
    }

    if (textoUbicacion) {
        textoUbicacion.textContent = 'Punto de recolección seleccionado. Puedes mover el marcador para ajustar la ubicación.';
    }
}

function usarUbicacionActual() {
    if (!navigator.geolocation) {
        alert('Tu navegador no permite obtener la ubicación actual.');
        return;
    }

    navigator.geolocation.getCurrentPosition(
        function (position) {
            const lat = position.coords.latitude;
            const lng = position.coords.longitude;

            establecerPuntoRecogida(lat, lng);
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
    const direccion = btn.dataset.direccion || '';
    const referencias = btn.dataset.referencias || '';
    const zona = btn.dataset.zona || '';
    const lat = parseFloat(btn.dataset.lat);
    const lng = parseFloat(btn.dataset.lng);

    const direccionInput = document.getElementById('direccion-recogida');
    const referenciasInput = document.getElementById('referencias-recogida');
    const zonaInput = document.getElementById('zona-recogida');

    if (direccionInput) direccionInput.value = direccion;
    if (referenciasInput) referenciasInput.value = referencias;
    if (zonaInput) zonaInput.value = zona;

    if (!isNaN(lat) && !isNaN(lng)) {
        establecerPuntoRecogida(lat, lng);
    } else {
        alert('Tu ubicación predeterminada no tiene coordenadas válidas.');
    }
}

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

// ── Validar formulario de solicitud de paseo ─────
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('modal-form');

    if (!form) return;

    form.addEventListener('submit', function (e) {
        const direccion = document.getElementById('direccion-recogida');
        const zona = document.getElementById('zona-recogida');
        const latitud = document.getElementById('latitud-recogida');
        const longitud = document.getElementById('longitud-recogida');

        if (direccion && direccion.value.trim() === '') {
            e.preventDefault();
            alert('Escribe la dirección de recolección.');
            direccion.focus();
            return;
        }

        if (zona && zona.value.trim() === '') {
            e.preventDefault();
            alert('Selecciona la zona de recolección.');
            zona.focus();
            return;
        }

        if (latitud && longitud && (latitud.value.trim() === '' || longitud.value.trim() === '')) {
            e.preventDefault();
            alert('Marca el punto de recolección en el mapa, usa tu ubicación actual o usa tu ubicación predeterminada.');
            return;
        }
    });
});

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
                    document.querySelector('.form-section').prepend(img);
                }
            }

            img.src = e.target.result;
        };

        reader.readAsDataURL(input.files[0]);
    }
}