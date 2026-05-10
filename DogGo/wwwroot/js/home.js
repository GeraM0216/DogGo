// ═══════════════════════════════════════════════
// DogGo — home.js  v2  Agency-tier
// Widget mascotas · Carrusel · Contadores · Razas
// ═══════════════════════════════════════════════

/* ══════════════════════════════════════════════
   WIDGET DE MASCOTAS
══════════════════════════════════════════════ */
const DogGoWidget = (() => {
    let pets = [], cur = 0;

    async function init() {
        if (!document.getElementById('pw-loading')) return;
        try {
            const res  = await fetch('/Perro/MisPerrosJson');
            const data = await res.json();
            pets = data;

            if (!pets?.length) { show('pw-empty'); return; }

            show('pw-card');
            render(true);

            const banner = document.getElementById('paseo-banner');
            const title  = document.getElementById('pb-title');
            if (banner && title && pets.length) {
                title.textContent = 'Próximo paseo de ' + pets[0].nombre;
                banner.style.display = 'flex';
                banner.style.animation = 'fadeUp .5s cubic-bezier(0.22,1,0.36,1) both';
            }
        } catch { show('pw-empty'); }
    }

    function show(id) {
        ['pw-loading','pw-empty','pw-card'].forEach(i => {
            const el = document.getElementById(i);
            if (el) el.style.display = i === id ? 'block' : 'none';
        });
    }

    function render(skipAnim = false) {
        const p   = pets[cur];
        const img = document.getElementById('pw-foto');
        const fb  = document.getElementById('pw-foto-fallback');
        const card = document.getElementById('pw-card');

        if (!skipAnim && card) {
            card.style.opacity = '0';
            card.style.transform = 'translateX(10px)';
            setTimeout(() => {
                card.style.transition = 'opacity .3s ease, transform .3s cubic-bezier(0.22,1,0.36,1)';
                card.style.opacity = '1';
                card.style.transform = 'translateX(0)';
            }, 50);
        }

        if (p.imagenUrl) {
            img.src = p.imagenUrl; img.style.display = 'block'; fb.style.display = 'none';
        } else {
            img.style.display = 'none'; fb.style.display = 'flex';
        }

        setText('pw-nombre',  p.nombre);
        setText('pw-raza',    p.raza || 'Sin raza registrada');
        setText('pw-edad',    p.edad ? p.edad + ' año' + (p.edad === 1 ? '' : 's') : '—');
        setText('pw-tamaño',  p.tamaño || '—');

        const nw = document.getElementById('pw-notas-wrap');
        const ne = document.getElementById('pw-notas');
        if (nw && ne) { ne.textContent = p.notas || ''; nw.style.display = p.notas ? 'block' : 'none'; }

        const lnk = document.getElementById('pw-ver-perfil');
        if (lnk) lnk.href = '/Perro/Details/' + p.id;

        renderDots();
    }

    function setText(id, val) {
        const el = document.getElementById(id);
        if (el) el.textContent = val;
    }

    function renderDots() {
        const c = document.getElementById('pw-dots');
        if (!c) return;
        c.innerHTML = '';
        pets.forEach((_, i) => {
            const d = document.createElement('div');
            d.className = 'pdot' + (i === cur ? ' active' : '');
            d.style.cssText = `
                width:${i === cur ? '20px' : '8px'};height:8px;border-radius:999px;
                background:${i === cur ? '#0F9B8E' : '#EDE8E0'};
                transition:all .3s cubic-bezier(0.34,1.56,0.64,1);cursor:pointer;
            `;
            d.onclick = () => { cur = i; render(); };
            c.appendChild(d);
        });
    }

    function next() { cur = (cur + 1) % pets.length; render(); }
    function prev() { cur = (cur - 1 + pets.length) % pets.length; render(); }

    return { init, next, prev };
})();

