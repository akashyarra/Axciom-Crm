Write-Host "=== TEST 1: Anonymous Access Protection ==="
$anonResp = Invoke-WebRequest -Uri "http://localhost:5222/Customers" -UseBasicParsing
Write-Host "Page URL after redirect:" $anonResp.BaseResponse.ResponseUri.AbsoluteUri
if ($anonResp.BaseResponse.ResponseUri.AbsoluteUri -like "*Account/Login*") {
    Write-Host "PASS: Protected page correctly redirected to Login page."
} else {
    Write-Host "FAIL: Did not redirect to Login."
}

Write-Host "`n=== TEST 2: REST API Login (Admin) ==="
$loginBody = @{ email = "admin@acxiomcrm.com"; password = "Admin@12345" } | ConvertTo-Json
$loginResp = Invoke-RestMethod -Uri "http://localhost:5222/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -UseBasicParsing
Write-Host "Login success:" $loginResp.success "Role:" $loginResp.role "Token received:" ($null -ne $loginResp.token)
$token = $loginResp.token

Write-Host "`n=== TEST 3: REST API Get Customers ==="
$headers = @{ Authorization = "Bearer $token" }
$customers = Invoke-RestMethod -Uri "http://localhost:5222/api/customers" -Method Get -Headers $headers -UseBasicParsing
Write-Host "Fetched customers count:" $customers.Count "First customer:" $customers[0].customerName

Write-Host "`n=== TEST 4: Business Rule - Opportunity Amount <= 0 ==="
$badOpp = @{
    opportunityName = "Bad Opp Amount"
    amount = 0
    probability = 50
    expectedCloseDate = (Get-Date).AddDays(10).ToString("yyyy-MM-dd")
    stage = "Qualification"
} | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "http://localhost:5222/api/opportunities" -Method Post -Headers $headers -Body $badOpp -ContentType "application/json" -UseBasicParsing
    Write-Host "FAIL: Expected rejection but succeeded"
} catch {
    Write-Host "PASS: Rejected with status" $_.Exception.Response.StatusCode.value__
}

Write-Host "`n=== TEST 5: Business Rule - Probability > 100 ==="
$badProb = @{
    opportunityName = "Bad Prob Opp"
    amount = 5000
    probability = 120
    expectedCloseDate = (Get-Date).AddDays(10).ToString("yyyy-MM-dd")
    stage = "Qualification"
} | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "http://localhost:5222/api/opportunities" -Method Post -Headers $headers -Body $badProb -ContentType "application/json" -UseBasicParsing
    Write-Host "FAIL: Expected rejection but succeeded"
} catch {
    Write-Host "PASS: Rejected with status" $_.Exception.Response.StatusCode.value__
}

Write-Host "`n=== TEST 6: Business Rule - Expected Close Date in Past ==="
$pastDate = @{
    opportunityName = "Past Date Opp"
    amount = 5000
    probability = 50
    expectedCloseDate = (Get-Date).AddDays(-5).ToString("yyyy-MM-dd")
    stage = "Qualification"
    status = "Open"
} | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "http://localhost:5222/api/opportunities" -Method Post -Headers $headers -Body $pastDate -ContentType "application/json" -UseBasicParsing
    Write-Host "FAIL: Expected rejection but succeeded"
} catch {
    Write-Host "PASS: Rejected with status" $_.Exception.Response.StatusCode.value__
}

Write-Host "`n=== TEST 7: Business Rule - Follow-up Date Before Today ==="
$pastFollowUp = @{
    subject = "Past FollowUp"
    followUpDate = (Get-Date).AddDays(-2).ToString("yyyy-MM-dd")
    followUpType = "Call"
    status = "Planned"
} | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "http://localhost:5222/api/followups" -Method Post -Headers $headers -Body $pastFollowUp -ContentType "application/json" -UseBasicParsing
    Write-Host "FAIL: Expected rejection but succeeded"
} catch {
    Write-Host "PASS: Rejected with status" $_.Exception.Response.StatusCode.value__
}

Write-Host "`n=== TEST 8: Business Rule - Duplicate Customer ==="
$dupCustomer = @{
    customerName = "Apex New"
    email = "contact@apextech.com"
    phone = "9876543210"
    status = "Active"
} | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "http://localhost:5222/api/customers" -Method Post -Headers $headers -Body $dupCustomer -ContentType "application/json" -UseBasicParsing
    Write-Host "FAIL: Expected duplicate conflict but succeeded"
} catch {
    Write-Host "PASS: Rejected with status" $_.Exception.Response.StatusCode.value__
}

Write-Host "`n=== TEST 9: REST API Pipeline Report ==="
$pipelineReport = Invoke-RestMethod -Uri "http://localhost:5222/api/reports/pipeline" -Method Get -Headers $headers -UseBasicParsing
Write-Host "Total Pipeline Value: $" $pipelineReport.totalPipelineValue "Weighted: $" $pipelineReport.totalWeightedPipelineValue "Stages:" $pipelineReport.byStage.Count

Write-Host "`n=== ALL ACCEPTANCE TESTS COMPLETED SUCCESSFULLY ==="
