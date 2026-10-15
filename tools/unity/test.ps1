param(
	[ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode',
	[string]$Filter = '',
	[string]$Unity = $env:MORTAR_UNITY
)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $root 'unity\Mortar.Sample'
. (Join-Path $PSScriptRoot 'unity-path.ps1')
$Unity = Resolve-UnityPath $project $Unity

$outDir = Join-Path $root 'out\unity-tests'
New-Item -ItemType Directory -Force $outDir | Out-Null
$results = Join-Path $outDir "$Platform.xml"
$log = Join-Path $outDir "$Platform.log"
if (Test-Path $results) { Remove-Item $results }

$arguments = @('-batchmode', '-projectPath', "`"$project`"", '-runTests', '-testPlatform', $Platform,
	'-testResults', "`"$results`"", '-logFile', "`"$log`"")
if ($Filter) { $arguments += @('-testFilter', $Filter) }
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow

if (-not (Test-Path $results)) {
	Write-Host "[ERROR] no test results; see $log"
	exit 1
}
[xml]$xml = Get-Content $results
$run = $xml.'test-run'
Write-Host "[$Platform] total $($run.total), passed $($run.passed), failed $($run.failed), skipped $($run.skipped)"
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
	Write-Host "FAILED $($case.fullname)"
	Write-Host $case.failure.message.InnerText
}
exit $process.ExitCode
