$baseUrl = "http://localhost:5035/api"
$results = @{}

function Log($msg) { Write-Host "[TEST] $msg" -ForegroundColor Cyan }

function Test-Endpoint($Name, $Action) {
    Log "Testing $Name..."
    try {
        $res = & $Action
        $results[$Name] = $res
    } catch {
        Log "FAILED: $Name - $($_.Exception.Message)"
        $results[$Name] = @{ Error = $_.Exception.Message }
    }
}

# 1. Registration
$uniqueEmail = "test_$(Get-Random)@example.com"
Test-Endpoint "Register" {
    $regBody = @{ fullName = "Test User"; email = $uniqueEmail; password = "Password123"; role = "Student" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Auth/register" -Method POST -ContentType "application/json" -Body $regBody
}

# 2. Login
Test-Endpoint "Login" {
    $loginBody = @{ email = $uniqueEmail; password = "Password123" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method POST -ContentType "application/json" -Body $loginBody
}

$token = $results.Login.token
$headers = @{ Authorization = "Bearer $token" }

# 3. Forgot Password
Test-Endpoint "ForgotPassword" {
    Invoke-RestMethod -Uri "$baseUrl/Auth/forgot-password" -Method POST -ContentType "application/json" -Body "`"$uniqueEmail`""
}

# 4. Reset Password
Test-Endpoint "ResetPassword" {
    $resetBody = @{ email = $uniqueEmail; newPassword = "NewPassword123" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Auth/reset-password" -Method POST -ContentType "application/json" -Body $resetBody
}

# 5. Login New
Test-Endpoint "LoginNew" {
    $loginBody = @{ email = $uniqueEmail; password = "NewPassword123" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method POST -ContentType "application/json" -Body $loginBody
}

$token = $results.LoginNew.token
$headers = @{ Authorization = "Bearer $token" }

# 6. Me
Test-Endpoint "Me" {
    Invoke-RestMethod -Uri "$baseUrl/Users/Me" -Method GET -Headers $headers
}

# 7. Catalog
Test-Endpoint "Categories" { Invoke-RestMethod -Uri "$baseUrl/Categories" -Method GET }
Test-Endpoint "Services" { Invoke-RestMethod -Uri "$baseUrl/Services" -Method GET }

$serviceId = $results.Services[0].id

# 8. Orders
Test-Endpoint "CreateOrder" {
    $orderBody = @{ serviceId = $serviceId; price = 100 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Orders" -Method POST -Headers $headers -ContentType "application/json" -Body $orderBody
}
$orderId = $results.CreateOrder.id

Test-Endpoint "AvailableOrders" { Invoke-RestMethod -Uri "$baseUrl/Orders/Available" -Method GET -Headers $headers }
Test-Endpoint "MyOrders" { Invoke-RestMethod -Uri "$baseUrl/Orders" -Method GET -Headers $headers }

# 9. Payments
Test-Endpoint "ProcessPayment" {
    Invoke-RestMethod -Uri "$baseUrl/Payments/$orderId" -Method POST -Headers $headers -ContentType "application/json" -Body "100"
}
Test-Endpoint "Earnings" { Invoke-RestMethod -Uri "$baseUrl/Payments/Earnings" -Method GET -Headers $headers }

# 10. Chat
Test-Endpoint "OrderChat" { Invoke-RestMethod -Uri "$baseUrl/Chat/Order/$orderId" -Method GET -Headers $headers }
$chatId = $results.OrderChat.id

Test-Endpoint "SendMessage" {
    # Using curl for multipart
    curl.exe -s -X POST "$baseUrl/Chat/$chatId/Message" -H "Authorization: Bearer $token" -F "content=Hello from curl" | ConvertFrom-Json
}

# 11. Tickets
Test-Endpoint "CreateTicket" {
    Invoke-RestMethod -Uri "$baseUrl/Ticket" -Method POST -Headers $headers -ContentType "application/json" -Body "`"Test Ticket`""
}
$ticketId = $results.CreateTicket.id

Test-Endpoint "MyTickets" { Invoke-RestMethod -Uri "$baseUrl/Ticket" -Method GET -Headers $headers }
Test-Endpoint "TicketDetails" { Invoke-RestMethod -Uri "$baseUrl/Ticket/$ticketId" -Method GET -Headers $headers }
Test-Endpoint "TicketReply" {
    curl.exe -s -X POST "$baseUrl/Ticket/$ticketId/Reply" -H "Authorization: Bearer $token" -F "content=Reply from curl" | ConvertFrom-Json
}

# 12. KYC
Test-Endpoint "KycStatus" { Invoke-RestMethod -Uri "$baseUrl/Kyc/Status" -Method GET -Headers $headers }

# 13. Notifications
Test-Endpoint "Notifications" { Invoke-RestMethod -Uri "$baseUrl/Notifications" -Method GET -Headers $headers }

$results | ConvertTo-Json -Depth 5 > test_results.json
Log "Test completed. Results saved to test_results.json"
