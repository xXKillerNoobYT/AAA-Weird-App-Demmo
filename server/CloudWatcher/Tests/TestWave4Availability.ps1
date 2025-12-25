# Wave 4 Testing - Test Execution Script
# Tasks 2-7: Endpoint Testing and Calculation Verification

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Wave 4 Testing - Availability Endpoint" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"
$headers = @{"X-Api-Key" = "dev-local-key"}
$testPartId = "550e8400-e29b-41d4-a716-446655440000"

# Task 2: Test availability endpoint
Write-Host "📋 TASK 2: Testing Availability Endpoint" -ForegroundColor Yellow
$availabilityUrl = "$baseUrl/api/v2/inventory/$testPartId/availability"
try {
    $response = Invoke-RestMethod -Uri $availabilityUrl -Method Get -Headers $headers
    Write-Host "✅ Endpoint responded successfully" -ForegroundColor Green
    
    Write-Host "`n📊 Availability Data:" -ForegroundColor Cyan
    Write-Host "  Part: $($response.partCode) - $($response.partName)"
    Write-Host "  Total On Hand: $($response.totalQuantityOnHand) units"
    Write-Host "  Reserved: $($response.reservedUnits) units"
    Write-Host "  Incoming: $($response.incomingUnits) units"
    Write-Host "  Backorder: $($response.backorderedUnits) units"
    Write-Host "  Effective Available: $($response.effectiveAvailableUnits) units"
    Write-Host "  Location Count: $($response.locationCount)"
    
    # Calculate expected vs actual
    $expected = $response.totalQuantityOnHand - $response.reservedUnits + $response.incomingUnits - $response.backorderedUnits
    Write-Host "`n🔍 Calculation Verification:" -ForegroundColor Cyan
    Write-Host "  Expected: $($response.totalQuantityOnHand) - $($response.reservedUnits) + $($response.incomingUnits) - $($response.backorderedUnits) = $expected"
    Write-Host "  Actual: $($response.effectiveAvailableUnits)"
    
    if ($expected -eq $response.effectiveAvailableUnits) {
        Write-Host "  ✅ Calculation CORRECT" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  DISCREPANCY: Expected $expected but got $($response.effectiveAvailableUnits)" -ForegroundColor Yellow
    }
    
    # Store for later verification
    $script:availabilityData = $response
} catch {
    Write-Host "❌ Endpoint test FAILED: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================`n" -ForegroundColor Cyan
Write-Host "✅ TASK 2 COMPLETE" -ForegroundColor Green
Write-Host "`nNext: Verify calculations against database (Tasks 4-7)`n"
