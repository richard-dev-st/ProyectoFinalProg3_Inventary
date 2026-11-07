using Application.DTOs.Documentos;
using Application.Interfaces;
using Application.Services;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class DocumentoServiceTests
{
    //Helper para tener los datos en memoria
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ArchivoEntrada CrearArchivo(string nombre, string tipoContenido = "application/pdf", string contenido = "contenido")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
        return new ArchivoEntrada(nombre, tipoContenido, bytes.Length, new MemoryStream(bytes));
    }

    private static Usuario CrearAdmin(string email)
    {
        var admin = new Usuario(email, "hash");
        admin.CambiarRol(RolUsuario.Administrador, Guid.NewGuid());
        return admin;
    }

    //RF-DOC-01: subir registra nombre, tipo, tamano, fecha y quien lo subio
    [Fact]
    public async Task RF_DOC_01_Subir_RegistraTodosLosDatos()
    {
        //Arrange
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "hash");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var almacen = new AlmacenEnMemoria();
        var service = new DocumentoService(context, almacen);

        //Act
        var resultado = await service.SubirDocumentoAsync(CrearArchivo("informe.pdf"), usuario.Id);

        //Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("informe.pdf", resultado.NombreOriginal);
        Assert.Equal("application/pdf", resultado.TipoContenido);
        Assert.True(resultado.TamanioBytes > 0);
        Assert.True(resultado.FechaSubida > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(usuario.Id, resultado.SubidoPorUsuarioId);
        Assert.Equal("carlos@dominio.com", resultado.SubidoPorEmail);
        Assert.False(resultado.EsEliminado);
        Assert.Single(context.Documentos);
        Assert.True(almacen.Existe(context.Documentos.Single().NombreAlmacenado));
    }

    //RF-DOC-01: si el cliente no envia tipo, se registra uno por defecto
    [Fact]
    public async Task RF_DOC_01_Subir_SinTipoContenido_UsaValorPorDefecto()
    {
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "hash");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());

        var resultado = await service.SubirDocumentoAsync(CrearArchivo("notas.txt", tipoContenido: ""), usuario.Id);

        Assert.Equal("application/octet-stream", resultado.TipoContenido);
    }

    //RF-DOC-02: el listado excluye eliminados y documentos de otros usuarios
    [Fact]
    public async Task RF_DOC_02_Listar_ExcluyeEliminadosYAjenos()
    {
        using var context = CreateContext();
        var usuario1 = new Usuario("carlos@dominio.com", "hash");
        var usuario2 = new Usuario("maria@dominio.com", "hash");
        context.Usuarios.AddRange(usuario1, usuario2);
        await context.SaveChangesAsync();

        var almacen = new AlmacenEnMemoria();
        var service = new DocumentoService(context, almacen);

        var propio = await service.SubirDocumentoAsync(CrearArchivo("propio.pdf"), usuario1.Id);
        var ajeno = await service.SubirDocumentoAsync(CrearArchivo("ajeno.pdf"), usuario2.Id);
        var eliminado = await service.SubirDocumentoAsync(CrearArchivo("eliminado.pdf"), usuario1.Id);
        await service.EliminarDocumentoAsync(eliminado.Id, usuario1.Id);

        var resultado = (await service.ListarDocumentosAsync(usuario1.Id)).ToList();

        Assert.Single(resultado);
        Assert.Equal(propio.Id, resultado[0].Id);
        Assert.DoesNotContain(resultado, d => d.Id == ajeno.Id);
        Assert.DoesNotContain(resultado, d => d.Id == eliminado.Id);
    }

    //RF-DOC-02: el Administrador ve los documentos de todos
    [Fact]
    public async Task RF_DOC_02_Listar_AdministradorVeTodos()
    {
        using var context = CreateContext();
        var usuario1 = new Usuario("carlos@dominio.com", "hash");
        var usuario2 = new Usuario("maria@dominio.com", "hash");
        var admin = CrearAdmin("admin@dominio.com");
        context.Usuarios.AddRange(usuario1, usuario2, admin);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());

        await service.SubirDocumentoAsync(CrearArchivo("uno.pdf"), usuario1.Id);
        await service.SubirDocumentoAsync(CrearArchivo("dos.pdf"), usuario2.Id);

        var resultado = (await service.ListarDocumentosAsync(admin.Id)).ToList();

        Assert.Equal(2, resultado.Count);
    }

    //RF-DOC-03: el dueno descarga su documento
    [Fact]
    public async Task RF_DOC_03_Descargar_DuenioPuede()
    {
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "hash");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());
        var subido = await service.SubirDocumentoAsync(CrearArchivo("informe.pdf", contenido: "datos"), usuario.Id);

        var descarga = await service.DescargarDocumentoAsync(subido.Id, usuario.Id);

        using var reader = new StreamReader(descarga.Contenido);
        Assert.Equal("informe.pdf", descarga.NombreOriginal);
        Assert.Equal("application/pdf", descarga.TipoContenido);
        Assert.Equal("datos", await reader.ReadToEndAsync());
    }

    //RF-DOC-03 + RD-06: otro usuario Estandar es rechazado al invocar el servicio directamente
    [Fact]
    public async Task RF_DOC_03_Descargar_OtroUsuarioEsRechazado()
    {
        using var context = CreateContext();
        var duenio = new Usuario("carlos@dominio.com", "hash");
        var intruso = new Usuario("maria@dominio.com", "hash");
        context.Usuarios.AddRange(duenio, intruso);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());
        var subido = await service.SubirDocumentoAsync(CrearArchivo("informe.pdf"), duenio.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DescargarDocumentoAsync(subido.Id, intruso.Id));
    }

    //RF-DOC-03: el Administrador descarga el documento de otro
    [Fact]
    public async Task RF_DOC_03_Descargar_AdministradorPuede()
    {
        using var context = CreateContext();
        var duenio = new Usuario("carlos@dominio.com", "hash");
        var admin = CrearAdmin("admin@dominio.com");
        context.Usuarios.AddRange(duenio, admin);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());
        var subido = await service.SubirDocumentoAsync(CrearArchivo("informe.pdf"), duenio.Id);

        var descarga = await service.DescargarDocumentoAsync(subido.Id, admin.Id);

        Assert.Equal("informe.pdf", descarga.NombreOriginal);
    }

    //RF-DOC-04: la eliminacion es logica, el registro sigue existiendo
    [Fact]
    public async Task RF_DOC_04_Eliminar_EsLogicaYDesapareceDelListado()
    {
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "hash");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());
        var subido = await service.SubirDocumentoAsync(CrearArchivo("informe.pdf"), usuario.Id);

        await service.EliminarDocumentoAsync(subido.Id, usuario.Id);

        var registro = context.Documentos.Single(d => d.Id == subido.Id);
        Assert.True(registro.EsEliminado);
        Assert.NotNull(registro.FechaEliminacion);
        Assert.Empty(await service.ListarDocumentosAsync(usuario.Id));
    }

    //RF-DOC-04 + RD-06: otro usuario Estandar es rechazado al eliminar
    [Fact]
    public async Task RF_DOC_04_Eliminar_OtroUsuarioEsRechazado()
    {
        using var context = CreateContext();
        var duenio = new Usuario("carlos@dominio.com", "hash");
        var intruso = new Usuario("maria@dominio.com", "hash");
        context.Usuarios.AddRange(duenio, intruso);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());
        var subido = await service.SubirDocumentoAsync(CrearArchivo("informe.pdf"), duenio.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.EliminarDocumentoAsync(subido.Id, intruso.Id));

        Assert.False(context.Documentos.Single(d => d.Id == subido.Id).EsEliminado);
    }

    //RD-07: un archivo ausente produce un rechazo controlado
    [Fact]
    public async Task RD_07_Descargar_ArchivoAusente_LanzaRechazoControlado()
    {
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "hash");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var documento = new Documento("perdido.pdf", "no-existe.pdf", "application/pdf", 10, usuario.Id, DateTime.UtcNow);
        context.Documentos.Add(documento);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DescargarDocumentoAsync(documento.Id, usuario.Id));
    }

    //RD-07: un archivo vacio produce un rechazo controlado
    [Fact]
    public async Task RD_07_Subir_ArchivoVacio_LanzaRechazoControlado()
    {
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "hash");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());
        var archivo = new ArchivoEntrada("vacio.pdf", "application/pdf", 0, new MemoryStream());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubirDocumentoAsync(archivo, usuario.Id));
    }

    //RD-07: una extension no permitida produce un rechazo controlado
    [Fact]
    public async Task RD_07_Subir_ExtensionNoPermitida_LanzaRechazoControlado()
    {
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "hash");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var service = new DocumentoService(context, new AlmacenEnMemoria());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubirDocumentoAsync(CrearArchivo("malware.exe"), usuario.Id));
    }

    private sealed class AlmacenEnMemoria : IAlmacenDocumentos
    {
        private readonly Dictionary<string, byte[]> _archivos = new();

        public async Task GuardarAsync(Stream contenido, string nombreAlmacenado, CancellationToken ct = default)
        {
            using var memoria = new MemoryStream();
            await contenido.CopyToAsync(memoria, ct);
            _archivos[nombreAlmacenado] = memoria.ToArray();
        }

        public Task<Stream> AbrirAsync(string nombreAlmacenado, CancellationToken ct = default)
            => Task.FromResult<Stream>(new MemoryStream(_archivos[nombreAlmacenado]));

        public bool Existe(string nombreAlmacenado) => _archivos.ContainsKey(nombreAlmacenado);

        public Task EliminarAsync(string nombreAlmacenado, CancellationToken ct = default)
        {
            _archivos.Remove(nombreAlmacenado);
            return Task.CompletedTask;
        }
    }
}
