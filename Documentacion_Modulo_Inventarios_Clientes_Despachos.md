# Desarrollo del módulo de inventarios, clientes y despachos — App POS San Magno

## 1. Introducción

El proyecto App POS San Magno implementa un sistema de punto de venta para administrar productos, categorías, existencias, clientes y despachos. La solución está organizada en tres proyectos: `POS.Client`, `POS.Server` y `POS.Shared`. La interfaz está desarrollada con Blazor WebAssembly y MudBlazor; el backend expone una API REST con ASP.NET Core; y el acceso persistente se realiza mediante Entity Framework Core y SQL Server.

## 2. Propósito y alcance de la consigna

Desarrollar formularios, controladores y lógica de negocio para operaciones CRUD relacionales de inventarios, clientes y despachos, asegurando que las operaciones que modifican existencias y sus registros históricos se confirmen o reviertan de manera conjunta.

El alcance funcional revisado en el código incluye:

- Productos y categorías: consulta, creación, actualización y baja lógica de productos.
- Inventario: consulta del historial de movimientos y registro de entradas, salidas y ajustes.
- Clientes: consulta, registro, modificación y desactivación lógica.
- Despachos: creación con detalle de productos, consulta, avance de estado y cancelación.
- Persistencia: entidades relacionadas y migraciones de Entity Framework Core.

## 3. Arquitectura

| Capa | Proyecto | Responsabilidad |
|---|---|---|
| Presentación | `POS.Client` | Formularios Razor, validación visual, navegación y consumo de API. |
| Servicios de interfaz | `POS.Client/Services` | Clientes HTTP tipados, envío del token Bearer y estado de sesión. |
| API | `POS.Server/Controllers` | Endpoints HTTP, autorización, validación de solicitudes y códigos de respuesta. |
| Negocio | `POS.Server/Services` | Reglas de stock, clientes, productos y despachos. |
| Acceso a datos | `POS.Server/Repositories` | Consultas y operaciones sobre entidades persistentes. |
| Modelo compartido | `POS.Shared` | Entidades, DTO y enumeraciones utilizadas por cliente y servidor. |
| Base de datos | SQL Server LocalDB | Persistencia de productos, categorías, usuarios, clientes, existencias y despachos. |

## 4. Modelo relacional utilizado

Las entidades relevantes para la consigna son:

- `Product`: código, nombre, precio, stock actual, stock mínimo, categoría y estado.
- `Category`: clasificación de los productos.
- `Stock`: historial de movimientos, producto, fecha, tipo, cantidad, observaciones y usuario responsable.
- `Customer`: nombre, documento, correo, teléfono, dirección, estado y fecha de creación.
- `Dispatch`: número, fecha, cliente, usuario responsable, estado, importe total y observaciones.
- `DispatchDetail`: producto, cantidad, precio unitario e importe de cada línea.
- `User`: cuenta, correo, hash de contraseña, rol y estado.

Relaciones principales: un producto pertenece a una categoría; un producto puede tener muchos movimientos de stock; un despacho pertenece a un cliente y a un usuario; y un despacho contiene varios detalles, cada uno relacionado con un producto.

## 5. Formularios e interfaz

El proyecto incluye las páginas `Products.razor`, `Categories.razor`, `Stock.razor`, `Customers.razor` y `Dispatches.razor`. Los formularios de alta y edición se implementan mediante `ProductDialog.razor`, `CategoryDialog.razor`, `CustomerDialog.razor` y `DispatchDialog.razor`. Los componentes dialog permiten ingresar o modificar datos sin abandonar la pantalla del módulo.

La interfaz muestra información de inventario, niveles mínimos, estados de clientes y estados de despacho. La disponibilidad de acciones se controla según el rol y el estado de los registros.

## 6. Controladores y endpoints

| Controlador | Operación | Ruta |
|---|---|---|
| ProductsController | Listar / consultar | `GET api/Products`, `GET api/Products/{id}` |
| ProductsController | Crear / editar / baja lógica | `POST api/Products`, `PUT api/Products/{id}`, `DELETE api/Products/{id}` |
| CategoriesController | CRUD de categorías | `api/Categories` |
| StocksController | Consultar movimientos | `GET api/Stocks?productId={id}` |
| StocksController | Registrar movimiento | `POST api/Stocks` |
| CustomersController | Consultar / crear / editar / desactivar | `api/Customers` |
| DispatchesController | Consultar despachos | `GET api/Dispatches`, `GET api/Dispatches/{id}` |
| DispatchesController | Crear / cambiar estado / cancelar | `POST api/Dispatches`, `PUT api/Dispatches/{id}/status`, `POST api/Dispatches/{id}/cancel` |
| HealthController | Verificar API y base de datos | `GET api/Health` |

Las rutas de creación y modificación están protegidas mediante autenticación JWT. Las operaciones administrativas, como el mantenimiento del catálogo y la desactivación de registros, requieren el rol `Administrador` cuando así se define en el controlador.

## 7. Lógica de negocio y transacciones

### 7.1 Movimientos de inventario

`StockService.RegisterMovementAsync` valida que el producto exista y que la cantidad sea mayor que cero. Después inicia una transacción de base de datos:

