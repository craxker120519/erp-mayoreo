# ERP — Mayoreo y mostrador

Sistema ERP para una empresa de distribución al mayoreo (B2B, con crédito) que
también vende al público en mostrador.

## Módulos

- **Catálogos:** clientes, proveedores, productos (con hoja de seguridad adjunta), empleados y almacenes.
- **Inventario:** existencias por almacén, kardex (PDF y Excel), traspasos entre almacenes.
- **Ventas B2B:** cotización → pedido → remisión/factura, cuentas por cobrar, pagos y notas de crédito.
- **Compras:** pedidos a proveedor con recepción a inventario.
- **Mostrador:** venta al público con apertura y cierre de turno de caja (corte con diferencia calculada).
- **Reportes:** existencias, ventas por cliente/producto, cartera vencida y cortes de caja.
- **Seguridad:** login con JWT, usuarios, bitácora de auditoría y candados de acceso.

## Arquitectura

```
ERP.slnx
  ERP.Data/   — modelos, AppDbContext y migraciones (EF Core + PostgreSQL)
  ERP.Api/    — Web API REST con autenticación JWT
  ERP.App/    — aplicación de escritorio para Windows (.NET MAUI Blazor Hybrid)
  ERP.Tests/  — pruebas de integración de la API
```

La API vive en un servidor de la red local; la app de escritorio se conecta a
ella (la dirección del servidor se captura en la pantalla de login).

## Stack

- .NET 10 · ASP.NET Core Web API · .NET MAUI Blazor Hybrid
- PostgreSQL (Entity Framework Core + Npgsql)
- JWT para autenticación
- Generación de PDF y Excel para documentos y reportes

## Correr en local

1. Instala el .NET 10 SDK y PostgreSQL.
2. Crea una base de datos (por ejemplo `erp_dev`) y configura los secretos de la API:

   ```bash
   cd ERP.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=erp_dev;Username=postgres;Password=..."
   dotnet user-secrets set "Jwt:Key" "<una clave aleatoria de al menos 32 caracteres>"
   ```

3. Arranca la API (aplica las migraciones al iniciar):

   ```bash
   dotnet run --project ERP.Api
   ```

4. Compila y abre la app de escritorio:

   ```bash
   dotnet build ERP.App/ERP.App.csproj
   ```

   En el primer arranque, indica la URL del servidor (`http://localhost:5143`).
   Si la base está vacía, la API crea el usuario `admin` / `admin1234`; cámbialo
   desde *Administración → Usuarios* al entrar.

## Licencia

Código publicado **solo para consulta** (portafolio). Todos los derechos reservados.
Antes de usar, copiar o modificar cualquier parte de este proyecto debes
**contactar al autor y obtener su autorización por escrito**:
[github.com/craxker120519](https://github.com/craxker120519). Ver [`LICENSE`](./LICENSE).
