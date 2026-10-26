$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet test 'tests/PanelBase.Api.Tests/PanelBase.Api.Tests.csproj' --disable-build-servers -m:1 -p:UseSharedCompilation=false --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de la API.' }
} finally { Pop-Location }
