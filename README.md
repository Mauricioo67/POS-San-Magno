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

### Moneda

Los importes se guardan en la base como `decimal` sin símbolo y representan **bolivianos (Bs)**. El cliente fija la cultura al arrancar en `POS.Client/Program.cs` (símbolo `Bs`, coma decimal y punto de miles) y `POS.Client.csproj` define `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>`, necesario porque Blazor no permite cambiar la cultura al iniciar sin cargar los datos de globalización. Todo `ToString("C")` se muestra como `Bs 12,50`; las exportaciones CSV llevan solo números.

## Configuración

`POS.Server/appsettings.Development.json` **no contiene secretos**. Solo define `JwtSettings:Issuer`, `JwtSettings:Audience`, `JwtSettings:ExpirationMinutes`, `AdminSeed:Name`, `AdminSeed:Email` y `Cors:AllowedOrigins`.

Los secretos se cargan con **user-secrets** (solo en desarrollo):

```powershell
cd POS.Server
dotnet user-secrets set "JwtSettings:SecretKey" "<clave aleatoria de 32+ caracteres>"
dotnet user-secrets set "AdminSeed:Password" "<contraseña del administrador inicial>"
dotnet user-secrets list
```

Si falta `JwtSettings:SecretKey` o tiene menos de 32 caracteres, el servidor no arranca (falla de forma explícita). `AdminSeed` solo se usa si **no existe ningún administrador activo**.

`POS.Server/appsettings.json` contiene solo logging, `AllowedHosts` y la cadena de conexión (autenticación integrada de Windows). En producción configura `JwtSettings`, `AdminSeed` y `ConnectionStrings:DefaultConnection` con variables de entorno o un gestor de secretos.

## Ejecución

```powershell
dotnet run --project POS.Server
dotnet run --project POS.Client
```

En Visual Studio puedes configurar ambos proyectos como proyectos de inicio. El servidor aplica las migraciones/crea el administrador inicial al arrancar (si la BD ya existe y no hay administrador activo).

## Cambios recientes

- Moneda: precios, totales, recibos, reportes y cierres se muestran en **Bs** (bolivianos) con coma decimal; antes dependían de la cultura del navegador del equipo (S/, $).
- Alertas de stock: nuevo `StockAlertService` y campana con contador en la barra superior. Cuando hay productos agotados o bajo el mínimo se muestra un aviso emergente con acceso al centro de alertas, y el estado se refresca al navegar, al cobrar una venta y al registrar movimientos de stock.
- Correcciones: `GET /api/Sales` devolvía 500 por un ciclo de serialización JSON (`SaleDetail.Sale` y `DispatchDetail.Dispatch` ahora llevan `[JsonIgnore]`); el dashboard y el centro de alertas fallaban sin cierres de caja porque `GET /api/CashClosings/last` responde 204 y el cliente intentaba deserializar el cuerpo vacío (ahora se interpreta como “sin cierre”).
- Seguridad: se eliminó el registro público; los usuarios se crean desde **Usuarios** (solo administrador). `Email` de usuario es único a nivel de BD. Secretos movidos a user-secrets. Cabeceras de seguridad (`nosniff`, `frame-options`, `referrer-policy`, `permissions-policy`) y HSTS fuera de desarrollo.
- Concurrencia: cancelación de despacho y cambio de estado con `ExecuteUpdate` condicional (sin doble restitución de stock ni transiciones perdidas). Cierre de caja con transacción `Serializable` y totales agregados en SQL.
- Robustez: manejador global de errores (409 para conflictos de datos, 500 controlado), `AsNoTracking` en lecturas y validación de ajustes de stock.
- Cliente: el token se inyecta por petición con un `DelegatingHandler` (sin mutar `DefaultRequestHeaders`). Código de producto editable al modificar.
- Higiene: `global.json` (SDK 10.0.404) y `.gitignore`.
- Seguridad previa: `PasswordHash` oculto en JSON, administrador inicial (`AdminSeed`), validaciones de FK/null, CORS restringido, rate limiting por IP en login y descarte de tokens expirados en el cliente.
- Concurrencia previa: stock descontado con `ExecuteUpdate` condicional dentro de una transacción (sin sobreventa).
- Arranque: el servidor aplica migraciones pendientes y crea el administrador inicial si no existe.
- Reportes: exportación CSV del período filtrado.
- Punto de venta: efectivo recibido, cálculo de vuelto y recibo de la venta con opción de copiar el resumen.

