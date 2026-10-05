namespace ERP.Tests.Infrastructure;

// Una sola ApiFactory (y una sola limpieza de base) para TODA la corrida de pruebas -- no por
// clase, para no pagar el costo de arrancar la app una y otra vez. Las pruebas individuales no
// chocan entre sí porque cada una crea sus propios clientes/productos/usuarios con nombres únicos
// (ver Infrastructure/Unique.cs) en vez de depender de datos sembrados compartidos.
public class DatabaseFixture : IAsyncLifetime
{
    public ApiFactory Factory { get; } = new();

    public async Task InitializeAsync() => await Factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}

// xUnit corre en paralelo las clases de distintas colecciones -- todas las pruebas de este
// proyecto comparten la misma base "erp_test", así que todas van en ESTA colección para que
// corran en serie contra la misma ApiFactory/DatabaseFixture.
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<DatabaseFixture>;
