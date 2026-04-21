// ═══════════════════════════════════════════════
// DogGo — paseo.js
// Toggle panel cancelar en MisPaseos
// ═══════════════════════════════════════════════

function toggleCancelar(id) {
    const panel = document.getElementById('cancelar-' + id);
    if (!panel) return;
    panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
}