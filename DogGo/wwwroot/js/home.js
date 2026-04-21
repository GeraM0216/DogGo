// ═══════════════════════════════════════════════
// DogGo — home.js
// Widget de mascotas, carrusel, contadores,
// accordion de razas
// ═══════════════════════════════════════════════

// ── Widget de mascotas (solo dueños logueados) ──
const DogGoWidget = (() => {
    let pets = [], cur = 0;

    async function init() {
        // Solo ejecutar si existe el widget en la página
        if (!document.getElementById('pw-loading')) return;

        try {
            const res = await fetch('/Perro/MisPerrosJson');
            const data = await res.json();
            pets = data;

            if (!pets || pets.length === 0) {
                show('pw-empty');
                return;
            }

            show('pw-card');
            render();

            // Mostrar banner con el primer perro si existe
            const banner = document.getElementById('paseo-banner');
            const title = document.getElementById('pb-title');
            if (banner && title && pets.length > 0) {
                title.textContent = 'Próximo paseo de ' + pets[0].nombre;
                banner.style.display = 'flex';
            }
        } catch (e) {
            show('pw-empty');
        }
    }

    function show(id) {
        ['pw-loading', 'pw-empty', 'pw-card'].forEach(function (i) {
            const el = document.getElementById(i);
            if (el) el.style.display = (i === id) ? 'block' : 'none';
        });
    }

    function render() {
        const p = pets[cur];
        const img = document.getElementById('pw-foto');
        const fb = document.getElementById('pw-foto-fallback');

        if (p.imagenUrl) {
            img.src = p.imagenUrl;
            img.style.display = 'block';
            fb.style.display = 'none';
        } else {
            img.style.display = 'none';
            fb.style.display = 'flex';
        }

        document.getElementById('pw-nombre').textContent = p.nombre;
        document.getElementById('pw-raza').textContent = p.raza || 'Sin raza registrada';
        document.getElementById('pw-edad').textContent = p.edad ? p.edad + ' año' + (p.edad === 1 ? '' : 's') : '—';
        document.getElementById('pw-tamaño').textContent = p.tamaño || '—';

        const nw = document.getElementById('pw-notas-wrap');
        const ne = document.getElementById('pw-notas');
        if (p.notas) {
            ne.textContent = p.notas;
            nw.style.display = 'block';
        } else {
            nw.style.display = 'none';
        }

        const lnk = document.getElementById('pw-ver-perfil');
        if (lnk) lnk.href = '/Perro/Details/' + p.id;

        renderDots();
    }

    function renderDots() {
        const c = document.getElementById('pw-dots');
        if (!c) return;
        c.innerHTML = '';
        pets.forEach(function (_, i) {
            const d = document.createElement('div');
            d.className = 'pdot' + (i === cur ? ' active' : '');
            d.onclick = function () { cur = i; render(); };
            c.appendChild(d);
        });
    }

    function next() { cur = (cur + 1) % pets.length; render(); }
    function prev() { cur = (cur - 1 + pets.length) % pets.length; render(); }

    return { init, next, prev };
})();

// ── Carrusel "¿Por qué DogGo?" (visitantes) ────
const CarouselDG = (() => {
    const track = document.getElementById('carousel');
    if (!track) return { next() { }, prev() { } };

    const cards = track.querySelectorAll('.why-card');
    const total = cards.length;
    const GAP = 20;
    let ci = 0, autoTimer;

    function getVisible() {
        const w = track.parentElement.offsetWidth;
        if (w >= 900) return 3;
        if (w >= 560) return 2;
        return 1;
    }

    function render() {
        const visible = getVisible();
        const w = track.parentElement.offsetWidth;
        const cw = (w - GAP * (visible - 1)) / visible;

        cards.forEach(function (c) {
            c.style.minWidth = cw + 'px';
            c.style.maxWidth = cw + 'px';
        });

        const maxCi = Math.max(0, total - visible);
        if (ci > maxCi) ci = maxCi;
        track.style.transform = 'translateX(-' + (ci * (cw + GAP)) + 'px)';

        const cd = document.getElementById('cdots');
        if (cd) {
            cd.innerHTML = '';
            for (let i = 0; i <= maxCi; i++) {
                const d = document.createElement('div');
                d.className = 'cdot' + (i === ci ? ' active' : '');
                d.onclick = function () { ci = i; render(); resetAuto(); };
                cd.appendChild(d);
            }
        }
    }

    function next() {
        const v = getVisible();
        ci = (ci < total - v) ? ci + 1 : 0;
        render();
        resetAuto();
    }

    function prev() {
        const v = getVisible();
        ci = (ci > 0) ? ci - 1 : total - v;
        render();
        resetAuto();
    }

    function resetAuto() {
        clearInterval(autoTimer);
        autoTimer = setInterval(next, 3800);
    }

    document.addEventListener('DOMContentLoaded', function () {
        render();
        window.addEventListener('resize', render);
        resetAuto();
    });

    return { next, prev };
})();

// ── Contadores animados (visitantes) ───────────
(function () {
    function animateCount(id, target, suffix, duration) {
        const el = document.getElementById(id);
        if (!el) return;
        let start = 0;
        const step = target / 60;
        const timer = setInterval(function () {
            start += step;
            if (start >= target) {
                el.textContent = target + suffix;
                clearInterval(timer);
            } else {
                el.textContent = Math.floor(start) + suffix;
            }
        }, duration / 60);
    }

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            animateCount('s1', 247, '+', 1200);
            animateCount('s2', 38, '+', 1000);
            animateCount('s3', 183, '+', 1100);
            observer.disconnect();
        });
    }, { threshold: 0.3 });

    document.addEventListener('DOMContentLoaded', function () {
        const band = document.querySelector('.stats-band');
        if (band) observer.observe(band);
    });
})();

// ── Accordion de razas (dueños) ─────────────────
function toggleRaza(card) {
    const detalle = card.querySelector('.raza-detalle');
    const isOpen = card.classList.contains('open');

    // Cerrar todos
    document.querySelectorAll('.raza-card').forEach(function (c) {
        c.classList.remove('open');
        c.querySelector('.raza-detalle').style.display = 'none';
    });

    // Abrir el clickeado si estaba cerrado
    if (!isOpen) {
        card.classList.add('open');
        detalle.style.display = 'block';
    }
}

// ── Init al cargar ──────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    DogGoWidget.init();
});