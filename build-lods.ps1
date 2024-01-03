# PowerShell Script

# Check if an argument for sceneId is provided
param (
    [string]$sceneId
)

if (-not $sceneId) {
    Write-Error "No sceneId provided. Usage: .\scriptname.ps1 -sceneId 'your_scene_id'"
    exit
}

# Get the directory of the script
$scriptPath = Get-Location

# Change to the Manifest-builder directory
cd $scriptPath\Manifest-Builder

# Run npm build command with sceneId
$outputManifestBuilder = npm run start --sceneId=$sceneId
Write-Host $outputManifestBuilder

# Change to the Lod-Generator directory
cd $scriptPath\Lod-Generator

# Get output Manifest path
$relativePath = "/Manifest-Builder/output-manifests/"
$fullPath = Join-Path $scriptPath $relativePath

# Run DCL_PiXYZ.exe with the scene ID
$outputLodGeneration = .\DCL_PiXYZ.exe $sceneId $fullPath
Write-Host $outputLodGeneration

cd $scriptPath