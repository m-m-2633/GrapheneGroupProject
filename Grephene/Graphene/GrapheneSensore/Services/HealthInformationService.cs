using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    public class HealthInformationService
    {
        public async Task<List<HealthInformation>> GetUserHealthInformationAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var context = new SensoreDbContext();
                var query = context.HealthInformation.Where(hi => hi.UserId == userId);

                if (startDate.HasValue)
                {
                    query = query.Where(hi => hi.RecordDate >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(hi => hi.RecordDate <= endDate.Value.Date);
                }

                return await query.OrderByDescending(hi => hi.RecordDate).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving health information for user: {userId}", ex, "HealthInformationService");
                throw;
            }
        }
        public async Task<(bool success, string message, HealthInformation? healthInfo)> AddHealthInformationAsync(HealthInformation healthInfo)
        {
            try
            {
                using var context = new SensoreDbContext();
                context.HealthInformation.Add(healthInfo);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Health information added for user: {healthInfo.UserId}", "HealthInformationService");
                return (true, "Health information added successfully", healthInfo);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error adding health information", ex, "HealthInformationService");
                return (false, "An error occurred while adding health information", null);
            }
        }
        public async Task<(bool success, string message)> UpdateHealthInformationAsync(HealthInformation healthInfo)
        {
            try
            {
                using var context = new SensoreDbContext();
                context.HealthInformation.Update(healthInfo);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Health information updated: {healthInfo.HealthId}", "HealthInformationService");
                return (true, "Health information updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating health information: {healthInfo.HealthId}", ex, "HealthInformationService");
                return (false, "An error occurred while updating health information");
            }
        }
        public async Task<(bool success, string message)> DeleteHealthInformationAsync(Guid healthId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var healthInfo = await context.HealthInformation.FindAsync(healthId);
                
                if (healthInfo == null)
                {
                    return (false, "Health information record not found");
                }

                context.HealthInformation.Remove(healthInfo);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Health information deleted: {healthId}", "HealthInformationService");
                return (true, "Health information deleted successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting health information: {healthId}", ex, "HealthInformationService");
                return (false, "An error occurred while deleting health information");
            }
        }
        public async Task<HealthInformation?> GetLatestHealthInformationAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.HealthInformation
                    .Where(hi => hi.UserId == userId)
                    .OrderByDescending(hi => hi.RecordDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving latest health information for user: {userId}", ex, "HealthInformationService");
                return null;
            }
        }
    }
}
