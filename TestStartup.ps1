# Test startup functionality
Write-Host "=== Testing Startup Manager ===" -ForegroundColor Cyan

# Check current status
$regPath = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
$appName = "TrafficPilot"

Write-Host "`nCurrent registry value:" -ForegroundColor Yellow
$currentValue = Get-ItemProperty -Path $regPath -Name $appName -ErrorAction SilentlyContinue
if ($currentValue) {
    Write-Host "  $appName = $($currentValue.$appName)" -ForegroundColor Green
} else {
    Write-Host "  (not set)" -ForegroundColor Gray
}

# Test the application
Write-Host "`nChecking TrafficPilot.exe locations:" -ForegroundColor Yellow
$locations = @(
    "D:\TrafficPilot\TrafficPilot.exe",
    "D:\source\TrafficPilot\bin\Debug\net10.0-windows\TrafficPilot.exe",
    "D:\source\TrafficPilot\bin\Release\net10.0-windows\TrafficPilot.exe"
)

foreach ($loc in $locations) {
    if (Test-Path $loc) {
        Write-Host "  ✓ Found: $loc" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Not found: $loc" -ForegroundColor Gray
    }
}

Write-Host "`nEnvironment.ProcessPath would return:" -ForegroundColor Yellow
Write-Host "  (will be set at runtime when application runs)" -ForegroundColor Gray

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
