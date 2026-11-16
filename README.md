# POS — Sistema de Punto de Venta (San Magno)

Solución .NET 9 con backend Web API + frontend Blazor WebAssembly (PWA, MudBlazor).

## Estructura

| Proyecto | Tipo | Descripción |
|---|---|---|
| `POS.Server` | ASP.NET Core Web API (net9.0) | API REST con EF Core + SQL Server (`POSDB`), JWT Bearer, BCrypt, CORS |
| `POS.Client` | Blazor WASM + PWA | Frontend MudBlazor, consume la API con HttpClient |
| `POS.Shared` | Class library (netstandard2.0) | Entidades, DTOs y enums compartidos |

### Puertos
- Servidor: `https://localhost:7008` / `http://localhost:5004`
- Cliente: `https://localhost:7282` / `http://localhost:5281`
- API base del cliente: `POS.Client/wwwroot/appsettings.json` (`ApiBaseUrl`)

## Configuración

`POS.Server/appsettings.Development.json`:

- `JwtSettings` — `SecretKey` (mínimo 32 caracteres), `Issuer`, `Audience`, `ExpirationMinutes`.
- `AdminSeed` — `Name`, `Email`, `Password` del administrador inicial. Solo se usa si **no existe ningún administrador activo**.
- `Cors:AllowedOrigins` — orígenes permitidos del frontend.

`POS.Server/appsettings.json` contiene solo logging, `AllowedHosts` y la cadena de conexión.

En producción no uses los valores de desarrollo: configura `JwtSettings`, `AdminSeed` y `ConnectionStrings:DefaultConnection` con variables de entorno, user-secrets o un gestor de secretos.

## Ejecución

```powershell
dotnet run --project POS.Server
dotnet run --project POS.Client
```

En Visual Studio puedes configurar ambos proyectos como proyectos de inicio. El servidor aplica las migraciones/crea el administrador inicial al arrancar (si la BD ya existe y no hay administrador activo).

## Cambios recientes

- Seguridad: `PasswordHash` oculto en JSON, administrador inicial (`AdminSeed`), validaciones de FK/null, CORS restringido, rate limiting por IP en login y descarte de tokens expirados en el cliente.
- Concurrencia: stock descontado con `ExecuteUpdate` condicional dentro de una transacción (sin sobreventa).
- Arranque: el servidor aplica migraciones pendientes y crea el administrador inicial si no existe.
- Reportes: exportación CSV del período filtrado.
- Punto de venta: efectivo recibido, cálculo de vuelto y recibo de la venta con opción de copiar el resumen.
- Login: validación por campo, botón mostrar/ocultar contraseña e indicador de carga.

## Funcionalidad implementada

- Autenticación JWT con login y registro (el registro siempre crea usuarios `Cajero`).
- Roles `Administrador` y `Cajero`; endpoints de escritura de catálogo, usuarios y cierres de caja restringidos a administrador.
- Categorías: CRUD.
- Productos: CRUD, búsqueda, activación/desactivación lógica, alerta de stock bajo.
- Ventas: carrito, métodos de pago, efectivo recibido/vuelto, recibo al cobrar (venta, productos, total y vuelto) y descuento atómico de stock con movimiento de `Salida` en una transacción.
- Stock: entradas, salidas y ajustes con validación de existencias.
- Cierre de caja: período desde el último cierre, totales por método de pago y ajustes (solo administrador).
- Usuarios: listado/edición/activación (solo administrador).
- Dashboard, reportes con filtros por fecha, totales por método de pago y exportación CSV, historial de ventas, página Acerca de y PWA.
- Login con validación por campo, mostrar/ocultar contraseña e indicador de carga.

## Seguridad y robustez

- Contraseñas con BCrypt; `User.PasswordHash` se excluye de la serialización JSON.
- Validación de categoría existente al crear/editar productos y de método de pago/saldo inicial.
- Stock actualizado con `ExecuteUpdate` condicional para evitar sobreventa por concurrencia.
- Rate limiting en login (5 intentos por minuto por IP).
- CORS restringido a orígenes configurados.
- El cliente descarta tokens expirados al iniciar sesión.
- Manejo de errores con mensajes en pantalla en las páginas principales.

## Pendiente

- Pruebas automatizadas (unitarias/integración).
- Paginación y filtrado en servidor para listados grandes.
- Anulación/devoluciones, descuentos, impuestos y clientes.
- Conteo de caja, reapertura y exportación de cierres.
- Impresión física del recibo (hoy se muestra en pantalla y se puede copiar).
- Proveedores, compras, carga de imágenes y códigos de barras.
- Cambio de contraseña, recuperación, bloqueo por intentos y 2FA.
- Observabilidad (logs estructurados, health checks) y despliegue (Docker/CI).

## Notas

- `Product.StockMinimun` es un typo existente en la entidad — **no renombrar** sin revisar la BD/migraciones.
- Los enums viajan como números entre cliente y servidor (comportamiento por defecto de System.Text.Json).
- `POS.Shared` mantiene el namespace antiguo y compila con `LangVersion 9.0` (netstandard2.0).
- `POS.Client/wwwroot/js/download.js` contiene las funciones de descarga CSV y copiado al portapapeles.
- Tras actualizar el cliente, usa **Ctrl+F5** para evitar que el service worker/WASM sirva una versión en caché.
