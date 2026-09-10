$dotnetDir = Join-Path $HOME ".dotnet"
if (Test-Path $dotnetDir) {
    $env:DOTNET_ROOT = $dotnetDir
    $env:PATH = "$dotnetDir;$env:PATH"
}

Write-Host "Running EasyPods tests..." -ForegroundColor Magenta
& dotnet test EasyPods.sln $args
