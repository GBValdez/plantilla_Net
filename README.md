# Pedidos · Plantilla .NET

API ASP.NET Core/EF Core 10.0.14 con PostgreSQL, usuarios, roles, clientes y bases reutilizables para catálogos. SDK fijado en `global.json`: .NET 10.0.403. El [frontend Angular](https://github.com/GBValdez/plantilla_front) está en su propio repositorio.

## Configurar

1. Instala el SDK compatible y PostgreSQL.
2. Copia `appsettings.Local.example.json` a `appsettings.Local.json`.
3. Configura una base nueva, SMTP con TLS y el correo/contraseña de `Bootstrap` para la cuenta administradora inicial.
4. Genera una clave JWT aleatoria desde la raíz del repositorio:

```powershell
$configPath = Join-Path $PWD 'appsettings.Local.json'
$config = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json
$config.Jwt.Key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
$config | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $configPath -Encoding utf8
```

El archivo local está excluido de Git. También se admiten secretos de usuario y variables como `Jwt__Key`, `Email__Password` y `ConnectionStrings__DefaultConnection`. Nunca subas contraseñas reales al repositorio.

## Inicializar y arrancar

Inicializa una sola vez una base **nueva**:

```powershell
dotnet run --launch-profile http -- --initialize-database
```

Se crean los roles `ADMINISTRATOR` y `userNormal` y el administrador configurado. No cambia la contraseña de una cuenta existente ni eleva una cuenta normal. `EnsureCreated` es para arranque de una base nueva; usa migraciones para evolucionar una base con datos. Retira la contraseña Bootstrap de la configuración una vez inicializada.

```powershell
dotnet run --launch-profile http
```

API: http://localhost:5279. Salud: `/health`. OpenAPI sólo en desarrollo: `/openapi/v1.json`. El registro y recuperación requieren SMTP. Las credenciales ficticias de la demo Angular no son cuentas de esta API.

También puedes utilizar `.\Iniciar.ps1` y `.\Iniciar.ps1 -Inicializar`.

## Comprobar

```powershell
dotnet test tests/PanelBase.Api.Tests/PanelBase.Api.Tests.csproj
```

O ejecuta `.\Verificar.ps1`. Las pruebas usan SQLite temporal y correo simulado; no necesitan PostgreSQL ni envían correo real.

## Integración y seguridad

JWT valida firma, emisor, audiencia, vigencia, cuenta activa y sello de seguridad. Los endpoints administrativos requieren `ADMINISTRATOR`. Registro transaccional, límites de solicitudes, bloqueo por intentos incorrectos y mensajes uniformes de recuperación están incluidos. `ObjectMapper` realiza mapeos explícitos.

En producción configura HTTPS, orígenes CORS, dominio de `AllowedHosts`, URL pública de `FrontUrl`, persistencia de claves Data Protection y proxies de confianza. `/health` no verifica la conexión a PostgreSQL ni SMTP.

Para extender catálogos, deriva de la base abstracta `cataloguesController`, define `codCatalogue`, rutas/autorización y el tipo de catálogo, y añade mapeos en `utils/mapping/ObjectMapper.cs` y las pruebas de tus reglas de negocio.
