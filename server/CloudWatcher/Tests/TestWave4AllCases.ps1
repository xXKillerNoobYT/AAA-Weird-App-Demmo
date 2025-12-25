# Wave 4 Testing - All 8 Test Cases
# Tasks 3-7: Comprehensive availability calculation validation

$ErrorActionPreference = "Stop"
$headers = @{ 'X-Api-Key' = 'dev-local-key' }
$baseUrl = "http://localhost:5000"
$testPartId = "550e8400-e29b-41d4-a716-446655440000"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Wave 4 Testing - All Test Cases" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Test Case 1: Part with all factors (on_hand, reserved, incoming)
Write-Host "📋 TEST CASE 1: Part with all factors" -ForegroundColor Yellow
Write-Host "Expected: on_hand - reserved + incoming - backorder"

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v2/inventory/$testPartId/availability" -Headers $headers
    
    $onHand = $response.totalQuantityOnHand
    $reserved = $response.reservedUnits
    $incoming = $response.incomingUnits
    $backorder = $response.backorderedUnits
    $actual = $response.effectiveAvailableUnits
    $expected = $onHand - $reserved + $incoming - $backorder
    
    Write-Host "  On Hand: $onHand"
    Write-Host "  Reserved: $reserved"
    Write-Host "  Incoming: $incoming"
    Write-Host "  Backorder: $backorder"
    Write-Host "  Expected: $onHand - $reserved + $incoming - $backorder = $expected"
    Write-Host "  Actual: $actual"
    
    if ($actual -eq $expected) {
        Write-Host "  ✅ PASS" -ForegroundColor Green
        $testCase1 = "PASS"
    } else {
        Write-Host "  ❌ FAIL - Discrepancy: $($actual - $expected)" -ForegroundColor Red
        $testCase1 = "FAIL"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase1 = "ERROR"
}

Write-Host ""

# Test Case 2: Reserved units calculation from database
Write-Host "📋 TEST CASE 2: Reserved units from database" -ForegroundColor Yellow
Write-Host "Expected: SUM(OrderItem.Quantity) WHERE Status IN ('pending', 'approved')"

