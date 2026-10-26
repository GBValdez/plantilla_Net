param([switch]$Inicializar)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if ($Inicializar) { dotnet run --launch-profile http -- --initialize-database }
    else { dotnet run --launch-profile http }
    if ($LASTEXITCODE -ne 0) { throw 'La API no pudo iniciar. Revisa README.md.' }
} finally { Pop-Location }
