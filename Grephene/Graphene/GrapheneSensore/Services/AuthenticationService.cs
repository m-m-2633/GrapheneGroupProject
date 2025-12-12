using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Helpers;
using GrapheneSensore.Logging;
using GrapheneSensore.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    public class AuthenticationService
    {
        private UserSession? _currentSession;
        private readonly object _sessionLock = new object();
        public User? CurrentUser { get; private set; }
        public UserSession? CurrentSession
        {
            get
            {
                lock (_sessionLock)
                {
                    if (_currentSession != null && !_currentSession.IsValid())
                    {
                        Logout();
                        return null;
                    }
                    return _currentSession;
                }
            }
        }
        public bool IsAuthenticated => CurrentUser != null && CurrentSession != null && CurrentSession.IsValid();
        public async Task<(bool success, string message, User? user)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Logger.Instance.LogWarning("Login attempt with empty credentials", "Auth");
                return (false, "Username and password are required", null);
            }

            try
            {
                Logger.Instance.LogInfo($"Login attempt for user: {username}", "Auth");
                
                using var context = new SensoreDbContext();
                var user = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    Logger.Instance.LogWarning($"Login failed - user not found: {username}", "Auth");
                    return (false, "Invalid username or password", null);
                }

                if (!user.IsActive)
                {
                    Logger.Instance.LogWarning($"Login failed - user inactive: {username}", "Auth");
                    return (false, "Account is inactive. Please contact administrator.", null);
                }
                bool passwordValid = PasswordHelper.VerifyPassword(password, user.PasswordHash);
                
                if (!passwordValid)
                {
                    Logger.Instance.LogWarning($"Login failed - invalid password for user: {username}", "Auth");
                    return (false, "Invalid username or password", null);
                }
                using var updateContext = new SensoreDbContext();
                var userToUpdate = await updateContext.Users.FindAsync(user.UserId);
                if (userToUpdate != null)
                {
                    userToUpdate.LastLoginDate = DateTime.UtcNow;
                    await updateContext.SaveChangesAsync();
                }
                CreateSession(user);

                CurrentUser = user;
                Logger.Instance.LogInfo($"Login successful for user: {username} (Type: {user.UserType})", "Auth");
                return (true, "Login successful", user);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Login error for user: {username}", ex, "Auth");
                return (false, "An error occurred during login. Please try again.", null);
            }
        }
        public void Logout()
        {
            lock (_sessionLock)
            {
                if (CurrentUser != null)
                {
                    Logger.Instance.LogInfo($"User logged out: {CurrentUser.Username}", "Auth");
                }
                CurrentUser = null;
                _currentSession = null;
            }
        }
        private void CreateSession(User user)
        {
            lock (_sessionLock)
            {
                var config = AppConfiguration.Instance;
                _currentSession = new UserSession
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    UserType = user.UserType,
                    LoginTime = DateTime.UtcNow,
                    LastActivityTime = DateTime.UtcNow,
                    ExpirationTime = DateTime.UtcNow.AddMinutes(config.SessionTimeoutMinutes),
                    MachineName = Environment.MachineName
                };
            }
        }
        public void UpdateSessionActivity()
        {
            lock (_sessionLock)
            {
                if (_currentSession != null && _currentSession.IsValid())
                {
                    var config = AppConfiguration.Instance;
                    _currentSession.UpdateActivity(config.SessionTimeoutMinutes);
                }
            }
        }
        public async Task<(bool success, string message)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "Passwords cannot be empty");
            }

            if (oldPassword == newPassword)
            {
                return (false, "New password must be different from the current password");
            }

            try
            {
                var (isValid, validationMessage) = PasswordHelper.ValidatePassword(newPassword);
                if (!isValid)
                {
                    return (false, validationMessage);
                }

                using var context = new SensoreDbContext();
                var user = await context.Users.FindAsync(userId);

                if (user == null)
                {
                    Logger.Instance.LogWarning($"Password change failed - user not found: {userId}", "Auth");
                    return (false, "User not found");
                }

                if (!PasswordHelper.VerifyPassword(oldPassword, user.PasswordHash))
                {
                    Logger.Instance.LogWarning($"Password change failed - incorrect old password for user: {user.Username}", "Auth");
                    return (false, "Current password is incorrect");
                }

                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Password changed successfully for user: {user.Username}", "Auth");
                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error changing password for user: {userId}", ex, "Auth");
                return (false, "An error occurred while changing the password. Please try again.");
            }
        }
        public bool HasPermission(string requiredUserType)
        {
            if (!IsAuthenticated || CurrentUser == null)
                return false;

            UpdateSessionActivity();
            return CurrentUser.UserType == requiredUserType || CurrentUser.UserType == "Admin";
        }
        public async Task<bool> CanAccessUserDataAsync(Guid userId)
        {
            if (!IsAuthenticated || CurrentUser == null)
                return false;

            UpdateSessionActivity();
            if (CurrentUser.UserType == "Admin")
                return true;
            if (CurrentUser.UserId == userId)
                return true;
            if (CurrentUser.UserType == "Clinician")
            {
                try
                {
                    using var context = new SensoreDbContext();
                    var patient = await context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId);
                    return patient?.AssignedClinicianId == CurrentUser.UserId;
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError($"Error checking data access for user: {userId}", ex, "Auth");
                    return false;
                }
            }

            return false;
        }
        [Obsolete("Use CanAccessUserDataAsync instead for better performance")]
        public bool CanAccessUserData(Guid userId)
        {
            return CanAccessUserDataAsync(userId).GetAwaiter().GetResult();
        }
    }
}
