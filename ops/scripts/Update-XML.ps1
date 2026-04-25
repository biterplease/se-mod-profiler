param (
    [string]$Version,
    [string]$CommitSha
)
$xmlPath = "SEProfiler.xml"
if (-not (Test-Path $xmlPath)) {
    Write-Error "Could not find $xmlPath"
    exit 1
}
$xmlContent = Get-Content $xmlPath -Raw
Write-Host "Updating $xmlPath to Version: $Version and Commit: $CommitSha"
# Update SEProfiler.Lib version
$xmlContent = $xmlContent -replace '(<PackageReference Include="SEProfiler\.Lib" Version=")[^"]*(")', "`${1}$Version`$2"
# Update Commit SHA
$xmlContent = $xmlContent -replace '(<Commit>)[^<]*(</Commit>)', "`${1}$CommitSha`$2"
Set-Content $xmlPath $xmlContent -NoNewline
Write-Host "Update successful."