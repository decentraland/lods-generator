$coords = Get-Content -Path "CoordinatesToTest.txt"
$logFileSuccess = "command_log_success.txt"
$logFileFailed = "command_log_fail.txt"

# Ensure the log file is ready
if (Test-Path $logFileSuccess) {
    Clear-Content $logFileSuccess
} else {
    New-Item $logFileSuccess -ItemType File
}

# Ensure the log file is ready
if (Test-Path $logFileFailed) {
    Clear-Content $logFileFailed
} else {
    New-Item $logFileFailed -ItemType File
}

foreach ($coord in $coords) {
    Write-Host "Processing coordinate: $coord"
    $output = & npm run start --coords=$coord --overwrite 2>&1
    $fullOutput = $output -join "`n"  # Join all output lines for full context in logs

    # Check if the success message is in the output
    if ($output -match "Generated output!") {
        "${coord}`tSuccess`t" | Out-File $logFileSuccess -Append
        Write-Host "Success. Output indicates 'Generated output!'"
    } else {
        "${coord}`tError detected`t$fullOutput" | Out-File $logFileFailed -Append
        Write-Host "Error detected in output. Check log for details."
    }
}