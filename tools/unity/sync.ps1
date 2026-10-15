param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$package = Join-Path $root 'unity\com.backwoods.mortar'
$plugins = Join-Path $package 'Plugins'
$streaming = Join-Path $root 'unity\Mortar.Sample\Assets\StreamingAssets'
$publish = Join-Path $root 'out\unity-publish'

# Assemblies Unity already provides; copying them causes duplicate-assembly errors.
$skip = @()

function Get-StableGuid([string]$seed) {
	$md5 = [System.Security.Cryptography.MD5]::Create()
	$bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
	return -join ($bytes | ForEach-Object { $_.ToString('x2') })
}

function Write-FolderMeta([string]$path) {
	$guid = Get-StableGuid "mortar-folder:$([IO.Path]::GetFileName($path))"
	$text = "fileFormatVersion: 2`nguid: $guid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
	[IO.File]::WriteAllText("$path.meta", $text)
}

function Write-PluginMeta([string]$path) {
	$guid = Get-StableGuid "mortar-plugin:$([IO.Path]::GetFileName($path))"
	$text = @"
fileFormatVersion: 2
guid: $guid
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 0
  platformData:
  - first:
      Any: 
    second:
      enabled: 1
      settings: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
	[IO.File]::WriteAllText("$path.meta", ($text -replace "`r`n", "`n"))
}

dotnet publish (Join-Path $root 'src\Mortar.Json\Mortar.Json.csproj') -c $Configuration -f netstandard2.1 -o $publish --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

if (Test-Path $plugins) { Remove-Item $plugins -Recurse -Force }
New-Item -ItemType Directory -Force $plugins | Out-Null
Write-FolderMeta $plugins
$copied = 0
Get-ChildItem $publish -Filter *.dll | Where-Object { $skip -notcontains $_.BaseName } | ForEach-Object {
	$target = Join-Path $plugins $_.Name
	Copy-Item $_.FullName $target
	Write-PluginMeta $target
	$copied++
}

if (Test-Path $streaming) { Remove-Item $streaming -Recurse -Force }
New-Item -ItemType Directory -Force $streaming | Out-Null
Copy-Item (Join-Path $root 'content') (Join-Path $streaming 'content') -Recurse

Write-Host "[SYNC] $copied assemblies -> $plugins"
Write-Host "[SYNC] content -> $streaming"
