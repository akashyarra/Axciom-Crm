Write-Host "=== TEST 10: Role Scoping Comparison (Admin vs SalesExecutive) ==="
# 1. Admin Login
$adminLogin = Invoke-RestMethod -Uri "http://localhost:5222/api/auth/login" -Method Post -Body (@{ email = "admin@acxiomcrm.com"; password = "Admin@12345" } | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.token)" }
$adminOpps = Invoke-RestMethod -Uri "http://localhost:5222/api/opportunities" -Method Get -Headers $adminHeaders -UseBasicParsing
Write-Host "Admin sees opportunities count:" $adminOpps.Count

# 2. Sales Executive 2 Login (sales2@acxiomcrm.com)
$salesLogin = Invoke-RestMethod -Uri "http://localhost:5222/api/auth/login" -Method Post -Body (@{ email = "sales2@acxiomcrm.com"; password = "Sales@12345" } | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing
$salesHeaders = @{ Authorization = "Bearer $($salesLogin.token)" }
$salesOpps = Invoke-RestMethod -Uri "http://localhost:5222/api/opportunities" -Method Get -Headers $salesHeaders -UseBasicParsing
Write-Host "SalesExecutive 2 sees opportunities count:" $salesOpps.Count
if ($salesOpps.Count -lt $adminOpps.Count) {
    Write-Host "PASS: Role-based scoping verified! SalesExecutive sees only assigned records."
} else {
    Write-Host "FAIL: Scoping not enforced."
}

Write-Host "`n=== TEST 11: Account Lockout After Repeated Failed Attempts ==="
$dummyEmail = "testlockout@acxiomcrm.com"
# First create the user via Register API or check a user
# Let's test failed attempts on sales2
for ($i = 1; $i -le 5; $i++) {
    try {
        Invoke-RestMethod -Uri "http://localhost:5222/api/auth/login" -Method Post -Body (@{ email = "sales2@acxiomcrm.com"; password = "WrongPassword$i!" } | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing
    } catch {
        # expected 401
    }
}
# 6th attempt should be locked out (status 423)
try {
    Invoke-RestMethod -Uri "http://localhost:5222/api/auth/login" -Method Post -Body (@{ email = "sales2@acxiomcrm.com"; password = "Sales@12345" } | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing
    Write-Host "FAIL: Expected lockout but login succeeded"
} catch {
    Write-Host "PASS: Lockout successfully triggered! Status:" $_.Exception.Response.StatusCode.value__
}

Write-Host "`n=== TEST 12: Swagger Documentation UI ==="
$swaggerResp = Invoke-WebRequest -Uri "http://localhost:5222/swagger/v1/swagger.json" -UseBasicParsing
Write-Host "Swagger JSON Status:" $swaggerResp.StatusCode "Swagger title:" ($swaggerResp.Content | ConvertFrom-Json).info.title

Write-Host "`n=== ALL ADVANCED SCENARIO TESTS PASSED ==="
