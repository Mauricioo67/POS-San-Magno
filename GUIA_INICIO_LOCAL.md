# App POS San Magno — guía de ejecución local

## Configuración incorporada

- Base de datos: `POSDB`.
- Motor: SQL Server LocalDB (`(localdb)\MSSQLLocalDB`).
- API: `https://localhost:7008/`.
- Interfaz Blazor WebAssembly: `https://localhost:7282/`.
- Orígenes CORS de desarrollo: `https://localhost:7282` y `http://localhost:5281`.
- El backend aplica las migraciones EF Core al iniciar.

## Inicio recomendado

1. Cierra las ejecuciones antiguas de POS.Server y POS.Client.
2. Verifica que SQL Server LocalDB esté instalado y que exista la instancia `(localdb)\MSSQLLocalDB`.
3. Abre la carpeta extraída del proyecto.
4. Ejecuta `Iniciar-POS.bat` con doble clic.
5. Se abrirán dos ventanas de PowerShell. Mantén ambas abiertas.
6. Abre `https://localhost:7282/login` si el navegador no se abre automáticamente.
7. Si Chrome muestra una advertencia de certificado, ejecuta en una terminal: `dotnet dev-certs https --trust` y vuelve a abrir el sitio.

## Cuenta de administrador de desarrollo

- Correo: `admin@pos.local`
- Contraseña inicial: `SanMagno#2026`

La configuración de desarrollo restablece esta contraseña para la cuenta `admin@pos.local` al iniciar el backend. Después de comprobar el acceso, cambia `AdminSeed:ResetPassword` de `true` a `false` en `POS.Server/appsettings.Development.json` para que un inicio posterior no vuelva a restablecer la contraseña.

La clave JWT incluida en `appsettings.Development.json` es exclusivamente para desarrollo local. No debe reutilizarse en producción. En esta máquina ya se había configurado `JwtSettings:SecretKey` mediante User Secrets; si ese secreto local existe, prevalece sobre el valor de desarrollo del archivo.

## Si vuelve a aparecer el error de conexión

- Comprueba que la ventana `POS.Server - API` muestre `Now listening on: https://localhost:7008`.
- Comprueba que `POS.Client/wwwroot/appsettings.json` contenga `https://localhost:7008/`.
- En el navegador usa `Ctrl + Shift + R` para recargar sin caché.
- Si el error persiste, abre DevTools > Application > Service Workers y elimina cualquier worker registrado para `localhost`; el cliente ahora evita registrar el service worker en desarrollo.
- Comprueba que ningún proceso antiguo esté ocupando los puertos 7008 o 7282.

## Funciones de la consigna

La solución contiene módulos de productos, categorías, clientes, stock y despachos. Los despachos descuentan existencias y registran el movimiento de salida dentro de una transacción; su cancelación repone existencias y registra el movimiento inverso dentro de otra transacción. Los movimientos manuales de stock se guardan junto con la actualización de existencias en una transacción. La evidencia final debe comprobarse en ejecución con la base de datos local.
