# UIS Auto-Deployment Script
# This script builds and runs the all-in-one Docker image locally.

Write-Host "🚀 Starting Auto-Deployment for UIS..." -ForegroundColor Cyan

# 1. Stop and remove existing container if it exists
if (docker ps -a --format '{{.Names}}' | Select-String -Pattern "^uis-app$") {
    Write-Host "🛑 Stopping and removing existing 'uis-app' container..." -ForegroundColor Yellow
    docker rm -f uis-app
}

# 2. Build the all-in-one image
Write-Host "🏗️ Building the Docker image (ASP.NET + SQLite)..." -ForegroundColor Cyan
docker build -f Dockerfile -t uis-all-in-one .

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# 3. Run the container
Write-Host "🏃 Starting the container on http://localhost:5035..." -ForegroundColor Cyan
docker run -d `
    -p 5035:80 `
    --name uis-app `
    --restart unless-stopped `
    uis-all-in-one

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start the container!" -ForegroundColor Red
    exit 1
}

# 4. Wait for the app to be ready
Write-Host "⏳ Waiting for the application to initialize (Database & Seeding)..." -ForegroundColor Yellow
$retries = 0
$maxRetries = 30
while ($retries -lt $maxRetries) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5035/swagger" -Method Get -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ UIS is UP and RUNNING!" -ForegroundColor Green
            Write-Host "👉 Swagger: http://localhost:5035/swagger" -ForegroundColor Cyan
            Write-Host "👉 Admin Panel: http://localhost:5035/Admin" -ForegroundColor Cyan
            break
        }
    } catch {}
    $retries++
    Write-Host "." -NoNewline
    Start-Sleep -Seconds 2
}

if ($retries -eq $maxRetries) {
    Write-Host "`n❌ Application took too long to start. Check logs with: docker logs uis-app" -ForegroundColor Red
}

Write-Host "`n🎉 Deployment complete!" -ForegroundColor Green
