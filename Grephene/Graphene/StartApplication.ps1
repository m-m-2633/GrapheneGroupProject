Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Graphene Sensore Startup Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

function Test-SqlServerRunning {
    Write-Host "Checking SQL Server status..." -ForegroundColor Yellow
    
    $sqlService = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
    
    if ($null -eq $sqlService) {
        Write-Host "SQL Server Express service not found!" -ForegroundColor Red
        Write-Host "Please install SQL Server Express from: https://aka.ms/ssmsfullsetup" -ForegroundColor Yellow
        return $false
    }
    
    if ($sqlService.Status -ne "Running") {
        Write-Host "SQL Server is not running. Attempting to start..." -ForegroundColor Yellow
        try {
            Start-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction Stop
            Write-Host "SQL Server started successfully!" -ForegroundColor Green
            Start-Sleep -Seconds 3
        }
        catch {
            Write-Host "Failed to start SQL Server: $_" -ForegroundColor Red
            return $false
        }
    }
    else {
        Write-Host "SQL Server is running!" -ForegroundColor Green
    }
    
    return $true
}

function Test-DotNetSdk {
    Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
    
    try {
        $dotnetVersion = dotnet --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Green
            return $true
        }
    }
    catch {
        Write-Host ".NET SDK not found!" -ForegroundColor Red
        Write-Host "Please install .NET 6.0 SDK from: https://dotnet.microsoft.com/download/dotnet/6.0" -ForegroundColor Yellow
        return $false
    }
    
    return $false
}

function Initialize-Database {
    Write-Host ""
    Write-Host "Setting up database..." -ForegroundColor Yellow
    
    $sqlFile = Join-Path $PSScriptRoot "Database\CreateDatabase.sql"
    
    if (-not (Test-Path $sqlFile)) {
        Write-Host "Database script not found at: $sqlFile" -ForegroundColor Red
        return $false
    }
    
    try {
        Write-Host "Executing database creation script..." -ForegroundColor Yellow
        
        $null = sqlcmd -S "MUZAMIL-WORLD\SQLEXPRESS" -i $sqlFile -E 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Database setup completed successfully!" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "Database setup encountered issues. The application will attempt to create the database on first run." -ForegroundColor Yellow
            return $true
        }
    }
    catch {
        Write-Host "Warning: Could not execute SQL script: $_" -ForegroundColor Yellow
        Write-Host "The application will attempt to create the database on first run." -ForegroundColor Yellow
        return $true
    }
}

function Restore-Packages {
    Write-Host ""
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    
    $projectPath = Join-Path $PSScriptRoot "GrapheneSensore"
    Push-Location $projectPath
    
    try {
        dotnet restore 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Packages restored successfully!" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "Failed to restore packages" -ForegroundColor Red
            return $false
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-ApplicationBuild {
    Write-Host ""
    Write-Host "Building application..." -ForegroundColor Yellow
    
    $projectPath = Join-Path $PSScriptRoot "GrapheneSensore"
    Push-Location $projectPath
    
    try {
        $output = dotnet build --configuration Release 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Build completed successfully!" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "Build failed:" -ForegroundColor Red
            Write-Host $output -ForegroundColor Red
            return $false
        }
    }
    finally {
        Pop-Location
    }
}

function Start-Application {
    Write-Host ""
    Write-Host "Starting Graphene Sensore application..." -ForegroundColor Green
    Write-Host ""
    Write-Host "Default Login Credentials:" -ForegroundColor Cyan
    Write-Host "  Username: admin" -ForegroundColor White
    Write-Host "  Password: Admin@123" -ForegroundColor White
    Write-Host ""
    Write-Host "Please change the default password after first login!" -ForegroundColor Yellow
    Write-Host ""
    
    $projectPath = Join-Path $PSScriptRoot "GrapheneSensore"
    Push-Location $projectPath
    
    try {
        dotnet run --configuration Release
    }
    finally {
        Pop-Location
    }
}

Write-Host "Step 1: Checking Prerequisites" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Cyan

if (-not (Test-DotNetSdk)) {
    Write-Host ""
    Write-Host "Prerequisites check failed. Please install required software and try again." -ForegroundColor Red
    exit 1
}

if (-not (Test-SqlServerRunning)) {
    Write-Host ""
    Write-Host "SQL Server is not available. Please install and start SQL Server Express." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Database Setup" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Cyan

Initialize-Database | Out-Null

Write-Host ""
Write-Host "Step 3: Restore Dependencies" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Cyan

if (-not (Restore-Packages)) {
    Write-Host ""
    Write-Host "Failed to restore packages. Cannot continue." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 4: Build Application" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Cyan

if (-not (Invoke-ApplicationBuild)) {
    Write-Host ""
    Write-Host "Build failed. Please check the error messages above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 5: Launch Application" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Cyan

Start-Application

Write-Host ""
Write-Host "Application closed." -ForegroundColor Cyan
