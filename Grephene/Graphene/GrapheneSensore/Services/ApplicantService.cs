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
    public class ApplicantService
    {
        public async Task<List<Applicant>> GetApplicantsBySessionUserAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.Applicants
                    .Where(a => a.SessionUserId == userId)
                    .OrderBy(a => a.LastName)
                    .ThenBy(a => a.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving applicants for user: {userId}", ex, "ApplicantService");
                throw;
            }
        }
        public async Task<(bool success, string message, Applicant? applicant)> AddApplicantAsync(Applicant applicant)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(applicant.FirstName) || string.IsNullOrWhiteSpace(applicant.LastName))
                {
                    return (false, "First name and last name are required", null);
                }

                using var context = new SensoreDbContext();
                context.Applicants.Add(applicant);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Applicant added: {applicant.FullName}", "ApplicantService");
                return (true, "Applicant added successfully", applicant);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error adding applicant: {applicant.FullName}", ex, "ApplicantService");
                return (false, "An error occurred while adding the applicant", null);
            }
        }
        public async Task<(bool success, string message)> UpdateApplicantAsync(Applicant applicant)
        {
            try
            {
                using var context = new SensoreDbContext();
                context.Applicants.Update(applicant);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Applicant updated: {applicant.FullName}", "ApplicantService");
                return (true, "Applicant updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating applicant: {applicant.ApplicantId}", ex, "ApplicantService");
                return (false, "An error occurred while updating the applicant");
            }
        }
        public async Task<(bool success, string message)> DeleteApplicantAsync(Guid applicantId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var applicant = await context.Applicants.FindAsync(applicantId);
                
                if (applicant == null)
                {
                    return (false, "Applicant not found");
                }
                var feedbackSessions = await context.FeedbackSessions
                    .Where(fs => fs.ApplicantId == applicantId)
                    .ToListAsync();

                foreach (var session in feedbackSessions)
                {
                    var completedFeedbacks = await context.CompletedFeedbacks
                        .Where(cf => cf.SessionId == session.SessionId)
                        .ToListAsync();
                    context.CompletedFeedbacks.RemoveRange(completedFeedbacks);
                    var responses = await context.FeedbackResponses
                        .Where(fr => fr.SessionId == session.SessionId)
                        .ToListAsync();
                    context.FeedbackResponses.RemoveRange(responses);
                }
                context.FeedbackSessions.RemoveRange(feedbackSessions);
                context.Applicants.Remove(applicant);
                
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Applicant and related data deleted: {applicantId}", "ApplicantService");
                return (true, "Applicant and all related feedback data deleted successfully");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                Logger.Instance.LogError($"Database error deleting applicant: {applicantId}", dbEx, "ApplicantService");
                var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
                if (innerMsg.Contains("REFERENCE constraint"))
                {
                    return (false, "Cannot delete applicant: There are related records that prevent deletion. Please contact support.");
                }
                return (false, $"Database error: {innerMsg}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting applicant: {applicantId}", ex, "ApplicantService");
                return (false, $"An error occurred while deleting: {ex.Message}");
            }
        }
        public async Task<(bool success, string message)> DeleteAllApplicantsForSessionAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var applicants = await context.Applicants
                    .Where(a => a.SessionUserId == userId)
                    .ToListAsync();

                if (applicants.Count == 0)
                {
                    return (true, "No applicants to delete");
                }

                int totalDeleted = 0;
                foreach (var applicant in applicants)
                {
                    var feedbackSessions = await context.FeedbackSessions
                        .Where(fs => fs.ApplicantId == applicant.ApplicantId)
                        .ToListAsync();

                    foreach (var session in feedbackSessions)
                    {
                        var completedFeedbacks = await context.CompletedFeedbacks
                            .Where(cf => cf.SessionId == session.SessionId)
                            .ToListAsync();
                        context.CompletedFeedbacks.RemoveRange(completedFeedbacks);
                        var responses = await context.FeedbackResponses
                            .Where(fr => fr.SessionId == session.SessionId)
                            .ToListAsync();
                        context.FeedbackResponses.RemoveRange(responses);
                    }
                    context.FeedbackSessions.RemoveRange(feedbackSessions);
                    totalDeleted++;
                }
                context.Applicants.RemoveRange(applicants);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Deleted {totalDeleted} applicants and related data for user: {userId}", "ApplicantService");
                return (true, $"Deleted {totalDeleted} applicant(s) and all related feedback data successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting applicants for user: {userId}", ex, "ApplicantService");
                return (false, $"An error occurred while deleting applicants: {ex.Message}");
            }
        }
        public async Task<Applicant?> GetApplicantByIdAsync(Guid applicantId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.Applicants.FindAsync(applicantId);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving applicant: {applicantId}", ex, "ApplicantService");
                return null;
            }
        }
    }
}
