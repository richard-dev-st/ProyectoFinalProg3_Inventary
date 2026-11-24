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
