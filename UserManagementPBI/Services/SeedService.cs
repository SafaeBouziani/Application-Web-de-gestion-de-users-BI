using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Globalization;
using System.Security.Cryptography;
using UserManagementPBI.Data;  
using UserManagementPBI.Models;

namespace UserManagementPBI.Services

{
    public class SeedService
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Admins>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                try
                {
                    logger.LogInformation("Creating database...");
                    await dbContext.Database.EnsureCreatedAsync();

                    logger.LogInformation("Seeding Roles...");
                    await AddRoleAsync(roleManager, "Admin");
                    await AddRoleAsync(roleManager, "User");

                    logger.LogInformation("Seeding Admin Users from CSV...");
                    string csvFilePath = @"C:\Users\$wagger\Desktop\UserManagementPBI\UserManagementPBI\Data\allowed users.csv";
                    await SeedAdminUsersFromCsvAsync(csvFilePath, userManager, roleManager, logger, emailSender);


                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                await roleManager.CreateAsync(role);
            }
        }

        private static async Task SeedAdminUsersFromCsvAsync(
            string filePath,
            UserManager<Admins> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger,
            IEmailSender emailSender)
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning($"CSV file not found: {filePath}");
                return;
            }

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<AdminUserCsv>();

            foreach (var record in records)
            {
                var user = await userManager.FindByEmailAsync(record.Email);
                if (user == null)
                {
                    user = new Admins
                    {
                        UserName = record.FullName,
                        Email = record.Email,
                        Nom = record.FullName
                    };
                    var generatedPassword = GenerateSecurePassword();

                    // Replace with a secure or random password logic as needed
                    var result = await userManager.CreateAsync(user, generatedPassword);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                        logger.LogInformation($"Admin user '{record.Email}' created and assigned to Admin role.");

                        // Envoi des identifiants
                        var subject = "Vos identifiants de compte administrateur";
                        var message = $@"
                        Bonjour {record.FullName},

                        Votre compte administrateur a été créé avec succès. Voici vos identifiants de connexion :

                        Nom d'utilisateur : {record.FullName}  
                        Mot de passe : {generatedPassword}

                        Veuillez vous connecter et changer votre mot de passe dès que possible.

                        Cordialement,  
                        L'équipe Admin
                        ";
                        await emailSender.SendEmailAsync(record.Email, subject, message);

                        logger.LogInformation($"Credentials sent to '{record.Email}'.");
                    }

                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            logger.LogError($"Error creating user '{record.Email}': {error.Description}");
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"User '{record.Email}' already exists.");
                }
            }
        }
        private static string GenerateSecurePassword(int length = 12)
        {
            if (length < 4)
                throw new ArgumentException("Password length must be at least 4 to satisfy complexity requirements.");

            const string lowers = "abcdefghijklmnopqrstuvwxyz";
            const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specials = "!@#$%^&*()-_=+[]{}<>?";

            string allChars = lowers + uppers + digits + specials;
            var random = new RNGCryptoServiceProvider();

            char[] password = new char[length];

            // Ensure each required character category is included
            password[0] = GetRandomChar(lowers, random);
            password[1] = GetRandomChar(uppers, random);
            password[2] = GetRandomChar(digits, random);
            password[3] = GetRandomChar(specials, random);

            for (int i = 4; i < length; i++)
            {
                password[i] = GetRandomChar(allChars, random);
            }

            // Shuffle the password characters so the guaranteed ones are not always at the front
            return Shuffle(password, random);
        }

        private static char GetRandomChar(string chars, RNGCryptoServiceProvider random)
        {
            byte[] buffer = new byte[1];
            char result;
            do
            {
                random.GetBytes(buffer);
                int idx = buffer[0] % chars.Length;
                result = chars[idx];
            } while (!chars.Contains(result));
            return result;
        }

        private static string Shuffle(char[] array, RNGCryptoServiceProvider random)
        {
            int n = array.Length;
            while (n > 1)
            {
                byte[] box = new byte[1];
                random.GetBytes(box);
                int k = box[0] % n;
                n--;
                (array[n], array[k]) = (array[k], array[n]);
            }
            return new string(array);
        }


        private class AdminUserCsv
        {
            [Name("Full Name")]
            public string FullName { get; set; }
            public string Email { get; set; }
        }
    }



}
