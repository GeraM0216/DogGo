// ═══════════════════════════════════════════════
// DogGo — paseador.js
// Modal solicitar paseo, zonas checkbox, preview foto
// ═══════════════════════════════════════════════

// ── Modal solicitar paseo ───────────────────────
function abrirModal(paseadorId, nombre, tarifa) {
    document.getElementById('modal-paseador-id').value = paseadorId;
    document.getElementById('modal-sub').textContent = 'Con ' + nombre + ' · $' + tarifa + '/hora';
    document.getElementById('modal-precio').value = tarifa;
    document.getElementById('modal-overlay').style.display = 'flex';
    document.body.style.overflow = 'hidden';
}

function cerrarModal() {
    document.getElementById('modal-overlay').style.display = 'none';
    document.body.style.overflow = '';
}

function cerrarModalFuera(e) {
    if (e.target === document.getElementById('modal-overlay')) cerrarModal();
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
                if (inputFecha) { inputFecha.required = false; inputFecha.value = ''; }
            }
        });
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
                if (ph) ph.replaceWith(img);
                else document.querySelector('.form-section').prepend(img);
            }
            img.src = e.target.result;
        };
        reader.readAsDataURL(input.files[0]);
    }
}