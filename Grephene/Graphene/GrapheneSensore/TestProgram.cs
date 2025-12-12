using GrapheneSensore.Data;
using GrapheneSensore.Services;
using System;
using System.Threading.Tasks;

namespace GrapheneSensore.Test
{
    internal class Program
    {
        internal static async Task Main(string[] args)
        {
            Console.WriteLine("Testing UserService.GetAllUsersAsync...");
            Console.WriteLine("==========================================");
            
            try
            {
                var userService = new UserService();
                Console.WriteLine("UserService created successfully");
                
                Console.WriteLine("\nAttempting to load users...");
                var users = await userService.GetAllUsersAsync();
                
                Console.WriteLine($"\nSUCCESS! Loaded {users.Count} users:");
                foreach (var user in users)
                {
                    Console.WriteLine($"  - {user.Username} ({user.FirstName} {user.LastName}) - {user.UserType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\nInner Exception: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"Message: {ex.InnerException.Message}");
                    Console.WriteLine($"\nInner Stack Trace:\n{ex.InnerException.StackTrace}");
                }
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
