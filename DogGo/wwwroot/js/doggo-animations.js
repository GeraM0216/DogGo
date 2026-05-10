/* ═══════════════════════════════════════════════════
   DogGo — doggo-animations.js
   Scroll reveal · Navbar · Counters · Micro-interactions
   ═══════════════════════════════════════════════════ */

(function () {
    'use strict';

    /* ── Navbar scroll effect ──────────────────────── */
    const navbar = document.querySelector('.navbar-doggo');
    if (navbar) {
        const onScroll = () => navbar.classList.toggle('scrolled', window.scrollY > 20);
        window.addEventListener('scroll', onScroll, { passive: true });
    }

    /* ── Scroll reveal (IntersectionObserver) ──────── */
    const revealEls = document.querySelectorAll('.reveal');
    if (revealEls.length) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.08, rootMargin: '0px 0px -40px 0px' });

        revealEls.forEach(el => observer.observe(el));
    }

    /* ── Auto-reveal para cards sin clase manual ──── */
    const autoRevealSelectors = [
        '.paseador-card', '.paseo-card', '.articulo-card',
        '.producto-card', '.lugar-card', '.raza-card',
        '.kpi-card', '.mp-kpi', '.doggo-card', '.perro-item'
    ];

    const autoEls = document.querySelectorAll(autoRevealSelectors.join(','));
    if (autoEls.length) {
        const autoObs = new IntersectionObserver((entries) => {
            entries.forEach((entry, i) => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    const delay = (el.dataset.revealDelay || 0);
                    setTimeout(() => el.classList.add('auto-visible'), delay);
                    autoObs.unobserve(el);
                }
            });
        }, { threshold: 0.06, rootMargin: '0px 0px -30px 0px' });

        autoEls.forEach((el, i) => {
            el.style.opacity = '0';
            el.style.transform = 'translateY(20px)';
            el.style.transition = `opacity .55s cubic-bezier(0.22,1,0.36,1) ${i * 60}ms, transform .55s cubic-bezier(0.22,1,0.36,1) ${i * 60}ms`;
            el.dataset.revealDelay = 0;
            autoObs.observe(el);
        });

        document.addEventListener('auto-visible', () => {});

        // Tiny helper para activar
        const style = document.createElement('style');
        style.textContent = '.auto-visible { opacity: 1 !important; transform: translateY(0) !important; }';
        document.head.appendChild(style);
    }

    /* ── Animated counters ─────────────────────────── */
    const counterEls = document.querySelectorAll('[data-counter]');
    if (counterEls.length) {
        const countObs = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                const el = entry.target;
                const target = parseFloat(el.dataset.counter);
                const prefix = el.dataset.prefix || '';
                const suffix = el.dataset.suffix || '';
                const decimals = el.dataset.decimals || 0;
                const duration = 1200;
                const start = performance.now();

                const tick = (now) => {
                    const elapsed = Math.min((now - start) / duration, 1);
                    // Ease-out quint
                    const eased = 1 - Math.pow(1 - elapsed, 5);
                    el.textContent = prefix + (target * eased).toFixed(decimals) + suffix;
                    if (elapsed < 1) requestAnimationFrame(tick);
                };

                requestAnimationFrame(tick);
                countObs.unobserve(el);
            });
        }, { threshold: 0.5 });

        counterEls.forEach(el => countObs.observe(el));
    }

    /* ── Ripple en botones primarios ───────────────── */
    document.querySelectorAll('.btn-primary-doggo, .btn-solicitar, .btn-modal-confirm, .pb-btn').forEach(btn => {
        btn.addEventListener('click', function (e) {
            const rect = this.getBoundingClientRect();
            const ripple = document.createElement('span');
            const size = Math.max(rect.width, rect.height);
            ripple.style.cssText = `
                position:absolute;width:${size}px;height:${size}px;
                border-radius:50%;background:rgba(255,255,255,.3);
                top:${e.clientY - rect.top - size/2}px;
                left:${e.clientX - rect.left - size/2}px;
                transform:scale(0);animation:ripple .55s ease-out forwards;
                pointer-events:none;
            `;
            this.style.position = 'relative';
            this.style.overflow = 'hidden';
            this.appendChild(ripple);
            setTimeout(() => ripple.remove(), 600);
        });
    });

    // Ripple keyframe
    const rippleStyle = document.createElement('style');
    rippleStyle.textContent = '@keyframes ripple { to { transform: scale(2.5); opacity: 0; } }';
    document.head.appendChild(rippleStyle);

    /* ── Active nav link ───────────────────────────── */
    const currentPath = window.location.pathname;
    document.querySelectorAll('.navbar-doggo .nav-link').forEach(link => {
        if (link.getAttribute('href') === currentPath) link.classList.add('active');
    });

    /* ── Alert auto-dismiss ────────────────────────── */
    document.querySelectorAll('.alert').forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity .5s ease, transform .5s ease';
            alert.style.opacity = '0';
            alert.style.transform = 'translateX(20px)';
            setTimeout(() => alert.remove(), 500);
        }, 4500);
    });

})();
