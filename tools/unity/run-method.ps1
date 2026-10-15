param(
	[Parameter(Mandatory = $true)][string]$Method,
	[string]$Unity = $env:MORTAR_UNITY
)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $root 'unity\Mortar.Sample'
. (Join-Path $PSScriptRoot 'unity-path.ps1')
$Unity = Resolve-UnityPath $project $Unity

$outDir = Join-Path $root 'out\unity-methods'
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "$($Method -replace '[^A-Za-z0-9]', '-').log"
$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', $Method, '-logFile', "`"$log`"")
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) {
	Write-Host "[ERROR] $Method failed (exit $($process.ExitCode)); see $log"
	exit $process.ExitCode
}
Write-Host "[OK] $Method"