## Funcionalidad implementada

- Autenticación JWT con login. El registro público está deshabilitado; los usuarios se gestionan desde la página **Usuarios** (solo administrador).
- Roles `Administrador` y `Cajero`; endpoints de escritura de catálogo, usuarios y cierres de caja restringidos a administrador.
- Categorías: CRUD.
- Productos: CRUD, búsqueda, activación/desactivación lógica y alerta de stock bajo.
- Ventas: carrito, métodos de pago, efectivo recibido/vuelto, recibo al cobrar (venta, productos, total y vuelto) y descuento atómico de stock con movimiento de `Salida` en una transacción.
- Stock: entradas, salidas y ajustes con validación de existencias.
- Clientes: CRUD con documento único, activación/desactivación lógica y búsqueda.
- Despachos: creación con descuento de stock, detalle, transiciones de estado y cancelación con restitución de stock.
- Cierre de caja: período desde el último cierre, totales por método de pago y ajustes (solo administrador).
- Usuarios: listado/edición/activación (solo administrador).
- Centro de alertas: agotados, stock bajo, despachos pendientes y estado del cierre de caja, con aviso global (campana con contador y notificación emergente) cuando hay productos críticos.
- Dashboard, reportes con filtros por fecha, totales por método de pago y exportación CSV, historial de ventas, página Acerca de y PWA.
- Login con validación por campo, mostrar/ocultar contraseña e indicador de carga.

## Seguridad y robustez

- Contraseñas con BCrypt; `User.PasswordHash` se excluye de la serialización JSON.
- `Email` de usuario con índice único; login normaliza el correo a minúsculas.
- Registro público eliminado; los usuarios se crean desde el endpoint de administración (`api/Users`, rol Administrador).
- Validación de categoría existente al crear/editar productos y de método de pago/saldo inicial.
- Stock actualizado con `ExecuteUpdate` condicional para evitar sobreventa por concurrencia.
- Cancelación de despacho con actualización condicional de estado dentro de la transacción (evita restituir stock dos veces).
- Rate limiting en login (5 intentos por minuto por IP).
- CORS restringido a orígenes configurados.
- Cabeceras de seguridad en las respuestas de la API y HSTS en producción; manejador global de errores.
- El cliente inyecta el token por petición y descarta tokens expirados al iniciar sesión.

## Pendiente

- Pruebas automatizadas (unitarias/integración) y CI.
- Paginación y filtrado en servidor para listados grandes (hoy se cargan completos y se filtran en el cliente).
- Anulación/devoluciones de ventas, descuentos e impuestos.
- Conteo de caja, reapertura y exportación de cierres.
- Impresión física del recibo (hoy se muestra en pantalla y se puede copiar).
- Proveedores, compras, carga de imágenes y códigos de barras.
- Cambio de contraseña, recuperación, bloqueo por intentos y 2FA.
- Renombrar el typo `StockMinimun` y eliminar `User.Username` (requiere migración).
- Migrar el token de `localStorage` a cookies `HttpOnly` o añadir refresh tokens y CSP.
- Observabilidad (logs estructurados, health checks) y despliegue (Docker/CI).

## Notas

- `Product.StockMinimun` es un typo existente en la entidad — **no renombrar** sin revisar la BD/migraciones.
- Migraciones aplicadas: `InitialDB`, `AddCustomersAndDispatches` y `UniqueUserEmail` (índice único en `Users.Email`).
- `global.json` fija el SDK **10.0.404**; el proyecto apunta a `net9.0`.
- El repositorio Git actual es `C:\Users\HP` (todo el perfil) y **no rastrea** archivos de POS. Crea un repositorio dentro de `POS/` para versionar el proyecto con el `.gitignore` incluido.
- Los enums viajan como números entre cliente y servidor (comportamiento por defecto de System.Text.Json).
- `POS.Shared` mantiene el namespace antiguo y compila con `LangVersion 9.0` (netstandard2.0).
- `POS.Client/wwwroot/js/download.js` contiene las funciones de descarga CSV y copiado al portapapeles.
- Tras actualizar el cliente, usa **Ctrl+F5** para evitar que el service worker/WASM sirva una versión en caché.
- La cultura del cliente está fijada a bolivianos; en páginas nuevas basta `ToString("C")` para heredar el formato `Bs`. Un producto cuenta como **agotado** con stock ≤ 0 y como **stock bajo** con stock entre 1 y `StockMinimun`.
