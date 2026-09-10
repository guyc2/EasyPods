param(
    [string]$Configuration = "Debug"
)

$dotnetDir = Join-Path $HOME ".dotnet"
if (Test-Path $dotnetDir) {
    $env:DOTNET_ROOT = $dotnetDir
    $env:PATH = "$dotnetDir;$env:PATH"
}

Write-Host "Launching EasyPods WPF UI ($Configuration)..." -ForegroundColor Green
& dotnet run --project src/EasyPods.View/EasyPods.View.csproj -c $Configuration $args
