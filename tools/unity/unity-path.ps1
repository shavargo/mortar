function Resolve-UnityPath([string]$project, [string]$override) {
	if ($override) { return $override }
	$versionFile = Join-Path $project 'ProjectSettings\ProjectVersion.txt'
	$version = (Select-String -Path $versionFile -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value.Trim()
	$env:UNITY_NO_BANNER = '1'
	$editorRoot = (& unity editors path $version).Trim()
	if (-not $editorRoot) { throw "Unity $version is not installed; set MORTAR_UNITY to Unity.exe" }
	return Join-Path $editorRoot 'Editor\Unity.exe'
}
