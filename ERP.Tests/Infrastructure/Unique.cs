namespace ERP.Tests.Infrastructure;

// Sufijo único por proceso+contador para que cada prueba pueda crear su propio usuario/cliente/
// producto sin chocar con otras pruebas que corren contra la MISMA base compartida.
public static class Unique
{
    private static int _contador;

    public static string Nombre(string prefijo) =>
        $"{prefijo}-{Environment.ProcessId}-{Interlocked.Increment(ref _contador)}";
}
