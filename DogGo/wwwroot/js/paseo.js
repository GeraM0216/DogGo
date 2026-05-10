// ═══════════════════════════════════════════════
// DogGo — paseo.js  v2  Agency-tier
// Toggle cancelar · Confirmaciones · Animaciones
// ═══════════════════════════════════════════════

function toggleCancelar(id) {
    const panel = document.getElementById('cancelar-' + id);
    if (!panel) return;

    const isOpen = panel.style.display !== 'none' && panel.style.display !== '';

    if (isOpen) {
        panel.style.maxHeight = '0';
        panel.style.opacity   = '0';
        setTimeout(() => { panel.style.display = 'none'; }, 300);
    } else {
        panel.style.display   = 'block';
        panel.style.maxHeight = '0';
        panel.style.opacity   = '0';
        panel.style.overflow  = 'hidden';
        panel.style.transition = 'max-height .4s cubic-bezier(0.22,1,0.36,1), opacity .3s ease';
        requestAnimationFrame(() => {
            panel.style.maxHeight = '200px';
            panel.style.opacity   = '1';
        });
    }
}

// Exponer global
window.toggleCancelar = toggleCancelar;
