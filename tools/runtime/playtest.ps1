[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('Start', 'Stop', 'Status')][string]$Action,
    [string]$PlaytestDirectory,
    [string]$ArtifactsDirectory,
    [switch]$Baseline,
    [string]$Encounter,
    [string]$Residential,
    [ValidateNotNullOrEmpty()][string]$Character,
    [switch]$Capture
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $PlaytestDirectory) { $PlaytestDirectory = Join-Path $PSScriptRoot '../../local/playtest' }
if (-not $ArtifactsDirectory) { $ArtifactsDirectory = Join-Path $PSScriptRoot '../../artifacts' }
if (([int][bool]$Baseline + [int][bool]$Encounter + [int][bool]$Residential) -gt 1) { throw 'Baseline, Encounter, and Residential are mutually exclusive.' }

function Assert-PlainPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    $cursor = $full
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Reparse paths are not allowed: $cursor"
            }
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
    return $full.TrimEnd('\', '/')
}
function Quote-WindowsArgument([string]$Value) {
    # CommandLineToArgvW/CRT rules: double slashes before quotes and closing quote.
    $escaped = [regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}
function Assert-OwnedStage([string]$Directory) {
    $owned = $false
    try {
        $markerPath = Assert-PlainPath (Join-Path $Directory '.malcolm-playtest.json')
        $marker = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
        $owned = $marker.schemaVersion -eq 1 -and $marker.owner -ceq 'malcolm-mod-runtime'
    } catch { $owned = $false }
    if (-not $owned) { throw 'Playtest directory is not owned by malcolm-mod-runtime.' }
    $exe = Assert-PlainPath (Join-Path $Directory 'Malcolm.Runtime.exe')
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Staged Malcolm.Runtime.exe is missing.' }
    return $exe
}
function Get-TrackedProcess($Session, [string]$ExpectedExecutable) {
    if ($Session.schemaVersion -ne 1 -or $Session.owner -cne 'malcolm-playtest-controls' -or
        -not [string]::Equals($Session.executable, $ExpectedExecutable, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Tracked process identity mismatch; no process was stopped.'
    }
    try { $process = [Diagnostics.Process]::GetProcessById([int]$Session.pid) }
    catch [ArgumentException] { return $null }
    try {
        # Retain the OS handle across identity verification and Kill to avoid PID reuse.
        $null = $process.Handle
        $actualExe = $process.MainModule.FileName
        $actualStart = $process.StartTime.ToUniversalTime().Ticks.ToString()
        if (-not [string]::Equals($actualExe, $ExpectedExecutable, [StringComparison]::OrdinalIgnoreCase) -or
            $actualStart -cne [string]$Session.startTimeUtcTicks) {
            throw 'Tracked process identity mismatch; no process was stopped.'
        }
        return $process
    } catch { $process.Dispose(); throw }
}

$stage = Assert-PlainPath $PlaytestDirectory
$artifacts = Assert-PlainPath $ArtifactsDirectory
$sessionPath = Assert-PlainPath (Join-Path $artifacts 'playtest-session.json')
if ($Action -ne 'Start' -and -not (Test-Path -LiteralPath $sessionPath)) {
    Write-Output 'No tracked playtest session. Normal Steam launch remains unchanged.'
    exit 0
}
$executable = Assert-OwnedStage $stage
if ($Action -eq 'Start' -and $Character) {
    $Character = Assert-PlainPath $Character
    if (-not (Test-Path -LiteralPath $Character -PathType Leaf) -or [IO.Path]::GetFileName($Character) -cne 'manifest.json') {
        throw 'Character must identify an existing manifest.json file.'
    }
}
if ($Action -eq 'Start' -and $Residential) {
    $Residential = Assert-PlainPath $Residential
    if (-not (Test-Path -LiteralPath $Residential -PathType Container)) { throw "Residential art directory missing: $Residential" }
    foreach ($name in @('home.png', 'street.png', 'park.png')) {
        $art = Assert-PlainPath (Join-Path $Residential $name)
        if (-not (Test-Path -LiteralPath $art -PathType Leaf)) { throw "Residential art missing: $art" }
    }
}
if ($Action -eq 'Start' -and -not $Baseline -and -not $Residential) {
    if (-not $Encounter) { $Encounter = Join-Path $PSScriptRoot '../../encounters/episode1-lobby.json' }
    $Encounter = Assert-PlainPath $Encounter
    if (-not (Test-Path -LiteralPath $Encounter -PathType Leaf)) { throw "Encounter configuration missing: $Encounter" }
}
$controlLock = $null
if ($Action -ne 'Status') {
    $lockPath = Assert-PlainPath (Join-Path $artifacts 'playtest-session.lock')
    [IO.Directory]::CreateDirectory($artifacts) | Out-Null
    $controlLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
}
try {
$session = $null
$tracked = $null
if (Test-Path -LiteralPath $sessionPath) {
    $session = Get-Content -LiteralPath $sessionPath -Raw | ConvertFrom-Json
    $tracked = Get-TrackedProcess $session $executable
}
if ($Action -eq 'Status') {
    if ($null -eq $tracked) { Write-Output "Tracked playtest has exited. Session: $sessionPath" }
    else { Write-Output "Playtest running. PID: $($tracked.Id). Session: $sessionPath"; $tracked.Dispose() }
    exit 0
}
if ($Action -eq 'Stop') {
    if ($null -ne $tracked) {
        try { $tracked.Kill(); $tracked.WaitForExit(); } finally { $tracked.Dispose() }
    }
    Write-Output 'Playtest stopped. For rollback, launch the game normally through Steam; the installation was not changed.'
    exit 0
}
if ($null -ne $tracked) {
    $tracked.Dispose()
    throw 'A tracked playtest is already running. Stop it before starting another.'
}
[IO.Directory]::CreateDirectory($artifacts) | Out-Null
if ($null -ne $session) {
    # Keep completed session evidence, using a fresh filename and no overwrite.
    [IO.File]::Move($sessionPath, (Join-Path $artifacts ('session-' + [Guid]::NewGuid().ToString() + '.json')))
}
$id = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N')
$log = Join-Path $artifacts ($id + '.log')
$bitmap = $null
if ($Capture) { $bitmap = Join-Path $artifacts ($id + '.bmp') }
$arguments = @($stage, $log)
if ($Baseline) { $arguments += '--baseline' } elseif ($Residential) { $arguments += @('--residential', $Residential) } else { $arguments += @('--encounter', $Encounter) }
if ($Character) { $arguments += @('--character', $Character) }
$serialized = ($arguments | ForEach-Object { Quote-WindowsArgument $_ }) -join ' '
# CreateNew prevents concurrent launches from overwriting the active identity.
$stream = [IO.File]::Open($sessionPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
$child = $null
$previousCapture = [Environment]::GetEnvironmentVariable('MALCOLM_CAPTURE_FRAME', 'Process')
try {
    [Environment]::SetEnvironmentVariable('MALCOLM_CAPTURE_FRAME', $bitmap, 'Process')
    $child = Start-Process -FilePath $executable -ArgumentList $serialized -WorkingDirectory $stage -WindowStyle Normal -PassThru
    $record = [ordered]@{
        schemaVersion = 1; owner = 'malcolm-playtest-controls'; pid = $child.Id
        startTimeUtcTicks = $child.StartTime.ToUniversalTime().Ticks.ToString()
        executable = $executable; log = $log; capture = $bitmap
        baseline = [bool]$Baseline; encounter = $Encounter; residential = $Residential
        character = $Character
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($record | ConvertTo-Json))
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Flush()
} catch {
    # Only the freshly started child can be cleaned up on a recording failure.
    if ($null -ne $child -and -not $child.HasExited) { $child.Kill(); $child.WaitForExit() }
    $stream.Dispose()
    [IO.File]::Delete($sessionPath)
    throw
} finally {
    [Environment]::SetEnvironmentVariable('MALCOLM_CAPTURE_FRAME', $previousCapture, 'Process')
    $stream.Dispose()
    if ($null -ne $child) { $child.Dispose() }
}
Write-Output "Playtest started. Session: $sessionPath"
Write-Output "Log: $log"
if ($Capture) { Write-Output "Capture: $bitmap" }
} finally {
    if ($null -ne $controlLock) { $controlLock.Dispose() }
}
