using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UserManagementPBI.Data;
using UserManagementPBI.Models;
using UserManagementPBI.ViewModels;

namespace UserManagementPBI.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var users = await _context.Users
                .FirstOrDefaultAsync(m => m.ID == id);
            if (users == null)
            {
                return NotFound();
            }

            return View(users);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            _logger.LogInformation("POST Create called.");

            // Assign required fields before checking ModelState
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _context.Admins.FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser == null)
            {
                _logger.LogWarning("Current authenticated user not found.");
                return Unauthorized(); // or handle appropriately
            }

            var CreatedByAdmin = currentUser;
            var CreatedByAdminId = currentUser.Id;
            var DateCreation = DateTime.Now;
            var DateModification = DateCreation;
            var Pwd = GenerateSecurePassword(); // Generate a secure password    



            if (_context.Users.Any(u => u.userName == vm.UserName))
            {
                ModelState.AddModelError("UserName", "Username is already taken.");
            }



            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Attempting to save BIUser: {UserName}, Client: {Client}, CreatedBy: {CreatedByAdminId}",
                        vm.UserName, vm.Client, CreatedByAdminId);

                    var bIUser = new Users
                    {
                        userName = vm.UserName,
                        role = vm.BIUserRole,
                        client = vm.Client,
                        mail = vm.Mail,
                        view_user = vm.View_user,
                        DateCreation = DateCreation,
                        DateModification = DateModification,
                        CreatedByAdmin = CreatedByAdmin,
                        CreatedByAdminId = CreatedByAdminId,
                        pwd = Pwd, // Store the generated password
                        last_failed_utc_datetime = null, // Initialize to null
                        failed_times = 0 // Initialize failed attempts
                        // Add any default or audit values as needed
                    };

                    _context.Add(bIUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("BIUser saved successfully with ID={Id}", bIUser.ID);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while saving BIUser.");
                    ModelState.AddModelError("", "An error occurred while saving the user.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Errors:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Field: {Field}, Error: {ErrorMessage}", state.Key, error.ErrorMessage);
                    }
                }
            }
            return View(vm);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var vm = new UserFormViewModel
            {
                Id = user.ID,
                UserName = user.userName,
                BIUserRole = user.role,
                Client = user.client,
                Mail = user.mail,
                View_user = user.view_user
            };

            return View(vm);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserFormViewModel vm)
        {
            if (id != vm.Id)
            {
                return NotFound();
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // Load the existing entity from the database
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Update only the fields that are allowed to change
                    existingUser.userName = vm.UserName;
                    existingUser.role = vm.BIUserRole;
                    existingUser.client = vm.Client;
                    existingUser.mail = vm.Mail;
                    existingUser.view_user = vm.View_user;
                    existingUser.DateModification = DateTime.Now;

                    // Save the changes
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsersExists(vm.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        
            return View(vm);
        }


        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var users = await _context.Users
                .FirstOrDefaultAsync(m => m.ID == id);
            if (users == null)
            {
                return NotFound();
            }

            return View(users);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var users = await _context.Users.FindAsync(id);
            if (users != null)
            {
                _context.Users.Remove(users);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UsersExists(int id)
        {
            return _context.Users.Any(e => e.ID == id);
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

    }
}
