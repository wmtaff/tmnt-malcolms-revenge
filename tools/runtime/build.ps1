[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$SourceGameDirectory,
    [string]$PlaytestDirectory = (Join-Path $PSScriptRoot '../../local/playtest'),
    [string]$DependencyDirectory = (Join-Path $PSScriptRoot '../../local/dependencies')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Canonicalize existing ancestors through file handles, including 8.3/subst aliases.
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
public static class MalcolmBuildPaths {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFile(string path, uint access, uint share,
        IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint GetFinalPathNameByHandle(SafeFileHandle handle,
        StringBuilder path, uint size, uint flags);
    public static string Canonical(string path) {
        using (var handle = CreateFile(path, 0, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero)) {
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            var result = new StringBuilder(32768);
            uint length = GetFinalPathNameByHandle(handle, result, (uint)result.Capacity, 0);
            if (length == 0 || length >= result.Capacity)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            string value = result.ToString();
            if (!value.StartsWith(@"\\?\") || value.StartsWith(@"\\?\UNC\"))
                throw new InvalidOperationException("Only local paths are supported.");
            return value.Substring(4).TrimEnd('\\');
        }
    }
}
'@

function Get-Sha256([string]$Path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}

# Reject junctions/symlinks instead of trusting lexical path comparisons through them.
function Get-SafePath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    if ($full -notmatch '^[A-Za-z]:\\' -or $full.Length -le 3) {
        throw "Use an absolute local directory below a drive root: $Path"
    }
    $cursor = $full
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Reparse points are not allowed: $cursor"
            }
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
    $ancestor = $full
    $suffix = ''
    while (-not (Test-Path -LiteralPath $ancestor)) {
        $suffix = '\' + [IO.Path]::GetFileName($ancestor) + $suffix
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
    return [MalcolmBuildPaths]::Canonical($ancestor) + $suffix
}
function Assert-Separate([string]$A, [string]$B) {
    if ($A.Equals($B, [StringComparison]::OrdinalIgnoreCase) -or
        $A.StartsWith($B + '\', [StringComparison]::OrdinalIgnoreCase) -or
        $B.StartsWith($A + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Directories overlap: $A and $B"
    }
}
function Assert-PlainTree([string]$Path) {
    foreach ($item in Get-ChildItem -LiteralPath $Path -Force) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Reparse points are not allowed: $($item.FullName)"
        }
        if ($item.PSIsContainer) { Assert-PlainTree $item.FullName }
    }
}
function Install-File([string]$Source, [string]$Target) {
    $parent = [IO.Path]::GetDirectoryName($Target)
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    $temporary = Join-Path $parent ([Guid]::NewGuid().ToString() + '.tmp')
    [IO.File]::Copy($Source, $temporary)
    # Unlink an existing entry: never overwrite a possible hard link to the source.
    if ([IO.File]::Exists($Target)) { [IO.File]::Delete($Target) }
    [IO.File]::Move($temporary, $Target)
}

$source = Get-SafePath $SourceGameDirectory
$destination = Get-SafePath $PlaytestDirectory
$dependencies = Get-SafePath $DependencyDirectory
Assert-Separate $source $destination
Assert-Separate $source $dependencies
Assert-Separate $destination $dependencies
if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw 'Source game directory does not exist.' }
foreach ($directory in @($destination, $dependencies)) {
    if (Test-Path -LiteralPath $directory) {
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) { throw "Not a directory: $directory" }
        Assert-PlainTree $directory
    }
}
$markerPath = Join-Path $destination '.malcolm-playtest.json'
if ((Test-Path -LiteralPath $destination) -and @(Get-ChildItem -LiteralPath $destination -Force).Count -gt 0) {
    $owned = $false
    try {
        $marker = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
        $owned = $marker.schemaVersion -eq 1 -and $marker.owner -eq 'malcolm-mod-runtime'
    } catch { $owned = $false }
    if (-not $owned) { throw 'Nonempty playtest directory is not owned by malcolm-mod-runtime.' }
}
Assert-PlainTree $source
$expected = @{
    'TMNT.exe' = '36435CC7E1063F76E4641C92F95601414B62C1FAEDA39A64CCC270EEF6D82CC6'
    'ParisEngine.dll' = '02FAE3072962C2304F9F086DB1680D8111D24CBF2F16A362C262809C62E4E7DE'
    'ParisSerializers.dll' = '12A8F1B1288664D343EE8E8E68BB5EB593696D4CEB62D6ADD27AF5B72B81F554'
}
foreach ($name in $expected.Keys) {
    $path = Join-Path $source $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        (Get-Sha256 $path) -ne $expected[$name]) {
        throw "Unsupported game build: $name does not match the verified SHA256."
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $source 'Content') -PathType Container)) { throw 'Source Content directory is missing.' }
$compiler = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$launcherSources = @((Join-Path $PSScriptRoot 'RuntimeLauncher.cs'), (Join-Path $PSScriptRoot 'RuntimeLauncherTests.cs'))
foreach ($required in @($compiler) + $launcherSources) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required build input missing: $required" }
}

