# Análisis técnico y registro de cambios — POS San Magno

**Fecha:** 19/11/2026  
**Proyecto:** `C:\Users\HP\Downloads\proyecto\POS`  
**Solución:** .NET 9 (`POS.Server` Web API, `POS.Client` Blazor WASM/PWA, `POS.Shared`)  
**Base de datos:** SQL Server `POSDB` (`LAPTOP-VVQV1RGN\MSSQLSERVER01`)

---

## 1. Resumen ejecutivo

El sistema es funcional, compila sin advertencias y cubre el flujo completo de punto de venta, inventario, clientes, despachos, cierres de caja y usuarios. Los principales riesgos detectados eran de **seguridad** (registro público, secretos en texto plano) y **concurrencia** (doble restitución de stock al cancelar despachos, cierres de caja duplicados).

En esta intervención se corrigieron los hallazgos críticos y altos de seguridad/concurrencia, se añadieron controles de robustez (manejador global de errores, cabeceras de seguridad, `AsNoTracking`) y se corrigió el manejo del token en el cliente. Quedan pendientes mejoras estructurales (paginación en servidor, pruebas automatizadas, CSP/cookies `HttpOnly`) documentadas en la sección 7.

| Indicador | Antes | Después |
|---|---|---|
| Compilación (`dotnet build POS.sln -t:Rebuild`) | 0 advertencias / 0 errores | 0 advertencias / 0 errores |
| Registro público de usuarios | Abierto, sin rate limit | **Eliminado** (solo administrador vía `api/Users`) |
| Secretos en `appsettings.Development.json` | JWT y contraseña admin en claro | **Movidos a user-secrets** |
| `Users.Email` | Sin restricción única | **Índice único** (`IX_Users_Email`) |
| Cancelación de despacho | Lectura de estado + restitución (carrera) | **UPDATE condicional** + verificación de filas |
| Cambio de estado de despacho | `SaveChanges` last-wins | **UPDATE condicional** (`Status < nuevo`) |
| Cierre de caja | Carga todas las ventas en memoria, sin bloqueo | **Agregación SQL** + transacción `Serializable` |
| Token en el cliente | Mutaba `DefaultRequestHeaders` | **DelegatingHandler** por petición |
| Errores no controlados | 500 sin cuerpo | **Middleware global** (409 conflicto / 500 controlado) |

---

## 2. Metodología

1. Revisión estática de todos los proyectos (controladores, servicios, repositorios, entidades, DTOs, páginas Razor y configuración).
2. Verificación de la estructura con `mcp-visual` (solución, proyectos, referencias, símbolos).
3. Consultas directas a SQL Server para validar duplicados, nulos y datos existentes antes de migrar.
4. Corrección incremental con compilación tras cada bloque de cambios.
5. Migración de EF Core y actualización de la base de datos.
6. Pruebas de humo contra la API en ejecución (autorización, login, listados, conflicto, cabeceras, endpoint eliminado).
7. Registro de decisiones y pendientes en este documento y en `README.md`.

---

## 3. Hallazgos y estado

### 3.1 Críticos

| ID | Hallazgo | Estado | Evidencia / corrección |
|---|---|---|---|
| C1 | `POST /api/Auth/register` público, sin autorización ni rate limit; creaba cuentas `Cajero` y devolvía token, permitiendo spam y enumeración de usuarios | **Corregido** | Endpoint eliminado (`AuthController.cs`), método `RegisterAsync` eliminado de `IAuthService`/`AuthService`, DTO `RegisterRequest` eliminado. La creación de usuarios queda en `api/Users` (rol Administrador). |
| C2 | `JwtSettings:SecretKey` y `AdminSeed:Password` en texto plano en `appsettings.Development.json` | **Corregido** | Secretos movidos a **user-secrets**. El archivo ya no contiene `SecretKey` ni `Password`. El servidor falla al arrancar si la clave falta o mide menos de 32 caracteres. |
| C3 | `Users.Email` sin índice único: cuentas duplicadas y login ambiguo | **Corregido** | `AppDbContext.cs:33` + migración `20261119135153_UniqueUserEmail` aplicada. Verificado que no había duplicados ni nulos antes de migrar. |
| C4 | Cancelación de despacho con carrera: dos peticiones simultáneas podían restituir el stock dos veces | **Corregido** | `DispatchService.cs:189-247`: se reclama la cancelación con `UPDATE ... WHERE Status <> Cancelado AND Status <> Entregado`; si `affected == 0` se revierte. La restitución ocurre después de reclamar el estado, dentro de la misma transacción. |
| C5 | Cierre de caja con carrera y O(n): cargaba todas las ventas y dos cierres simultáneos podían solaparse | **Corregido** | `CashClosingService.cs:41-96`: transacción `Serializable`, validación de período y totales agregados en SQL mediante `ISaleRepository.GetTotalsByPaymentMethodAsync`. |

