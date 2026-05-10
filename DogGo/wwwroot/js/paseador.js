// ═══════════════════════════════════════════════
// DogGo — paseador.js  v2  Agency-tier
// Modal · Mapa · Geolocation · Micro-interactions
// ═══════════════════════════════════════════════

let tarifaActualPaseador = 0;
let mapaRecogida = null;
let marcadorRecogida = null;

const centroDefault = { lat: 25.6866, lng: -100.3161 };

/* ══════════════════════════════════════════════
   TOAST de feedback (reemplaza alert())
══════════════════════════════════════════════ */
function doggoToast(msg, tipo = 'info') {
    const colors = {
        info:    { bg: '#E0F5F3', border: '#0F9B8E', text: '#0d8a7e', icon: 'ℹ️' },
        success: { bg: '#e8f8f3', border: '#0F9B8E', text: '#0d7060', icon: '✅' },
        error:   { bg: '#FFF0EB', border: '#FF7043', text: '#c04020', icon: '❌' },
        warning: { bg: '#FFF3DC', border: '#F5A623', text: '#b07800', icon: '⚠️' },
        loading: { bg: '#f5f0e8', border: '#EDE8E0', text: '#2D3142', icon: '⏳' },
    };
    const c = colors[tipo] || colors.info;

    // Remover toast previo del mismo tipo
    document.querySelectorAll('.doggo-toast').forEach(t => t.remove());

    const toast = document.createElement('div');
    toast.className = 'doggo-toast';
    toast.style.cssText = `
        position:fixed; bottom:90px; left:50%; transform:translateX(-50%) translateY(20px);
        background:${c.bg}; border:1.5px solid ${c.border}; color:${c.text};
        padding:12px 20px; border-radius:999px; font-family:'Nunito',sans-serif;
        font-weight:800; font-size:.88rem; z-index:9999; white-space:nowrap;
        box-shadow:0 8px 30px rgba(45,49,66,.15); opacity:0;
        transition:opacity .3s cubic-bezier(0.22,1,0.36,1), transform .3s cubic-bezier(0.34,1.56,0.64,1);
        display:flex; align-items:center; gap:8px;
    `;
    toast.innerHTML = `<span>${c.icon}</span><span>${msg}</span>`;
    document.body.appendChild(toast);

    requestAnimationFrame(() => {
        toast.style.opacity = '1';
        toast.style.transform = 'translateX(-50%) translateY(0)';
    });

    if (tipo !== 'loading') {
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(-50%) translateY(10px)';
            setTimeout(() => toast.remove(), 350);
        }, 3200);
    }

    return toast;
}

/* ══════════════════════════════════════════════
   MODAL — Abrir / Cerrar
══════════════════════════════════════════════ */
function abrirModal(paseadorId, nombre, tarifa) {
    tarifaActualPaseador = Number(String(tarifa).replace(',', '.')) || 0;

    const overlay      = document.getElementById('modal-overlay');
    const inputId      = document.getElementById('modal-paseador-id');
    const modalSub     = document.getElementById('modal-sub');
    const duracionSel  = document.getElementById('modal-duracion');

    if (!overlay) { console.error('No se encontró #modal-overlay'); return; }

    if (inputId)     inputId.value = paseadorId;
    if (modalSub)    modalSub.textContent = `Con ${nombre} · $${tarifaActualPaseador.toFixed(2)}/hora`;
    if (duracionSel && !duracionSel.value) duracionSel.value = '30';

    recalcularPrecioEstimado();

    overlay.style.display = 'flex';
    document.body.style.overflow = 'hidden';

    // Pequeño delay para que el modal esté visible antes de inicializar el mapa
    setTimeout(inicializarMapaRecogida, 250);
}

function cerrarModal() {
    const overlay = document.getElementById('modal-overlay');
    if (!overlay) return;
    overlay.style.opacity = '0';
    setTimeout(() => {
        overlay.style.display = 'none';
        overlay.style.opacity = '';
        document.body.style.overflow = '';
    }, 200);
}

function cerrarModalFuera(e) {
    if (e.target === document.getElementById('modal-overlay')) cerrarModal();
}

/* ══════════════════════════════════════════════
   PRECIO ESTIMADO
══════════════════════════════════════════════ */
function recalcularPrecioEstimado() {
    const precioInput  = document.getElementById('modal-precio');
    const duracionSel  = document.getElementById('modal-duracion');
    const ayudaPrecio  = document.getElementById('modal-precio-help');

    if (!precioInput || !duracionSel) return;

    const duracion = Number(duracionSel.value || 0);
    const precio   = tarifaActualPaseador * (duracion / 60);

    if (duracion <= 0 || tarifaActualPaseador <= 0) {
        precioInput.value = '$0.00';
        if (ayudaPrecio) ayudaPrecio.textContent = 'Selecciona duración para calcular el precio.';
        return;
    }

    // Animación de contador al cambiar precio
    animarPrecio(precioInput, precio);

    if (ayudaPrecio) {
        ayudaPrecio.textContent =
            `Tarifa: $${tarifaActualPaseador.toFixed(2)}/hora · Duración: ${duracion} min · Total: $${precio.toFixed(2)}`;
    }
}

