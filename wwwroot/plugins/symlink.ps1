Remove-Item -Path (Join-Path -Path $PSScriptRoot -ChildPath "source") -Recurse -Force -ErrorAction SilentlyContinue

$src = Read-Host "Masukkan path source plugin"
if (-not (Test-Path -Path $src -PathType Container)) {
    Write-Host "Path tidak valid. Proses dibatalkan."
    return
}

$dst = Join-Path -Path $PSScriptRoot -ChildPath "source"
cmd /c mklink /D "$dst" "$src" | Out-Null
Write-Host "Symlink berhasil dibuat dari '$src' ke '$dst'"