### 3.2 Altos

| ID | Hallazgo | Estado | Evidencia / corrección |
|---|---|---|---|
| A1 | Token JWT en `localStorage` y ausencia de CSP/cabeceras de seguridad | **Parcial** | Se añadieron cabeceras `nosniff`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` y HSTS (solo fuera de Development). Migrar a cookies `HttpOnly` + CSP queda pendiente. |
| A2 | Sin manejo global de errores; conflictos de índice único terminaban en 500 | **Corregido** | `Program.cs:121-156`: `UseExceptionHandler` responde 409 para `DbUpdateException` y 500 controlado para el resto, con `traceId`. |
| A3 | Cliente sin `AuthorizeRouteView`; protección manual por página | **Pendiente** | Requiere implementar `AuthenticationStateProvider` sobre `AuthStateService`. La API ya exige JWT en todos los endpoints sensibles. |
| A4 | Listados sin paginación y con `Include` completos; dashboard/reportes agregan en el cliente | **Parcial** | Se añadió `AsNoTracking` en lecturas. La paginación/filtrado en servidor queda pendiente. |
| A5 | Mutación de `HttpClient.DefaultRequestHeaders` compartido | **Corregido** | Nuevo `AuthHeaderHandler` (`POS.Client/Services/AuthHeaderHandler.cs`) inyecta el token por petición; se eliminó `SetAuthHeader()` y sus 27 llamadas. |

### 3.3 Medios

| ID | Hallazgo | Estado | Evidencia / corrección |
|---|---|---|---|
| M1 | `Ajuste` de stock ignoraba `affected == 0` | **Corregido** | `StockService.cs:69-73`: revierte y devuelve error si el producto desapareció. |
| M2 | Código de producto no editable al modificar | **Corregido** | `ProductDialog.razor:4`: se quitó `Disabled="@(!IsNew)"`; el backend ya validaba duplicados excluyendo el propio producto. |
| M3 | `SaleRepository.AddSaleAsync` era `async` sin `await` | **Corregido** | Ahora devuelve `Task.FromResult(...)`, igual que `DispatchRepository.AddAsync`. |
| M4 | Sin `CancellationToken` ni `AsNoTracking` en lecturas | **Parcial** | `AsNoTracking` aplicado en ventas, despachos, stock, cierres y listado de usuarios. `CancellationToken` queda pendiente. |
| M5 | Entidades EF expuestas como contrato de API | **Pendiente** | Cambio estructural; requiere DTOs de salida para todas las entidades. |
| M6 | Sin bloqueo por intentos fallidos de login | **Pendiente** | Existe rate limit de 5/min por IP; falta bloqueo por cuenta. |

### 3.4 Bajos / higiene

| ID | Hallazgo | Estado | Evidencia / corrección |
|---|---|---|---|
| B1 | Proyecto no versionado: el repo Git es `C:\Users\HP` (perfil completo), sin `.gitignore` | **Parcial** | Se agregó `.gitignore` en `POS/`. Sigue pendiente crear un repositorio propio dentro del proyecto. |
| B2 | Sin `global.json`: se compilaba con SDK 11 | **Corregido** | `global.json` fija **10.0.404** (`rollForward: latestFeature`). |
| B3 | `README.md` desactualizado (clientes/despachos como pendientes) | **Corregido** | Se actualizaron configuración, funcionalidad, seguridad, pendientes y notas. |
| B4 | `sql.md` sin las tablas `Customers`/`Dispatches` ni la migración nueva | **Pendiente** | El detalle ya está en `informe.md`; conviene sincronizar `sql.md`. |
| B5 | Typo `Product.StockMinimun` y `User.Username` sin uso | **Pendiente** | Renombrar requiere migración y cambios en UI; documentado para no romper la BD. |
| B6 | `DispatchNumber` con índice no único (aunque se genera `DSP-{Id:D6}`) | **Pendiente** | No es un riesgo actual; evaluar índice único si se permiten números manuales. |
| B7 | Sin pruebas, CI, Docker ni health checks | **Pendiente** | Recomendado como siguiente fase. |

---

## 4. Cambios aplicados

### 4.1 Seguridad

**Eliminación del registro público**
- `POS.Server/Controllers/AuthController.cs`: se eliminó `[HttpPost("register")]`.
- `POS.Server/Services/IAuthService.cs` y `AuthService.cs`: se eliminó `RegisterAsync` y el hardcode de rol `Cajero`.
- `POS.Shared/DTOs/RegisterRequest.cs`: eliminado.
- `POS.Client/Services/AuthApiService.cs`: se eliminó `RegisterAsync`.
- `POS.Client/Pages/Login.razor`: se eliminó la pestaña “Registrarse” y su lógica.
- La gestión de usuarios sigue disponible en `api/Users` con `[Authorize(Roles = "Administrador")]`.

**Secretos fuera del repositorio**
- `POS.Server/appsettings.Development.json`: se quitaron `JwtSettings:SecretKey` y `AdminSeed:Password`.
- Los valores se guardaron con `dotnet user-secrets` en el perfil local:
  - `JwtSettings:SecretKey` (nueva clave aleatoria de 64 caracteres).
  - `AdminSeed:Password` (`Admin123!`, solo para un sembrado nuevo).
- `Program.cs` y `AuthService` ya validaban la clave (mínimo 32 caracteres) y ahora esa validación es la única fuente de configuración.
- **Efecto:** los tokens emitidos con la clave anterior dejan de ser válidos; hay que volver a iniciar sesión.

**Índice único de correo**
- `AppDbContext.cs:30-35`: configuración de `User` con `Email` requerido, máximo 150 y `HasIndex(...).IsUnique()`.
- Migración `POS.Server/Migrations/20261119135153_UniqueUserEmail.cs`: normaliza `Name`/`Email` (no nulos, 150) y crea `IX_Users_Email`.

### 4.2 Concurrencia e integridad

**Cancelación de despacho** (`DispatchService.cs:189`)
- La lectura usa `AsNoTracking()`.
- Se valida existencia, estado cancelado y estado entregado.
- Se ejecuta un `ExecuteUpdateAsync` condicional para pasar a `Cancelado`; si no afecta filas, se revierte y se informa que el estado cambió.
- Solo tras reclamar el estado se restituye el stock y se registran los movimientos `Entrada`.
- Resultado: dos cancelaciones simultáneas no duplican stock; la segunda recibe error.

**Cambio de estado** (`DispatchService.cs:151`)
- Lectura con `AsNoTracking()` y validación de transición.
- `ExecuteUpdateAsync` con `Status != Cancelado && Status < nuevoEstado`, por lo que una transición concurrente ya no se sobrescribe con “last-wins”.

**Cierre de caja** (`CashClosingService.cs`)
- Inyección de `AppDbContext` e `ILogger`.
- Transacción `IsolationLevel.Serializable` para serializar la creación de cierres.
- Validación de que el período tenga duración positiva.
- Totales calculados en SQL con `GetTotalsByPaymentMethodAsync` (`SaleRepository.cs`), evitando cargar todas las ventas.
- Ante error se revierte y se devuelve un mensaje controlado.

**Stock** (`StockService.cs:69`)
- El caso `Ajuste` verifica `affected == 0` y revierte si el producto ya no existe, igual que `Salida`.

### 4.3 Robustez y rendimiento

- `Program.cs`:
  - `UseHsts()` fuera de Development.
  - Cabeceras de seguridad en todas las respuestas.
  - `UseExceptionHandler` global: `DbUpdateException` → **409**, resto → **500** con `traceId`.
- `AsNoTracking()` en:
  - `SaleRepository.GetAllAsync` / `GetByIdAsync`.
  - `DispatchRepository.GetAllAsync` / `GetByIdAsync`.
  - `StockRepository.GetAllAsync`.
  - `CashClosingRepository.GetAllAsync` / `GetByIdAsync` / `GetLastAsync`.
  - `UserRepository.GetAllAsync`.
- `SaleRepository.AddSaleAsync` dejó de ser `async` sin `await`.

### 4.4 Cliente

- Nuevo `AuthHeaderHandler` (`DelegatingHandler`) que añade `Authorization: Bearer` por petición.
- `POS.Client/Program.cs:17-24`: el `HttpClient` se construye con el handler.
- `ApiClientBase.cs`: se eliminó `SetAuthHeader()` y la dependencia de `System.Net.Http.Headers`.
- Se quitaron las 27 llamadas a `SetAuthHeader()` de los servicios de API.
- `ProductDialog.razor`: código editable al modificar.

### 4.5 Configuración e higiene

- `global.json`: SDK 10.0.404 con `rollForward: latestFeature`.
- `.gitignore`: bin/obj, `.vs`, archivos de usuario, secretos locales, logs, `Backup/`.
- `README.md`: configuración con user-secrets, funcionalidad de clientes/despachos, seguridad, pendientes y notas de migración.

### 4.6 Base de datos

| Migración | Contenido | Estado |
|---|---|---|
| `20261111125614_InitialDB` | Esquema inicial | Aplicada |
| `20261119124714_AddCustomersAndDispatches` | Clientes y despachos | Aplicada |
| `20261119135153_UniqueUserEmail` | `IX_Users_Email` único; `Name`/`Email` requeridos (150) | **Aplicada en esta intervención** |

---

## 5. Verificación realizada

| Prueba | Resultado |
|---|---|
| `dotnet build POS.sln -t:Rebuild` (SDK 10.0.404) | **0 advertencias, 0 errores** |
| `dotnet ef database update --project POS.Server` | Migración aplicada; `CREATE UNIQUE INDEX [IX_Users_Email]` |
| `GET /api/Users` sin token | **401** |
| `GET /api/Products` sin token | **401** |
| `POST /api/Auth/Login` (usuario administrador de prueba) | **200** + token |
| `GET /api/Users`, `/api/Products`, `/api/Sales`, `/api/Dispatches`, `/api/Customers` con token | **200** |
| `GET /api/CashClosings/last` sin cierres | **204** |
| `POST /api/Auth/register` | **404** (endpoint eliminado) |
| `POST /api/Users` con email existente | **409** (índice único + mapeo global) |
| Cabeceras en respuesta autenticada | `nosniff`, `DENY`, `no-referrer` |
| Usuario temporal de prueba | Creado y **eliminado**; no queda residuo |

> La prueba de concurrencia real (dos cancelaciones simultáneas) no se ejecutó con carga; la corrección se basa en el `UPDATE` condicional dentro de la transacción y en la verificación de filas afectadas. Se recomienda una prueba de integración con dos peticiones paralelas.

---

## 6. Decisiones relevantes

1. **Registro eliminado en lugar de restringido:** el endpoint autoiniciaba sesión con el usuario creado, lo que sobrescribía la sesión del administrador en el cliente. La creación de usuarios ya existe en `api/Users` con roles.
2. **Secreto JWT rotado:** al moverlo a user-secrets se generó una clave nueva; las sesiones abiertas deben renovarse.
3. **HSTS solo en producción:** evita fijar `localhost` a HTTPS durante el desarrollo.
4. **409 para conflictos de datos:** el middleware global evita exponer detalles de EF/SQL y distingue conflictos de errores internos.
5. **Cierre de caja `Serializable`:** puede producir un deadlock esporádico bajo dos cierres simultáneos; el servicio lo captura y devuelve error controlado. Es preferible a duplicar períodos.
6. **No se renombró `StockMinimun` ni se eliminó `User.Username`:** habría requerido migraciones adicionales y cambios en UI; queda planificado.

---

## 7. Pendientes recomendados

1. **Pruebas automatizadas** (xUnit) de: cancelación de despacho, transiciones, cierre de caja, venta con stock insuficiente e índice único de email; más CI.
2. **Paginación y filtrado en servidor** para ventas, despachos, stock y reportes; mover agregados del cliente a SQL.
3. **Autenticación del cliente más estricta:** `AuthenticationStateProvider` + `AuthorizeRouteView`, y evaluar cookies `HttpOnly` o refresh tokens con rotación.
4. **CSP** en el cliente publicado (requiere ajustar orígenes de MudBlazor/WASM).
5. **Bloqueo por intentos fallidos** y recuperación/cambio de contraseña.
6. **DTOs de salida** para no exponer entidades EF (incluye `Product`, `Sale`, `Dispatch`, etc.).
7. **`CancellationToken`** en controladores, servicios y repositorios.
8. **Health checks, logs estructurados, Docker y CI/CD.**
9. **Sincronizar `sql.md`** con `Customers`, `Dispatches`, `DispatchDetails` y las migraciones nuevas.
10. **Renombrar `StockMinimun`** y eliminar `User.Username` con migraciones dedicadas.
11. **Repositorio Git propio dentro de `POS/`** (hoy el repo es el perfil completo del usuario y no rastrea el proyecto).

---

## 8. Cómo ejecutar y restaurar configuración

```powershell
# 1) Configurar secretos (solo desarrollo)
cd POS.Server
dotnet user-secrets set "JwtSettings:SecretKey" "<clave aleatoria de 32+ caracteres>"
dotnet user-secrets set "AdminSeed:Password" "<contraseña del administrador inicial>"

