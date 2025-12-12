using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Helpers;
using GrapheneSensore.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    public static class DatabaseInitializationService
    {
        private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private const string DefaultAdminUsername = "admin";
        private const string DefaultAdminPassword = "Admin@123";
        public static async Task InitializeDatabaseAsync()
        {
            try
            {
                Logger.Instance.LogInfo("Starting database initialization", "DatabaseInit");
                
                using var context = new SensoreDbContext();
                Logger.Instance.LogInfo("Testing database connection...", "DatabaseInit");
                if (!await context.Database.CanConnectAsync())
                {
                    throw new InvalidOperationException(
                        "Cannot establish database connection. Please verify:\n" +
                        "1. SQL Server is running (MUZAMIL-WORLD\\SQLEXPRESS)\n" +
                        "2. Database 'Grephene' exists or can be created\n" +
                        "3. Connection string in appsettings.json is correct\n" +
                        "4. Network connectivity to database server");
                }
                
                Logger.Instance.LogInfo("Database connection successful", "DatabaseInit");
                try
                {
                    var created = await context.Database.EnsureCreatedAsync();
                    if (created)
                    {
                        Logger.Instance.LogInfo("Database created successfully", "DatabaseInit");
                    }
                    else
                    {
                        Logger.Instance.LogInfo("Database already exists", "DatabaseInit");
                    }
                }
                catch (Exception ensureEx)
                {
                    Logger.Instance.LogWarning(
                        $"EnsureCreated warning (database may already exist): {ensureEx.Message}", 
                        "DatabaseInit");
                }
                await EnsureAdminUserExistsAsync();
                
                Logger.Instance.LogInfo("Database initialization completed successfully", "DatabaseInit");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogCritical("Failed to initialize database", ex, "DatabaseInit");
                throw new InvalidOperationException(
                    "Database initialization failed. Please verify:\n" +
                    "1. SQL Server is running (MUZAMIL-WORLD\\SQLEXPRESS)\n" +
                    "2. Database 'Grephene' exists or can be created\n" +
                    "3. Connection string in appsettings.json is correct\n" +
                    "4. Network connectivity to database server", 
                    ex);
            }
        }
        private static async Task EnsureAdminUserExistsAsync()
        {
            try
            {
                using var context = new SensoreDbContext();
                var adminUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == DefaultAdminUsername);

                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        UserId = AdminUserId,
                        Username = DefaultAdminUsername,
                        PasswordHash = PasswordHelper.HashPassword(DefaultAdminPassword),
                        UserType = "Admin",
                        FirstName = "System",
                        LastName = "Administrator",
                        Email = "admin@graphenetrace.com",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();
                    
                    Logger.Instance.LogInfo(
                        $"Admin user created successfully: {DefaultAdminUsername}", 
                        "DatabaseInit");
                }
                else
                {
                    bool needsUpdate = false;

                    if (!adminUser.IsActive)
                    {
                        adminUser.IsActive = true;
                        needsUpdate = true;
                        Logger.Instance.LogWarning(
                            "Admin user was inactive - reactivating", 
                            "DatabaseInit");
                    }

                    if (adminUser.UserType != "Admin")
                    {
                        adminUser.UserType = "Admin";
                        needsUpdate = true;
                        Logger.Instance.LogWarning(
                            $"Admin user type was '{adminUser.UserType}' - correcting to 'Admin'", 
                            "DatabaseInit");
                    }
                    if (string.IsNullOrEmpty(adminUser.PasswordHash) || 
                        adminUser.PasswordHash.Length < 50)
                    {
                        adminUser.PasswordHash = PasswordHelper.HashPassword(DefaultAdminPassword);
                        needsUpdate = true;
                        Logger.Instance.LogWarning(
                            "Admin password hash was invalid - resetting to default", 
                            "DatabaseInit");
                    }

                    if (needsUpdate)
                    {
                        await context.SaveChangesAsync();
                        Logger.Instance.LogInfo(
                            "Admin user configuration updated successfully", 
                            "DatabaseInit");
                    }
                    else
                    {
                        Logger.Instance.LogInfo(
                            "Admin user verified - no updates needed", 
                            "DatabaseInit");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogCritical(
                    "Failed to ensure admin user exists", 
                    ex, 
                    "DatabaseInit");
                throw;
            }
        }
        public static async Task<bool> ResetAdminPasswordAsync()
        {
            try
            {
                Logger.Instance.LogWarning(
                    "Admin password reset requested", 
                    "DatabaseInit");
                
                using var context = new SensoreDbContext();
                
                var adminUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == DefaultAdminUsername);

                if (adminUser != null)
                {
                    adminUser.PasswordHash = PasswordHelper.HashPassword(DefaultAdminPassword);
                    adminUser.IsActive = true;
                    await context.SaveChangesAsync();
                    
                    Logger.Instance.LogWarning(
                        $"Admin password reset to default for user: {DefaultAdminUsername}", 
                        "DatabaseInit");
                    return true;
                }

                Logger.Instance.LogError(
                    $"Admin user '{DefaultAdminUsername}' not found for password reset", 
                    null,
                    "DatabaseInit");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogCritical(
                    "Failed to reset admin password", 
                    ex, 
                    "DatabaseInit");
                return false;
            }
        }
    }
}