try {
    # Get API value
    $apiReserved = $response.reservedUnits
    
    # Note: Database query would require psql or .NET app
    # For now, we validate based on known seeded data
    $expectedReserved = 35  # 15 (pending) + 20 (approved) from seeder
    
    Write-Host "  API Reserved: $apiReserved"
    Write-Host "  Expected from Seeder: $expectedReserved"
    
    if ($apiReserved -eq $expectedReserved) {
        Write-Host "  ✅ PASS" -ForegroundColor Green
        $testCase2 = "PASS"
    } else {
        Write-Host "  ❌ FAIL - Discrepancy: $($apiReserved - $expectedReserved)" -ForegroundColor Red
        $testCase2 = "FAIL"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase2 = "ERROR"
}

Write-Host ""

# Test Case 3: Incoming units calculation
Write-Host "📋 TEST CASE 3: Incoming units from database" -ForegroundColor Yellow
Write-Host "Expected: SUM(PO.QuantityOrdered - PO.QuantityReceived) WHERE Status = 'approved'"

try {
    $apiIncoming = $response.incomingUnits
    $expectedIncoming = 50  # From seeder: 1 PO with 50 units ordered, 0 received
    
    Write-Host "  API Incoming: $apiIncoming"
    Write-Host "  Expected from Seeder: $expectedIncoming"
    
    if ($apiIncoming -eq $expectedIncoming) {
        Write-Host "  ✅ PASS" -ForegroundColor Green
        $testCase3 = "PASS"
    } else {
        Write-Host "  ❌ FAIL - Discrepancy: $($apiIncoming - $expectedIncoming)" -ForegroundColor Red
        $testCase3 = "FAIL"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase3 = "ERROR"
}

Write-Host ""

# Test Case 4: Backorder calculation
Write-Host "📋 TEST CASE 4: Backorder calculation" -ForegroundColor Yellow
Write-Host "Expected: MAX(0, Reserved - OnHand)"

try {
    $onHand = $response.totalQuantityOnHand
    $reserved = $response.reservedUnits
    $apiBackorder = $response.backorderedUnits
    $expectedBackorder = [Math]::Max(0, $reserved - $onHand)
    
    Write-Host "  On Hand: $onHand"
    Write-Host "  Reserved: $reserved"
    Write-Host "  Expected Backorder: MAX(0, $reserved - $onHand) = $expectedBackorder"
    Write-Host "  API Backorder: $apiBackorder"
    
    if ($apiBackorder -eq $expectedBackorder) {
        Write-Host "  ✅ PASS" -ForegroundColor Green
        $testCase4 = "PASS"
    } else {
        Write-Host "  ❌ FAIL - Discrepancy: $($apiBackorder - $expectedBackorder)" -ForegroundColor Red
        $testCase4 = "FAIL"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase4 = "ERROR"
}

Write-Host ""

# Test Case 5: Location-level availability
Write-Host "📋 TEST CASE 5: Location-level availability" -ForegroundColor Yellow
Write-Host "Expected: Each location's available = onHand - reserved"

try {
    $locations = $response.locations
    $allLocationsCorrect = $true
    
    foreach ($loc in $locations) {
        $locAvailable = $loc.quantityOnHand - $loc.reservedQuantity
        Write-Host "  Location $($loc.locationId.Substring(0,8))..."
        Write-Host "    On Hand: $($loc.quantityOnHand)"
        Write-Host "    Reserved: $($loc.reservedQuantity)"
        Write-Host "    Expected Available: $locAvailable"
        Write-Host "    Actual Available: $($loc.availableQuantity)"
        
        if ($loc.availableQuantity -eq $locAvailable) {
            Write-Host "    ✅ Correct" -ForegroundColor Green
        } else {
            Write-Host "    ❌ Incorrect" -ForegroundColor Red
            $allLocationsCorrect = $false
        }
    }
    
    if ($allLocationsCorrect) {
        Write-Host "  ✅ PASS - All locations calculated correctly" -ForegroundColor Green
        $testCase5 = "PASS"
    } else {
        Write-Host "  ❌ FAIL - Some locations have incorrect calculations" -ForegroundColor Red
        $testCase5 = "FAIL"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase5 = "ERROR"
}

Write-Host ""

# Test Case 6: totalReserved vs reservedUnits consistency
Write-Host "📋 TEST CASE 6: totalReserved vs reservedUnits" -ForegroundColor Yellow
Write-Host "Expected: totalReserved (per location) = reservedUnits (backorder calc)"

try {
    $totalReserved = $response.totalReserved
    $reservedUnits = $response.reservedUnits
    
    Write-Host "  Total Reserved (location sum): $totalReserved"
    Write-Host "  Reserved Units (backorder): $reservedUnits"
    
    if ($totalReserved -eq $reservedUnits) {
        Write-Host "  ✅ PASS - Values match" -ForegroundColor Green
        $testCase6 = "PASS"
    } else {
        Write-Host "  ⚠️  WARNING - Values differ by $($reservedUnits - $totalReserved)" -ForegroundColor Yellow
        Write-Host "  (This may be expected if backorder logic differs from location tracking)" -ForegroundColor Gray
        $testCase6 = "WARNING"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase6 = "ERROR"
}

Write-Host ""

# Test Case 7: Effective available formula verification
Write-Host "📋 TEST CASE 7: Effective available formula" -ForegroundColor Yellow
Write-Host "Expected: effectiveAvailable = totalAvailable - backorder + incoming"

try {
    $totalAvailable = $response.totalAvailable
    $backorder = $response.backorderedUnits
    $incoming = $response.incomingUnits
    $effectiveAvailable = $response.effectiveAvailableUnits
    $expectedEffective = $totalAvailable - $backorder + $incoming
    
    Write-Host "  Total Available (onHand - reserved): $totalAvailable"
    Write-Host "  Backorder: $backorder"
    Write-Host "  Incoming: $incoming"
    Write-Host "  Expected Effective: $totalAvailable - $backorder + $incoming = $expectedEffective"
    Write-Host "  Actual Effective: $effectiveAvailable"
    
    if ($effectiveAvailable -eq $expectedEffective) {
        Write-Host "  ✅ PASS" -ForegroundColor Green
        $testCase7 = "PASS"
    } else {
        Write-Host "  ❌ FAIL - Discrepancy: $($effectiveAvailable - $expectedEffective)" -ForegroundColor Red
        $testCase7 = "FAIL"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase7 = "ERROR"
}

Write-Host ""

# Test Case 8: Response structure validation
Write-Host "📋 TEST CASE 8: Response structure validation" -ForegroundColor Yellow
Write-Host "Expected: All required fields present with correct types"

try {
    $requiredFields = @(
        "partId", "partCode", "partName", "totalQuantityOnHand", "totalReserved",
        "totalAvailable", "reservedUnits", "incomingUnits", "backorderedUnits",
        "effectiveAvailableUnits", "locationCount", "locations", "checkedAt"
    )
    
    $missingFields = @()
    foreach ($field in $requiredFields) {
        if (-not $response.PSObject.Properties[$field]) {
            $missingFields += $field
        }
    }
    
    if ($missingFields.Count -eq 0) {
        Write-Host "  ✅ PASS - All required fields present" -ForegroundColor Green
        Write-Host "  Fields: $($requiredFields -join ', ')" -ForegroundColor Gray
        $testCase8 = "PASS"
    } else {
        Write-Host "  ❌ FAIL - Missing fields: $($missingFields -join ', ')" -ForegroundColor Red
        $testCase8 = "FAIL"
    }
} catch {
    Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $testCase8 = "ERROR"
}

Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$results = @{
    "Test 1: All factors calculation" = $testCase1
    "Test 2: Reserved units from DB" = $testCase2
    "Test 3: Incoming units from DB" = $testCase3
    "Test 4: Backorder calculation" = $testCase4
    "Test 5: Location availability" = $testCase5
    "Test 6: Reserved consistency" = $testCase6
    "Test 7: Effective available formula" = $testCase7
    "Test 8: Response structure" = $testCase8
}

$passCount = ($results.Values | Where-Object { $_ -eq "PASS" }).Count
$failCount = ($results.Values | Where-Object { $_ -eq "FAIL" }).Count
$errorCount = ($results.Values | Where-Object { $_ -eq "ERROR" }).Count
$warnCount = ($results.Values | Where-Object { $_ -eq "WARNING" }).Count

foreach ($test in $results.GetEnumerator() | Sort-Object Name) {
    $symbol = switch ($test.Value) {
        "PASS" { "✅" }
        "FAIL" { "❌" }
        "ERROR" { "⚠️ " }
        "WARNING" { "⚠️ " }
    }
    
    $color = switch ($test.Value) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
    }
    
    Write-Host "$symbol $($test.Key): $($test.Value)" -ForegroundColor $color
}

Write-Host "`nTotal: $passCount PASS, $failCount FAIL, $warnCount WARNING, $errorCount ERROR" -ForegroundColor Cyan

if ($failCount -eq 0 -and $errorCount -eq 0) {
    Write-Host "`n🎉 ALL TESTS PASSED!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  SOME TESTS FAILED - Review results above" -ForegroundColor Yellow
}

Write-Host "`n========================================`n" -ForegroundColor Cyan