# 2) Restaurar/compilar
cd ..
dotnet build POS.sln

# 3) Ejecutar
dotnet run --project POS.Server
dotnet run --project POS.Client
```

Si la base de datos ya tiene un administrador activo, `AdminSeed` no se usa y la contraseña existente no cambia. Tras rotar `JwtSettings:SecretKey`, cierra sesión y vuelve a entrar.

---

## 9. Archivos modificados y creados

**Servidor**
- `POS.Server/Program.cs`
- `POS.Server/Data/AppDbContext.cs`
- `POS.Server/Controllers/AuthController.cs`
- `POS.Server/Services/IAuthService.cs`, `AuthService.cs`, `CashClosingService.cs`, `DispatchService.cs`, `StockService.cs`
- `POS.Server/Repositories/ISaleRepository.cs`, `SaleRepository.cs`, `DispatchRepository.cs`, `StockRepository.cs`, `CashClosingRepository.cs`, `UserRepository.cs`
- `POS.Server/appsettings.Development.json`
- `POS.Server/Migrations/20261119135153_UniqueUserEmail.cs` (+ Designer y snapshot)
- `POS.Server/Migrations/20261119135153_UniqueUserEmail.Designer.cs`

**Compartido**
- `POS.Shared/DTOs/RegisterRequest.cs` (eliminado)

**Cliente**
- `POS.Client/Program.cs`
- `POS.Client/Services/ApiClientBase.cs`, `AuthHeaderHandler.cs` (nuevo), `AuthApiService.cs`, `UserApiService.cs`, `CategoryApiService.cs`, `ProductApiService.cs`, `SaleApiService.cs`, `StockApiService.cs`, `CashClosingApiService.cs`, `CustomerApiService.cs`, `DispatchApiService.cs`
- `POS.Client/Pages/Login.razor`, `ProductDialog.razor`

**Raíz**
- `README.md`
- `global.json` (nuevo)
- `.gitignore` (nuevo)
- `analisis.md` (este documento)

---

## 10. Intervención posterior — moneda, dashboard/alertas y avisos de stock

**Fecha:** 19/11/2026 (misma jornada, después del módulo de clientes y despachos)

| ID | Hallazgo | Estado | Corrección |
|---|---|---|---|
| D1 | Los importes se mostraban con la cultura del navegador (`S/`, `$` u otra) en lugar de bolivianos | **Corregido** | Cultura fijada en `POS.Client/Program.cs` (`Bs`, coma decimal, punto de miles) + `BlazorWebAssemblyLoadAllGlobalizationData` en `POS.Client.csproj` |
| D2 | `GET /api/Sales` devolvía 500 por ciclo `SaleDetails.Sale.SaleDetails` en cuanto existían ventas | **Corregido** | `[JsonIgnore]` en `SaleDetail.Sale` y en `DispatchDetail.Dispatch` (mismo ciclo latente) |
| D3 | Dashboard y centro de alertas fallaban sin cierres de caja: `GET /api/CashClosings/last` responde 204 y `GetFromJsonAsync` lanzaba `JsonException` | **Corregido** | `CashClosingApiService.GetLastAsync` devuelve `null` cuando la respuesta es 204 |
| D4 | El stock bajo solo se veía entrando al centro de alertas; no había aviso global | **Corregido** | `StockAlertService` + campana con contador + snackbar con acción “Ver alertas”, con refresco al navegar, al vender y al mover stock |

### 10.1 Detalle de los cambios

- **Moneda:** `POS.Client/Program.cs` clona la cultura invariante al arrancar y define `CurrencySymbol = "Bs"`, patrón positivo `Bs 3,50`, coma decimal y punto de miles. `POS.Client.csproj` añade `BlazorWebAssemblyLoadAllGlobalizationData`; sin esa propiedad Blazor aborta el arranque con “change in the application's culture that is not supported”.
- **Serialización:** las navegaciones inversas se marcan con `[JsonIgnore]` (`SaleDetail.Sale`, `DispatchDetail.Dispatch`) en lugar de usar `ReferenceHandler.IgnoreCycles`, que no está disponible en el `System.Text.Json` referenciado por `POS.Shared`.
- **Cierres de caja:** el servidor mantiene `Ok(null)` → 204; es el cliente quien tolera la respuesta vacía. Aplica a dashboard, centro de alertas, cierre de caja e historial de cierres.
- **Alertas:** `StockAlertService` centraliza el cálculo (`IsActive && CurrentStock <= StockMinimun`; agotado con stock ≤ 0). `StockAlertBell.razor` pinta el contador, abre `/alerts` y muestra el aviso una sola vez por cambio de cantidad. Se refresca en `LocationChanged`, en el cambio de sesión, tras una venta (`PointOfSale.CheckoutAsync`) y tras un movimiento (`Stock.RegisterMovementAsync`).

### 10.2 Verificación

| Prueba | Resultado |
|---|---|
| `dotnet build POS.sln` | 0 errores, 0 advertencias |
| `GET /api/Products`, `/api/Sales`, `/api/Dispatches`, `/api/Stocks`, `/api/Categories` con token | **200** |
| `GET /api/CashClosings/last` sin cierres | **204** (el cliente lo trata como “sin cierre”) |
| Dashboard en navegador | Carga indicadores, últimas ventas, métodos de pago, stock bajo y “Aún no se ha realizado ningún cierre” |
| Centro de alertas en navegador | 0 agotados, 1 stock bajo, 0 despachos pendientes, sin cierres |
| Aviso de stock | Snackbar “1 producto(s) con stock bajo o agotado” y campana con contador 1; al pulsarla abre `/alerts` |
| Formato de importes | `Bs 212,90`, `Bs 462,60`, `Bs 3,50`, `Bs 1.234,50`, `Bs 0,00` |

### 10.3 Archivos

| Archivo | Tipo |
|---|---|
| `POS.Client/Program.cs` | Modificado (cultura y registro de `StockAlertService`) |
| `POS.Client/POS.Client.csproj` | Modificado (globalización completa) |
| `POS.Client/Services/CashClosingApiService.cs` | Modificado (manejo de 204) |
| `POS.Client/Services/StockAlertService.cs` | Nuevo |
| `POS.Client/Layout/StockAlertBell.razor` | Nuevo |
| `POS.Client/Layout/MainLayout.razor` | Modificado (campana en la barra) |
| `POS.Client/Pages/PointOfSale.razor`, `Pages/Stock.razor` | Modificados (refresco de alertas) |
| `POS.Shared/Entities/SaleDetail.cs`, `Entities/DispatchDetail.cs` | Modificados (`[JsonIgnore]`) |

### 10.4 Datos de demostración cargados

Se insertaron datos para probar el flujo completo: 6 categorías de negocio (Bebidas, Snacks, Lácteos, Abarrotes, Limpieza, Panadería), 18 productos (`BEB-001`…`PAN-002`) y 18 ventas por **Bs 462,60** repartidas en tres semanas, con 43 líneas de detalle y 61 movimientos de `Stocks` coherentes (entradas iniciales y salidas por venta). `CashClosing`, `Customers`, `Dispatches` y `DispatchDetails` quedan vacías. Los totales se verificaron en SQL: `SUM(Sales.TotalAmount)` = `SUM(SaleDetails.TotalPrice)`, sin stock negativo ni `CurrentStock` incoherente.