function animarPrecio(input, target) {
    const start     = parseFloat(input.value.replace('$', '')) || 0;
    const duration  = 400;
    const startTime = performance.now();

    function tick(now) {
        const t = Math.min((now - startTime) / duration, 1);
        const eased = 1 - Math.pow(1 - t, 3);
        input.value = '$' + (start + (target - start) * eased).toFixed(2);
        if (t < 1) requestAnimationFrame(tick);
    }
    requestAnimationFrame(tick);
}

/* ══════════════════════════════════════════════
   MAPA DE RECOLECCIÓN — Leaflet
══════════════════════════════════════════════ */
function inicializarMapaRecogida() {
    const mapDiv = document.getElementById('mapa-recogida');
    if (!mapDiv) return;

    if (typeof L === 'undefined') {
        console.error('Leaflet no está cargado.');
        return;
    }

    if (!mapaRecogida) {
        mapaRecogida = L.map('mapa-recogida', {
            zoomControl: true,
            attributionControl: false,
        }).setView([centroDefault.lat, centroDefault.lng], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19
        }).addTo(mapaRecogida);

        // Click en mapa para poner marcador
        mapaRecogida.on('click', e => colocarPuntoRecogida(e.latlng.lat, e.latlng.lng, false));
    }

    setTimeout(() => mapaRecogida.invalidateSize(), 250);
}

function colocarPuntoRecogida(lat, lng, centrar = true) {
    lat = Number(lat);
    lng = Number(lng);
    if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;

    const latInput = document.getElementById('latitud-recogida');
    const lngInput = document.getElementById('longitud-recogida');
    const texto    = document.getElementById('texto-ubicacion-recogida');

    if (latInput) latInput.value = lat.toFixed(7);
    if (lngInput) lngInput.value = lng.toFixed(7);

    if (mapaRecogida) {
        // Ícono custom teal
        const icon = L.divIcon({
            className: '',
            html: `<div style="
                width:36px;height:36px;border-radius:50% 50% 50% 0;
                background:#0F9B8E;border:3px solid #fff;
                box-shadow:0 4px 14px rgba(15,155,142,.4);
                transform:rotate(-45deg);
                display:flex;align-items:center;justify-content:center;
            "><span style="transform:rotate(45deg);font-size:14px;">🐾</span></div>`,
            iconSize:   [36, 36],
            iconAnchor: [18, 36],
        });

        if (!marcadorRecogida) {
            marcadorRecogida = L.marker([lat, lng], { draggable: true, icon }).addTo(mapaRecogida);
            marcadorRecogida.on('dragend', () => {
                const pos = marcadorRecogida.getLatLng();
                colocarPuntoRecogida(pos.lat, pos.lng, false);
            });
        } else {
            marcadorRecogida.setLatLng([lat, lng]);
        }

        if (centrar) mapaRecogida.flyTo([lat, lng], 16, { duration: 1.2 });
    }

    if (texto) {
        texto.innerHTML = `<span style="color:#0F9B8E;font-weight:800;">✅ Punto marcado</span> · ${lat.toFixed(4)}, ${lng.toFixed(4)}`;
    }
}

/* ══════════════════════════════════════════════
   GEOLOCATION — "Usar mi ubicación actual"
══════════════════════════════════════════════ */
function usarUbicacionActual() {
    inicializarMapaRecogida();

    if (!navigator.geolocation) {
        doggoToast('Tu navegador no soporta geolocalización', 'error');
        return;
    }

    const btnActual = document.querySelector('.btn-ubicacion-actual');
    const originalText = btnActual ? btnActual.textContent : '';
    const toast = doggoToast('Obteniendo tu ubicación...', 'loading');

    if (btnActual) {
        btnActual.textContent = '⏳ Buscando...';
        btnActual.disabled = true;
        btnActual.style.opacity = '.7';
    }

    navigator.geolocation.getCurrentPosition(
        pos => {
            toast.remove();
            doggoToast('¡Ubicación encontrada!', 'success');

            if (btnActual) {
                btnActual.textContent = originalText;
                btnActual.disabled = false;
                btnActual.style.opacity = '';
            }

            colocarPuntoRecogida(pos.coords.latitude, pos.coords.longitude, true);
        },
        err => {
            toast.remove();
            const msgs = {
                1: 'Permiso de ubicación denegado. Actívalo en tu navegador.',
                2: 'No se pudo obtener tu posición. Marca el punto en el mapa.',
                3: 'Tiempo de espera agotado. Intenta de nuevo.',
            };
            doggoToast(msgs[err.code] || 'Error al obtener ubicación', 'error');

            if (btnActual) {
                btnActual.textContent = originalText;
                btnActual.disabled = false;
                btnActual.style.opacity = '';
            }
        },
        { enableHighAccuracy: true, timeout: 12000, maximumAge: 0 }
    );
}

