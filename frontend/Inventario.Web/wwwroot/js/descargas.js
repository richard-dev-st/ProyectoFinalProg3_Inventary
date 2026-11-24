// RF-GP-05: descarga el CSV devuelto por GET /api/inventario/exportar.
window.descargarTexto = (nombre, contenido, tipo) => {
    const blob = new Blob([contenido], { type: tipo || 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    URL.revokeObjectURL(url);
};

// RF-GP-06: copia el id de una solicitud para poder repetir la transición desde la API.
window.copiarTexto = async (texto) => {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(texto);
            return true;
        }

        // Fallback para contextos donde la API de portapapeles no está disponible.
        const area = document.createElement('textarea');
        area.value = texto;
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        const ok = document.execCommand('copy');
        area.remove();
        return ok;
    } catch (e) {
        return false;
    }
};
