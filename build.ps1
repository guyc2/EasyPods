param(
    [string]$Configuration = "Debug"
)

$dotnetDir = Join-Path $HOME ".dotnet"
if (Test-Path $dotnetDir) {
    $env:DOTNET_ROOT = $dotnetDir
    $env:PATH = "$dotnetDir;$env:PATH"
}

Write-Host "Building EasyPods solution ($Configuration)..." -ForegroundColor Cyan
& dotnet build EasyPods.sln -c $Configuration $args
