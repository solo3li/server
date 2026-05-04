$ErrorActionPreference = "Stop"

$token = (Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@uis.com","password":"admin123"}').Token
$headers = @{ Authorization = "Bearer $token" }

$results = @{}

try { $results.Me = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Users/Me" -Method GET -Headers $headers } catch { $results.Me = $_.Exception.Message }
try { 
    $categories = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Categories" -Method GET 
    $results.Categories = $categories | Select-Object -First 1
} catch { $results.Categories = $_.Exception.Message }

try {
    $services = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Services" -Method GET
    $results.Services = $services | Select-Object -First 1
    $serviceId = $services[0].id
    $results.ServiceById = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Services/$serviceId" -Method GET
} catch { $results.Services = $_.Exception.Message }

try { $results.AvailableOrders = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Orders/Available" -Method GET | Select-Object -First 1 } catch { $results.AvailableOrders = $_.Exception.Message }

try {
    $newOrder = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Orders" -Method POST -Headers $headers -ContentType "application/json" -Body "{`"serviceId`":`"$serviceId`", `"price`": 150}"
    $results.CreateOrder = $newOrder
    $orderId = $newOrder.id
    
    $results.MyOrders = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Orders" -Method GET -Headers $headers | Select-Object -First 1
    $results.OrderById = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Orders/$orderId" -Method GET -Headers $headers
    
    $results.Payment = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Payments/$orderId" -Method POST -Headers $headers -ContentType "application/json" -Body "150"
} catch { $results.Orders = $_.Exception.Message }

try {
    $newTicket = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Ticket" -Method POST -Headers $headers -ContentType "application/json" -Body "`"My support issue`""
    $results.CreateTicket = $newTicket
    $ticketId = $newTicket.id
    
    $results.MyTickets = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Ticket" -Method GET -Headers $headers | Select-Object -First 1
    $results.TicketById = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Ticket/$ticketId" -Method GET -Headers $headers
} catch { $results.Tickets = $_.Exception.Message }

try {
    $results.KycStatus = Invoke-RestMethod -Uri "https://server-production-8fb9.up.railway.app/api/Kyc/Status" -Method GET -Headers $headers
} catch { $results.KycStatus = $_.Exception.Message }

$results | ConvertTo-Json -Depth 5