/* ══════════════════════════════════════════════
   CARRUSEL "¿Por qué DogGo?"
══════════════════════════════════════════════ */
const CarouselDG = (() => {
    const track = document.getElementById('carousel');
    if (!track) return { next() {}, prev() {} };

    const cards = track.querySelectorAll('.why-card');
    const total = cards.length;
    const GAP   = 20;
    let ci = 0, autoTimer;

    function getVisible() {
        const w = track.parentElement.offsetWidth;
        return w >= 900 ? 3 : w >= 560 ? 2 : 1;
    }

    function render() {
        const visible = getVisible();
        const w  = track.parentElement.offsetWidth;
        const cw = (w - GAP * (visible - 1)) / visible;

        cards.forEach(c => { c.style.minWidth = cw + 'px'; c.style.maxWidth = cw + 'px'; });

        const maxCi = Math.max(0, total - visible);
        if (ci > maxCi) ci = maxCi;

        track.style.transform = `translateX(-${ci * (cw + GAP)}px)`;
        track.style.transition = 'transform .55s cubic-bezier(0.32,0.72,0,1)';

        const cd = document.getElementById('cdots');
        if (cd) {
            cd.innerHTML = '';
            for (let i = 0; i <= maxCi; i++) {
                const d = document.createElement('div');
                d.className = 'cdot' + (i === ci ? ' active' : '');
                d.onclick = () => { ci = i; render(); resetAuto(); };
                cd.appendChild(d);
            }
        }
    }

    function next() {
        ci = ci < total - getVisible() ? ci + 1 : 0;
        render(); resetAuto();
    }

    function prev() {
        ci = ci > 0 ? ci - 1 : total - getVisible();
        render(); resetAuto();
    }

    function resetAuto() {
        clearInterval(autoTimer);
        autoTimer = setInterval(next, 4200);
    }

    document.addEventListener('DOMContentLoaded', () => {
        render();
        window.addEventListener('resize', render);
        resetAuto();
    });

    return { next, prev };
})();

/* ══════════════════════════════════════════════
   CONTADORES ANIMADOS
══════════════════════════════════════════════ */
(function () {
    function animateCount(id, target, suffix, duration = 1200) {
        const el = document.getElementById(id);
        if (!el) return;
        const start = performance.now();
        function tick(now) {
            const t = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - t, 4); // ease-out quart
            el.textContent = Math.floor(target * eased) + suffix;
            if (t < 1) requestAnimationFrame(tick);
        }
        requestAnimationFrame(tick);
    }

    const obs = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            animateCount('s1', 247, '+');
            animateCount('s2', 38,  '+', 900);
            animateCount('s3', 183, '+', 1100);
            obs.disconnect();
        });
    }, { threshold: 0.3 });

    document.addEventListener('DOMContentLoaded', () => {
        const band = document.querySelector('.stats-band');
        if (band) obs.observe(band);
    });
})();

/* ══════════════════════════════════════════════
   ACCORDION DE RAZAS — con animación suave
══════════════════════════════════════════════ */
function toggleRaza(card) {
    const detalle = card.querySelector('.raza-detalle');
    const isOpen  = card.classList.contains('open');

    document.querySelectorAll('.raza-card.open').forEach(c => {
        c.classList.remove('open');
        const d = c.querySelector('.raza-detalle');
        d.style.maxHeight = '0';
        d.style.opacity   = '0';
    });

    if (!isOpen) {
        card.classList.add('open');
        detalle.style.display    = 'block';
        detalle.style.maxHeight  = '0';
        detalle.style.overflow   = 'hidden';
        detalle.style.opacity    = '0';
        detalle.style.transition = 'max-height .4s cubic-bezier(0.22,1,0.36,1), opacity .3s ease';
        requestAnimationFrame(() => {
            detalle.style.maxHeight = '200px';
            detalle.style.opacity   = '1';
        });
    }
}

/* ══════════════════════════════════════════════
   INIT
══════════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', () => {
    DogGoWidget.init();

    // Exponer para botones inline
    window.DogGoWidget = DogGoWidget;
    window.CarouselDG  = CarouselDG;
    window.toggleRaza  = toggleRaza;
});