/* ══════════════════════════════════════════════
   UBICACIÓN PREDETERMINADA
══════════════════════════════════════════════ */
function usarUbicacionPredeterminada(btn) {
    if (!btn) return;
    inicializarMapaRecogida();

    const d = document.getElementById('direccion-recogida');
    const r = document.getElementById('referencias-recogida');
    const z = document.getElementById('zona-recogida');

    if (d) d.value = btn.dataset.direccion || '';
    if (r) r.value = btn.dataset.referencias || '';
    if (z) z.value = btn.dataset.zona || '';

    colocarPuntoRecogida(btn.dataset.lat, btn.dataset.lng, true);
    doggoToast('Ubicación predeterminada cargada ✓', 'success');
}

/* ══════════════════════════════════════════════
   SELECCIÓN DE PERROS — con animación de check
══════════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', () => {
    // Abrir modal via data attributes
    document.querySelectorAll('.js-abrir-modal').forEach(btn => {
        btn.addEventListener('click', () =>
            abrirModal(btn.dataset.paseadorId, btn.dataset.nombre || 'Paseador', btn.dataset.tarifa || '0')
        );
    });

    // Tipo paseo → mostrar/ocultar fecha
    const fechaWrap  = document.getElementById('fecha-wrap');
    const inputFecha = document.getElementById('input-fecha');
    document.querySelectorAll('.tipo-radio').forEach(r => {
        r.addEventListener('change', () => {
            const prog = document.querySelector('.tipo-radio[value="true"]');
            const show = prog && prog.checked;
            if (fechaWrap) {
                fechaWrap.style.maxHeight = show ? '120px' : '0';
                fechaWrap.style.overflow  = 'hidden';
                fechaWrap.style.transition = 'max-height .35s cubic-bezier(0.22,1,0.36,1)';
                if (!show && inputFecha) { inputFecha.required = false; inputFecha.value = ''; }
                if (show && inputFecha)  { inputFecha.required = true; }
            }
        });
    });
    // Estado inicial del fecha-wrap
    if (fechaWrap) { fechaWrap.style.maxHeight = '0'; fechaWrap.style.overflow = 'hidden'; }

    // Duración → recalcular precio
    const durSel = document.getElementById('modal-duracion');
    if (durSel) durSel.addEventListener('change', recalcularPrecioEstimado);

    // Perros seleccionados
    const resumen = document.getElementById('perros-seleccionados');
    document.querySelectorAll('.perro-check').forEach(chk => {
        chk.addEventListener('change', () => {
            // Animación en la card
            const body = chk.closest('.perro-option')?.querySelector('.perro-option-body');
            if (body) {
                body.style.transform = 'scale(0.97)';
                setTimeout(() => body.style.transform = '', 150);
            }
            actualizarResumenPerros(resumen);
        });
    });
    actualizarResumenPerros(resumen);

    // Zonas de servicio
    document.querySelectorAll('.zona-check').forEach(chk =>
        chk.addEventListener('change', actualizarZonas)
    );
    actualizarZonas();

    // Validación del form
    const form = document.getElementById('modal-form');
    if (form) {
        form.addEventListener('submit', e => {
            const perros  = document.querySelectorAll('.perro-check:checked');
            const lat     = document.getElementById('latitud-recogida');
            const lng     = document.getElementById('longitud-recogida');
            const durSel  = document.getElementById('modal-duracion');

            if (!perros.length) {
                e.preventDefault();
                doggoToast('Selecciona al menos un perro 🐶', 'warning');
                // Shake en la sección de perros
                const grid = document.querySelector('.perros-select-grid');
                if (grid) { grid.style.animation = 'shake .4s ease'; setTimeout(() => grid.style.animation = '', 400); }
                return;
            }
            if (!durSel?.value) {
                e.preventDefault();
                doggoToast('Selecciona la duración del paseo', 'warning');
                return;
            }
            if (!lat?.value || !lng?.value) {
                e.preventDefault();
                doggoToast('Marca el punto de recolección en el mapa 📍', 'warning');
                // Scroll suave al mapa
                document.getElementById('mapa-recogida')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                return;
            }
        });
    }
});

function actualizarResumenPerros(resumen) {
    if (!resumen) return;
    const sel = Array.from(document.querySelectorAll('.perro-check:checked'))
        .map(chk => chk.closest('.perro-option')?.querySelector('.perro-option-name')?.textContent?.trim())
        .filter(Boolean);

    if (!sel.length) {
        resumen.textContent = 'Ningún perro seleccionado.';
        resumen.style.color = '';
    } else {
        resumen.innerHTML = `<span style="color:#0F9B8E;font-weight:800;">✅</span> ${sel.length === 1 ? 'Seleccionado' : 'Seleccionados'}: ${sel.join(', ')}`;
    }
}

/* ══════════════════════════════════════════════
   ZONAS — sync hidden input
══════════════════════════════════════════════ */
function actualizarZonas() {
    const vals   = Array.from(document.querySelectorAll('.zona-check:checked')).map(c => c.value);
    const hidden = document.getElementById('zonaServicio');
    if (hidden) hidden.value = vals.join(', ');
}

