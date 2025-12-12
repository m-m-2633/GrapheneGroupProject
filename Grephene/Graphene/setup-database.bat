@echo off
echo ========================================
echo   Graphene Sensore Database Setup
echo ========================================
echo.

echo Creating database on MUZAMIL-WORLD\SQLEXPRESS...
echo.

sqlcmd -S MUZAMIL-WORLD\SQLEXPRESS -i "Database\CreateDatabase.sql" -o "database-setup.log"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo   SUCCESS! Database created.
    echo ========================================
    echo.
    echo Database: Grephene
    echo Server: MUZAMIL-WORLD\SQLEXPRESS
    echo.
    echo You can now run the application.
    echo.
) else (
    echo.
    echo ========================================
    echo   ERROR! Database setup failed.
    echo ========================================
    echo.
    echo Please check:
    echo 1. SQL Server is running
    echo 2. You have permissions
    echo 3. Check database-setup.log for details
    echo.
)

pause