# All local safety/build preflight above finishes before any filesystem mutation/network.
[IO.Directory]::CreateDirectory($dependencies) | Out-Null
$package = Join-Path $dependencies 'lib.harmony.2.2.1.nupkg'
$packageHash = 'EEDA9350790EE24606A60D8A4CDAEE87A143D12BFEACE6D4952959EA2DB6917C'
if (-not (Test-Path -LiteralPath $package)) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $download = Join-Path $dependencies ([Guid]::NewGuid().ToString() + '.download')
    Invoke-WebRequest -UseBasicParsing -Uri 'https://api.nuget.org/v3-flatcontainer/lib.harmony/2.2.1/lib.harmony.2.2.1.nupkg' -OutFile $download
    if ((Get-Sha256 $download) -ne $packageHash) { throw 'Downloaded Harmony package SHA256 mismatch.' }
    [IO.File]::Move($download, $package)
}
if ((Get-Sha256 $package) -ne $packageHash) { throw 'Cached Harmony package SHA256 mismatch.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    # Extract only the required library; bound size and never trust archive paths.
    $entry = $archive.GetEntry('lib/net45/0Harmony.dll')
    if ($null -eq $entry -or $entry.Length -gt 20MB) { throw 'Required bounded net45 Harmony library missing.' }
    $libraryDirectory = Join-Path $dependencies 'lib.harmony.2.2.1/lib/net45'
    [IO.Directory]::CreateDirectory($libraryDirectory) | Out-Null
    $harmony = Join-Path $libraryDirectory '0Harmony.dll'
    if ([IO.File]::Exists($harmony)) { [IO.File]::Delete($harmony) }
    [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $harmony)
} finally { $archive.Dispose() }

[IO.Directory]::CreateDirectory($destination) | Out-Null
$markerTemp = Join-Path $dependencies ([Guid]::NewGuid().ToString() + '.json')
'{"schemaVersion":1,"owner":"malcolm-mod-runtime"}' | Set-Content -LiteralPath $markerTemp -Encoding UTF8
Install-File $markerTemp $markerPath
[IO.File]::Delete($markerTemp)
foreach ($file in Get-ChildItem -LiteralPath $source -File -Force) {
    if ($file.Name -eq '.malcolm-playtest.json') { continue }
    Install-File $file.FullName (Join-Path $destination $file.Name)
}
$content = Join-Path $source 'Content'
[IO.Directory]::CreateDirectory((Join-Path $destination 'Content')) | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $content -File -Recurse -Force) {
    Install-File $file.FullName (Join-Path $destination $file.FullName.Substring($source.Length + 1))
}
Install-File $harmony (Join-Path $destination '0Harmony.dll')
$compiled = Join-Path $dependencies ([Guid]::NewGuid().ToString() + '.exe')
& $compiler /nologo /platform:x64 /target:exe "/reference:$harmony" "/out:$compiled" $launcherSources
if ($LASTEXITCODE -ne 0) { throw "Runtime compilation failed with exit code $LASTEXITCODE" }
Install-File $compiled (Join-Path $destination 'Malcolm.Runtime.exe')
[IO.File]::Delete($compiled)
Write-Output "Built runtime proof in $destination. No game was launched."
