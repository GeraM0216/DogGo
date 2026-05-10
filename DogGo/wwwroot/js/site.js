// ═══════════════════════════════════════════════
// DogGo — site.js  v2  Global micro-interactions
// ═══════════════════════════════════════════════

document.addEventListener('DOMContentLoaded', () => {

    /* ── Navbar scroll effect ─────────────────── */
    const navbar = document.querySelector('.navbar-doggo');
    if (navbar) {
        window.addEventListener('scroll', () => {
            navbar.classList.toggle('scrolled', window.scrollY > 20);
        }, { passive: true });
    }

    /* ── Active nav link ──────────────────────── */
    const path = window.location.pathname;
    document.querySelectorAll('.navbar-doggo .nav-link').forEach(link => {
        const href = link.getAttribute('href');
        if (href && path.startsWith(href) && href !== '/') {
            link.classList.add('active');
        } else if (href === '/' && path === '/') {
            link.classList.add('active');
        }
    });

    /* ── Scroll reveal (IntersectionObserver) ─── */
    const revealEls = document.querySelectorAll('.reveal');
    if (revealEls.length) {
        const obs = new IntersectionObserver(entries => {
            entries.forEach(e => {
                if (e.isIntersecting) {
                    e.target.classList.add('visible');
                    obs.unobserve(e.target);
                }
            });
        }, { threshold: 0.08, rootMargin: '0px 0px -40px 0px' });
        revealEls.forEach(el => obs.observe(el));
    }

    /* ── Auto reveal en cards ─────────────────── */
    const selectors = [
        '.paseador-card', '.paseo-card', '.articulo-card',
        '.producto-card', '.lugar-card', '.raza-card',
        '.kpi-card', '.mp-kpi', '.doggo-card', '.perro-item',
        '.quick-action'
    ].join(',');

    const autoEls = document.querySelectorAll(selectors);
    if (autoEls.length) {
        const style = document.createElement('style');
        style.textContent = '.auto-in { opacity: 1 !important; transform: translateY(0) !important; }';
        document.head.appendChild(style);

        const autoObs = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('auto-in');
                    autoObs.unobserve(entry.target);
                }
            });
        }, { threshold: 0.06, rootMargin: '0px 0px -30px 0px' });

        autoEls.forEach((el, i) => {
            el.style.cssText += `
                opacity:0;
                transform:translateY(18px);
                transition: opacity .55s cubic-bezier(0.22,1,0.36,1) ${Math.min(i * 55, 400)}ms,
                            transform .55s cubic-bezier(0.22,1,0.36,1) ${Math.min(i * 55, 400)}ms;
            `;
            autoObs.observe(el);
        });
    }

    /* ── Ripple en botones primarios ──────────── */
    document.querySelectorAll(
        '.btn-primary-doggo, .btn-solicitar, .btn-modal-confirm, .pb-btn, .nav-cta-btn'
    ).forEach(btn => {
        btn.style.position = 'relative';
        btn.style.overflow = 'hidden';
        btn.addEventListener('click', function(e) {
            const rect   = this.getBoundingClientRect();
            const size   = Math.max(rect.width, rect.height) * 2;
            const ripple = document.createElement('span');
            ripple.style.cssText = `
                position:absolute;
                width:${size}px;height:${size}px;border-radius:50%;
                background:rgba(255,255,255,.28);
                top:${e.clientY - rect.top - size/2}px;
                left:${e.clientX - rect.left - size/2}px;
                transform:scale(0);
                animation:doggo-ripple .6s ease-out forwards;
                pointer-events:none;
            `;
            this.appendChild(ripple);
            setTimeout(() => ripple.remove(), 650);
        });
    });

    /* ── Ripple keyframe ──────────────────────── */
    const rStyle = document.createElement('style');
    rStyle.textContent = `
        @keyframes doggo-ripple { to { transform:scale(1); opacity:0; } }
    `;
    document.head.appendChild(rStyle);

    /* ── Alert auto-dismiss con animación ─────── */
    document.querySelectorAll('.alert').forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity .45s ease, transform .45s ease, max-height .45s ease';
            alert.style.opacity    = '0';
            alert.style.transform  = 'translateY(-8px)';
            alert.style.maxHeight  = '0';
            alert.style.overflow   = 'hidden';
            setTimeout(() => alert.remove(), 450);
        }, 4500);
    });

    /* ── Smooth scroll para anchors ───────────── */
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', e => {
            const target = document.querySelector(anchor.getAttribute('href'));
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });

});
