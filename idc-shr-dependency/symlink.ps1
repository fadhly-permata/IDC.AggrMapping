Get-ChildItem -Path $PSScriptRoot -File | Where-Object { 
    $_.Name.StartsWith('IDC.Utilities.')
} | ForEach-Object {
    Remove-Item -Path $_.FullName -Force
}

$src = Read-Host "Masukkan path source folder DLL"
if (-not (Test-Path -Path $src -PathType Container)) {
    Write-Host "Path tidak valid. Proses dibatalkan."
    return
}

Get-ChildItem -Path $src -File | ForEach-Object {
    $dst = Join-Path -Path $PSScriptRoot -ChildPath $_.Name
    if (-not (Test-Path $dst)) {
        cmd /c mklink "$dst" "$($_.FullName)"
    }
}