/* ══════════════════════════════════════════════
   PREVIEW FOTO PASEADOR
══════════════════════════════════════════════ */
function previewFotoPaseador(input) {
    if (!input.files?.[0]) return;
    const reader = new FileReader();
    reader.onload = e => {
        let img = document.querySelector('.foto-preview-img');
        let ph  = document.querySelector('.foto-placeholder');
        if (!img) {
            img = document.createElement('img');
            img.className = 'foto-preview-img';
            img.style.cssText = 'width:100%;height:100%;object-fit:cover;border-radius:50%;';
            ph ? ph.replaceWith(img) : document.querySelector('.form-section')?.prepend(img);
        }
        img.src = e.target.result;
        img.style.animation = 'scaleIn .35s cubic-bezier(0.34,1.56,0.64,1)';
    };
    reader.readAsDataURL(input.files[0]);
}

/* ══════════════════════════════════════════════
   KEYFRAMES extras (shake, scaleIn)
══════════════════════════════════════════════ */
const extraStyles = document.createElement('style');
extraStyles.textContent = `
    @keyframes shake {
        0%,100%{transform:translateX(0)}
        20%{transform:translateX(-6px)}
        40%{transform:translateX(6px)}
        60%{transform:translateX(-4px)}
        80%{transform:translateX(4px)}
    }
    @keyframes scaleIn {
        from{opacity:0;transform:scale(.85)}
        to{opacity:1;transform:scale(1)}
    }
    #mapa-recogida {
        height: 280px;
        border-radius: 18px;
        border: 1.5px solid #EDE8E0;
        margin-top: .5rem;
        overflow: hidden;
        box-shadow: 0 4px 16px rgba(45,49,66,.08);
        transition: box-shadow .25s ease;
    }
    #mapa-recogida:hover { box-shadow: 0 8px 30px rgba(45,49,66,.13); }
    .btn-ubicacion-actual, .btn-ubicacion-predeterminada {
        display: flex; align-items: center; justify-content: center; gap: .5rem;
        width: 100%; padding: .65rem 1rem; border-radius: 12px;
        font-family: 'Nunito', sans-serif; font-size: .85rem; font-weight: 800;
        cursor: pointer; margin-bottom: .5rem;
        transition: transform .2s cubic-bezier(0.34,1.56,0.64,1),
                    box-shadow .2s ease, background .18s ease;
        border: 1.5px solid #EDE8E0;
    }
    .btn-ubicacion-actual {
        background: #0F9B8E; color: white;
        box-shadow: 0 4px 14px rgba(15,155,142,.25);
        border-color: #0F9B8E;
    }
    .btn-ubicacion-actual:hover {
        transform: translateY(-2px); box-shadow: 0 8px 24px rgba(15,155,142,.35);
    }
    .btn-ubicacion-actual:active { transform: scale(.96); }
    .btn-ubicacion-predeterminada {
        background: #FFF6ED; color: #2D3142;
    }
    .btn-ubicacion-predeterminada:hover {
        transform: translateY(-2px); border-color: #F5A623;
        box-shadow: 0 6px 18px rgba(245,166,35,.2);
    }
    .perros-seleccionados {
        font-size: .8rem; font-weight: 700; color: #7B8194;
        padding: .4rem .6rem; border-radius: 8px;
        background: #FFF6ED; margin-top: .35rem;
        transition: all .25s ease;
    }
    .texto-ubicacion-recogida {
        font-size: .78rem; font-weight: 700; color: #7B8194;
        margin-top: .4rem; transition: color .3s ease;
    }
`;
document.head.appendChild(extraStyles);

/* ══════════════════════════════════════════════
   GLOBALS para inline onclick en .cshtml
══════════════════════════════════════════════ */
window.abrirModal                = abrirModal;
window.cerrarModal               = cerrarModal;
window.cerrarModalFuera          = cerrarModalFuera;
window.usarUbicacionActual       = usarUbicacionActual;
window.usarUbicacionPredeterminada = usarUbicacionPredeterminada;
window.previewFotoPaseador       = previewFotoPaseador;
