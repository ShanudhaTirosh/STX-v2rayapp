<#
 Builds a clean, shippable STX release. Run from the repository root in PowerShell on Windows:
   .\scripts\package-release.ps1 -Version 2.1.0
 Output: dist\STX-Enterprises-<version>-win-x64.zip  (+ .sha256)
 What it deliberately leaves OUT of the package: guiConfigs\ (account IDs, server addresses), guiLogs\, binConfigs\ (generated
 configs contain the server and UUID), bin\cache.db, scratch\, build outputs, PDBs.
 NOT TESTED in the audit environment (Linux, no Windows SDK) - run it once on your build PC and smoke-test the zip.
#>
param(
  [Parameter(Mandatory = $true)][string]$Version,
  [string]$SignThumbprint = ""   # optional code-signing certificate thumbprint (strongly recommended: fixes most SmartScreen warnings)
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$out = Join-Path $root "dist"
$stage = Join-Path $out "stage"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

Write-Host "== self-tests =="
dotnet run --project tests\STX.SelfTest\STX.SelfTest.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "self-tests failed - not packaging" }

Write-Host "== publish =="
dotnet publish src\STXEnterprises\STXEnterprises.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:Version=$Version -p:AssemblyVersion="$Version.0" -p:FileVersion="$Version.0" -p:InformationalVersion=$Version `
  -o $stage
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

Write-Host "== assets =="
Copy-Item wwwroot (Join-Path $stage "wwwroot") -Recurse
New-Item -ItemType Directory -Path (Join-Path $stage "bin") | Out-Null
Copy-Item bin\xray (Join-Path $stage "bin\xray") -Recurse
Copy-Item bin\sing_box (Join-Path $stage "bin\sing_box") -Recurse
Copy-Item README.txt, docs\CHANGELOG.md $stage
Get-ChildItem $stage -Recurse -Include *.pdb,*.xml | Remove-Item -Force

if ($SignThumbprint) {
  Write-Host "== sign =="
  Get-ChildItem $stage -Recurse -Include STXEnterprises.exe | ForEach-Object {
    signtool sign /sha1 $SignThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $_.FullName
  }
}

$zip = Join-Path $out "STX-Enterprises-$Version-win-x64.zip"
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
(Get-FileHash $zip -Algorithm SHA256).Hash.ToLower() + "  " + (Split-Path $zip -Leaf) | Set-Content "$zip.sha256"
Write-Host "Done: $zip"
Write-Host "Publish the .sha256 next to the download so users can verify it."
