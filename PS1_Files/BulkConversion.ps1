# Get the directory of the current script
$scriptDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

# Construct the path to the directory on the same level as the script
# Replace 'OutputDirectoryPath' with the actual directory name you're targeting
$outputDirectoryPath = Join-Path -Path $scriptDirectory -ChildPath "built-lods"

# Construct the path to the directory on the same level as the script
$manifestDirectoryPath = Join-Path -Path $scriptDirectory -ChildPath "scene-lod-entities-manifest-builder"

# We install the manifest project
Set-Location -Path $manifestDirectoryPath
Invoke-Expression "npm i"
Invoke-Expression "npm run build"
Set-Location -Path $scriptDirectory


# Define the limit
$limitInt = 5  # Change this to your desired limit

# Initialize an empty array to hold the strings
$ScenesToAnalyze = @()

# Nested loops to generate the strings
for ($i = - $limitInt; $i -le $limitInt; $i++) {
    for ($j = - $limitInt; $j -le $limitInt; $j++) {
	Write-Host "Running for $i,$j"
        Start-Process -FilePath "DCL_Pixyz.exe" -ArgumentList @("coords", "$i,$j", $outputDirectoryPath, $manifestDirectoryPath, "true", "false") -Wait
	Write-Host "End running for $i,$j"    
    }
}
