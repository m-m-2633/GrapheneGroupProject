@echo off

echo ============================================
echo Graphene Feedback Management System Setup
echo Version 3.0.0
echo ============================================
echo.

echo Checking SQL Server connection...
sqlcmd -S .\SQLEXPRESS -Q "SELECT @@VERSION" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Cannot connect to SQL Server.
    echo Please ensure SQL Server Express is installed and running.
    echo Default instance name: .\SQLEXPRESS
    echo.
    pause
    exit /b 1
)
echo SQL Server connection: OK
echo.

echo Step 1: Creating main database...
sqlcmd -S .\SQLEXPRESS -i "Database\CreateDatabase.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to create main database.
    pause
    exit /b 1
)
echo Main database created successfully.
echo.

echo Step 2: Creating feedback system schema...
sqlcmd -S .\SQLEXPRESS -i "Database\CreateFeedbackSystem.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to create feedback system schema.
    pause
    exit /b 1
)
echo Feedback system schema created successfully.
echo.

echo Step 3: Restoring NuGet packages...
cd GrapheneSensore
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to restore NuGet packages.
    cd ..
    pause
    exit /b 1
)
echo NuGet packages restored successfully.
cd ..
echo.

echo Step 4: Building application...
cd GrapheneSensore
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to build application.
    cd ..
    pause
    exit /b 1
)
cd ..
echo Application built successfully.
echo.

echo Step 5: Creating output directories...
if not exist "%USERPROFILE%\Documents\GrapheneFeedbacks" (
    mkdir "%USERPROFILE%\Documents\GrapheneFeedbacks"
    echo Created: %USERPROFILE%\Documents\GrapheneFeedbacks
)
echo.

echo ============================================
echo Setup completed successfully!
echo ============================================
echo.
echo Database: Grephene (with feedback system)
echo Server: .\SQLEXPRESS
echo PDF Output: %USERPROFILE%\Documents\GrapheneFeedbacks
echo.
echo Default Login Credentials:
echo Username: admin
echo Password: Admin@123
echo.
echo IMPORTANT: Change the admin password after first login!
echo.
echo Next Steps:
echo 1. Configure email settings in appsettings.json (optional)
echo 2. Run the application using: run.bat
echo 3. Review FEEDBACK_SYSTEM_GUIDE.md for detailed documentation
echo.
pause