1. Entrada: incrementa el stock actual.
2. Salida: descuenta unidades únicamente si existe stock suficiente.
3. Ajuste: establece la cantidad física indicada; el endpoint restringe esta operación al administrador.
4. Registra el movimiento en `Stocks` con fecha, tipo, cantidad, observación y usuario responsable.
5. Confirma la transacción. Si ocurre un error, revierte la operación.

### 7.2 Registro de despachos

`DispatchService.CreateAsync` valida cliente activo, productos existentes y activos, cantidades positivas y stock suficiente. Agrupa líneas repetidas del mismo producto, calcula importes y abre una transacción. Dentro de ella descuenta las existencias mediante una actualización condicionada al stock disponible, crea el registro de despacho y sus detalles, y registra movimientos de salida. Si alguna operación falla, se revierte la transacción para evitar que el despacho quede guardado sin el descuento correspondiente o viceversa.

### 7.3 Cancelación de despachos

La cancelación no está permitida para despachos entregados o ya cancelados. La operación cambia el estado, repone las cantidades de los detalles y registra los movimientos de entrada dentro de una misma transacción. Si falla la reposición o el registro histórico, se revierte el cambio.

### 7.4 Clientes y productos

Los servicios validan los campos obligatorios y evitan duplicar documentos de clientes o códigos de productos. La baja de productos y clientes es lógica: se conserva el registro histórico y se marca como inactivo, evitando eliminar físicamente información relacionada con operaciones anteriores.

## 8. Seguridad

- Autenticación mediante JWT Bearer.
- Contraseñas almacenadas como hash BCrypt, no como texto plano.
- Identidad del usuario obtenida de los claims del token para registrar operaciones.
- Autorización por roles para operaciones administrativas.
- Restricción de CORS a los orígenes locales configurados.
- Limitación de intentos de inicio de sesión.
- La clave JWT de desarrollo es únicamente local y debe reemplazarse por un secreto seguro fuera del entorno de pruebas.

## 9. Configuración local de la base de datos y ejecución

Configuración de desarrollo incorporada en el proyecto:

- Base de datos: `POSDB`.
- Motor: SQL Server LocalDB, instancia `(localdb)\MSSQLLocalDB`.
- Backend: `https://localhost:7008/`.
- Frontend: `https://localhost:7282/`.
- CORS: `https://localhost:7282` y `http://localhost:5281`.

Para ejecutar, cerrar instancias antiguas y abrir `Iniciar-POS.bat`. El archivo abre dos terminales: una para `POS.Server` y otra para `POS.Client`. Se deben mantener ambas abiertas. El backend aplica las migraciones al iniciar. El endpoint `https://localhost:7008/api/Health` permite comprobar la disponibilidad de la API y la conexión a la base de datos.

Cuenta local de demostración configurada para recuperación inicial:

- Correo: `admin@pos.local`.
- Contraseña inicial: `SanMagno#2026`.

Después de confirmar el acceso, cambiar `AdminSeed:ResetPassword` a `false` en `POS.Server/appsettings.Development.json` para que el backend no vuelva a restablecer la contraseña en siguientes ejecuciones.

## 10. Plan de pruebas funcionales para la defensa

| N.º | Prueba | Resultado esperado |
|---|---|---|
| 1 | Abrir `api/Health` | Respuesta `status=ok` y `database=connected`. |
| 2 | Iniciar sesión como administrador | Acceso concedido y rol Administrador. |
| 3 | Registrar un producto | El producto aparece en el listado y queda guardado en SQL Server. |
| 4 | Editar un producto | Los cambios persisten al recargar la página. |
| 5 | Registrar entrada de stock | Aumenta el stock y aparece el movimiento histórico. |
| 6 | Registrar salida con stock suficiente | Disminuye el stock y queda el movimiento registrado. |
| 7 | Intentar salida mayor al stock disponible | La operación se rechaza y no modifica existencias. |
| 8 | Registrar un cliente | El cliente aparece en el listado. |
| 9 | Registrar despacho | Se crea el encabezado y sus detalles, se descuenta stock y se registran salidas. |
| 10 | Cancelar despacho no entregado | Se cambia el estado, se repone stock y se registra la entrada. |
| 11 | Repetir documento de cliente o código de producto | Se rechaza el duplicado. |
| 12 | Iniciar sesión con usuario sin rol administrador y ejecutar ajuste | La API deniega la operación administrativa. |

## 11. Evidencia y estado de validación

La solución fuente contiene los formularios, controladores, servicios, repositorios y migraciones descritos. La configuración de conexión y ejecución local fue alineada con la instancia LocalDB y los puertos observados en el entorno de desarrollo. El entorno de generación de este paquete no dispone del SDK .NET ni de SQL Server, por lo que no se declara una compilación o ejecución local ya verificada. Las pruebas de la tabla anterior deben ejecutarse en el equipo donde está instalada la base de datos; las capturas de pantalla de cada resultado deben tomarse directamente de esa ejecución y agregarse a la versión final de evidencias.

## 12. Conclusión

El módulo se organiza en capas separadas y utiliza operaciones CRUD para administrar productos, clientes, inventario y despachos. Las transacciones garantizan la consistencia entre las existencias y el historial de movimientos en las operaciones de entrada, salida, ajuste y despacho. La persistencia se realiza sobre SQL Server mediante Entity Framework Core y migraciones, mientras que la API protege las operaciones mediante JWT y autorización por roles.
