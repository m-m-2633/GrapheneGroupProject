using GrapheneSensore.Helpers;
using System;

namespace GrapheneSensore.Utilities
{
    public static class PasswordHashGenerator
    {
        public static string GenerateAndPrintHash(string password)
        {
            try
            {
                Console.WriteLine("===========================================");
                Console.WriteLine("    Password Hash Generator");
                Console.WriteLine("===========================================");
                Console.WriteLine();
                
                string hash = PasswordHelper.HashPassword(password);
                
                Console.WriteLine($"Password: {password}");
                Console.WriteLine($"BCrypt Hash: {hash}");
                Console.WriteLine();
                Console.WriteLine("Use this hash in your SQL script:");
                Console.WriteLine("─────────────────────────────────────────");
                Console.WriteLine($"PasswordHash = '{hash}'");
                Console.WriteLine("─────────────────────────────────────────");
                Console.WriteLine();
                bool isValid = PasswordHelper.VerifyPassword(password, hash);
                Console.WriteLine($"Hash Verification: {(isValid ? "✓ VALID" : "✗ INVALID")}");
                Console.WriteLine();
                
                return hash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating hash: {ex.Message}");
                throw;
            }
        }
        public static string Generate(string password)
        {
            return PasswordHelper.HashPassword(password);
        }
    }
}
