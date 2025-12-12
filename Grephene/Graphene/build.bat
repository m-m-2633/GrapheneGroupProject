@echo off
echo ========================================
echo Graphene Sensore - Build Script
echo ========================================
echo.

cd GrapheneSensore

echo Step 1: Cleaning previous builds...
dotnet clean
echo.

echo Step 2: Restoring NuGet packages...
dotnet restore
echo.

echo Step 3: Building application...
dotnet build --configuration Release
echo.

if %ERRORLEVEL% EQU 0 (
    echo ========================================
    echo BUILD SUCCESSFUL!
    echo ========================================
    echo.
    echo Application built in: GrapheneSensore\bin\Release\net6.0-windows\
    echo.
    echo Next steps:
    echo 1. Run database script: Database\CreateDatabase.sql
    echo 2. Run application: dotnet run
    echo.
) else (
    echo ========================================
    echo BUILD FAILED!
    echo ========================================
    echo Please check the errors above.
)

pause
