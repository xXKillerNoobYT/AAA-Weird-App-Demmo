# Wave 4 Testing - Database Seeding Script
# Executes SeedTestData_Wave4.sql using the API endpoint

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$ApiKey = "dev-local-key"
)

Write-Host "🌱 Wave 4 Database Seeding Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Read the SQL script
$sqlScript = Get-Content -Path ".\Data\SeedTestData_Wave4.sql" -Raw

# Create request body
$body = @{
    sql = $sqlScript
} | ConvertTo-Json

# Execute via maintenance endpoint
Write-Host "`n📊 Executing SQL script..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$ApiUrl/api/v2/maintenance/execute-sql" `
        -Method Post `
        -Headers @{
            "X-Api-Key" = $ApiKey
            "Content-Type" = "application/json"
        } `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "✅ Database seeded successfully!" -ForegroundColor Green
    Write-Host $response
}
catch {
    Write-Host "❌ Error seeding database:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    
    if ($_.ErrorDetails) {
        Write-Host "`nDetails:" -ForegroundColor Yellow
        Write-Host $_.ErrorDetails.Message
    }
    
    exit 1
}

Write-Host "`n🔍 Verifying data..." -ForegroundColor Yellow

# Check if part exists
try {
    $part = Invoke-RestMethod -Uri "$ApiUrl/api/v2/inventory/550e8400-e29b-41d4-a716-446655440000" `
        -Method Get `
        -Headers @{ "X-Api-Key" = $ApiKey } `
        -ErrorAction Stop
    
    Write-Host "✅ Part found: $($part.partCode) - $($part.partName)" -ForegroundColor Green
    Write-Host "   Total on hand: $($part.totalOnHand) units"
}
catch {
    Write-Host "⚠️ Could not retrieve part (may not exist yet)" -ForegroundColor Yellow
}

Write-Host "`n✅ Seeding complete!" -ForegroundColor Green
