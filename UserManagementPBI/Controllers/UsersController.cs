using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using UserManagementPBI.Data;
using UserManagementPBI.Models;
using UserManagementPBI.ViewModels;

namespace UserManagementPBI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IEmailSender _sender;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger, IEmailSender sender)
        {
            _context = context;
            _logger = logger;
            _sender = sender;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/All
        public async Task<IActionResult> IndexAll()
        {
            return View("Index", await _context.Users.IgnoreQueryFilters().ToListAsync());
        }

        public async Task<IActionResult> IndexInactivated()
        {
            // Fetch only inactive users

            return View("Index", await _context.Users.IgnoreQueryFilters()
                .Where(u => !u.is_active)
                .ToListAsync());
        }


        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var user = await _context.Users
                .Include(u => u.CreatedByAdmin)
                .Include(u => u.UsersReports)
                    .ThenInclude(ur => ur.Report)
                        .ThenInclude(r => r.ReportsBIs)
                .FirstOrDefaultAsync(u => u.ID == id);

            if (user == null)
                return NotFound();

            return View(user);

        }

        // GET: Users/CreateModal
        [HttpGet]
        public IActionResult CreateModal()
        {
            // Build existing groups select list from Reports table
            var groups = _context.Reports
                .Select(r => new SelectListItem { Value = r.ID.ToString(), Text = r.title })
                .ToList();

            ViewBag.ExistingGroups = groups;
            // Pass an empty model to the partial (JS will populate the state)
            var catalogList = _context.Catalog
                .Select(c => new {
                    c.ItemID,
                    Display = c.Name + " (/" + c.Path + ")",
                    c.Name,
                    c.Path
                })
                .ToList();

            // Populate dropdown with custom display
            ViewData["id_report_bi"] = new SelectList(catalogList, "ItemID", "Display");

            // Map ItemID to Name and Path for JS
            ViewBag.CatalogMap = catalogList.ToDictionary(c => c.ItemID.ToString(), c => new { c.Name, c.Path });
            var vm = new CreateUserAjaxViewModel();
            return PartialView("_CreateUserModal", vm);
        }

        // POST: Users/CreateUserAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUserAjax([FromBody] CreateUserAjaxViewModel vm)
        {
            if (vm == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

            // server validation
            if (string.IsNullOrWhiteSpace(vm.UserName))
                return BadRequest(new { success = false, message = "Username is required." });

            // check duplicate username
            if (_context.Users.Any(u => u.userName == vm.UserName))
            {
                return BadRequest(new { success = false, message = "Username already exists." });
            }

            // Try to determine the admin creating this user (optional)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _context.Admins.FirstOrDefaultAsync(a => a.Id == userId);

            var createdById = currentUser?.Id;

            // Create Users entity
            var now = DateTime.Now;
            var generatedPwd = GenerateSecurePassword(); // method already in this controller

            var newUser = new Users
            {
                userName = vm.UserName,
                role = vm.BIUserRole,
                client = vm.Client,
                mail = vm.Mail,
                view_user = vm.View_user,
                DateCreation = now,
                DateModification = now,
                CreatedByAdminId = createdById,
                pwd = generatedPwd,
                failed_times = 0
            };

            // Add user first so we have its ID for links
            _context.Users.Add(newUser);
            var subject = "Vos identifiants de compte SY";
            var message = $@"
                        Bonjour {newUser.userName},

                        Votre compte SY a été créé avec succès. Voici vos identifiants de connexion :

                        Nom d'utilisateur : {newUser.userName}  
                        Mot de passe : {newUser.pwd}

                        Veuillez vous connecter et changer votre mot de passe dès que possible.

                        Cordialement,  
                        L'équipe Admin
                        ";
            await _context.SaveChangesAsync();
            await _sender.SendEmailAsync(newUser.mail, subject, message);
            

            // Link selected existing groups (Reports) to user via Users_Reports
            if (vm.SelectedExistingGroupIds != null)
            {
                foreach (var gid in vm.SelectedExistingGroupIds.Distinct())
                {
                    // ensure the report-group exists
                    var existingGroup = await _context.Reports.FindAsync(gid);
                    if (existingGroup != null)
                    {
                        var ur = new Users_Reports
                        {
                            id_users = newUser.ID,
                            id_reports = existingGroup.ID
                        };
                        _context.Add(ur);
                    }
                }
            }

            // Create new groups + their BI reports and link them
            if (vm.NewGroups != null && vm.NewGroups.Any())
            {
                // Create Report groups
                foreach (var g in vm.NewGroups)
                {
                    var newGroup = new Reports
                    {
                        title = g.Title,
                        commentaire = g.Comment
                    };
                    _context.Reports.Add(newGroup);
                    // Save so newGroup.ID is assigned (or we could SaveChanges once after loop).
                    await _context.SaveChangesAsync();

                    // Add Reports_Reports_BI entries (BI reports inside group)
                    if (g.Reports != null && g.Reports.Any())
                    {
                        foreach (var r in g.Reports)
                        {
                            var rr = new Reports_Reports_BI
                            {
                                title = r.Title,
                                id_report_bi = r.Id,
                                order_report = r.Order,
                                id_web = r.IdWeb,
                                report = r.Report, // Assuming this is a byte array for the report content
                                // set navigation
                                ReportGroup = newGroup
                            };
                            _context.Reports_Reports_BI.Add(rr);
                        }
                    }

                    // Link the newly created group to the user
                    var urNew = new Users_Reports
                    {
                        id_users = newUser.ID,
                        id_reports = newGroup.ID
                    };
                    _context.Users_Reports.Add(urNew);
                    await _context.SaveChangesAsync();
                }
            }

            // Final save
            await _context.SaveChangesAsync();

            // Return success JSON
            return Json(new { success = true, userId = newUser.ID });
        }

        public async Task<IActionResult> EditModal(int id)
        {
            var user = await _context.Users
                .Include(u => u.UsersReports)
                .FirstOrDefaultAsync(u => u.ID == id);

            if (user == null)
                return NotFound();

            var existingGroupIds = user.UsersReports
                .Select(ur => ur.id_reports)
                .ToList();

            var vm = new CreateUserAjaxViewModel
            {
                Id = user.ID,
                UserName = user.userName,
                BIUserRole = user.role,
                Client = user.client,
                Mail = user.mail,
                View_user = user.view_user,
                SelectedExistingGroupIds = existingGroupIds
            };

            var groups = _context.Reports
                .Select(r => new SelectListItem { Value = r.ID.ToString(), Text = r.title + " (ID: " + r.ID + ")" })
                .ToList();
            ViewBag.ExistingGroups = groups;
            

            var catalogList = _context.Catalog
                .Select(c => new {
                    c.ItemID,
                    Display = c.Name + " (/" + c.Path + ")",
                    c.Name,
                    c.Path
                })
                .ToList();

            ViewData["id_report_bi"] = new SelectList(catalogList, "ItemID", "Display");
            ViewBag.CatalogMap = catalogList.ToDictionary(c => c.ItemID.ToString(), c => new { c.Name, c.Path });

            return PartialView("_CreateUserModal", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserAjax(int id, [FromBody] CreateUserAjaxViewModel vm)
        {
            if (id != vm.Id)
                return NotFound();

            if (vm == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Validation failed." });

            try
            {
                // Load user with their group associations
                var existingUser = await _context.Users
                    .Include(u => u.UsersReports)
                    .FirstOrDefaultAsync(u => u.ID == id);

                if (existingUser == null)
                    return NotFound();

                // Update fields
                existingUser.userName = vm.UserName;
                existingUser.role = vm.BIUserRole;
                existingUser.client = vm.Client;
                existingUser.mail = vm.Mail;
                existingUser.view_user = vm.View_user;
                existingUser.DateModification = DateTime.Now;

                // Step 1: Manage existing group associations
                var currentGroupIds = existingUser.UsersReports.Select(ur => ur.id_reports).ToList();
                var selectedGroupIds = vm.SelectedExistingGroupIds?.Distinct().ToList() ?? new List<int>();

                // Add missing groups
                var groupsToAdd = selectedGroupIds.Except(currentGroupIds).ToList();
                foreach (var gid in groupsToAdd)
                {
                    _context.Users_Reports.Add(new Users_Reports
                    {
                        id_users = existingUser.ID,
                        id_reports = gid
                    });
                }

                // Remove unselected groups
                var groupsToRemove = currentGroupIds.Except(selectedGroupIds).ToList();
                if (groupsToRemove.Any())
                {
                    var toRemove = existingUser.UsersReports
                        .Where(ur => groupsToRemove.Contains(ur.id_reports))
                        .ToList();
                    _context.Users_Reports.RemoveRange(toRemove);
                }

                // Step 2: Create new groups + their reports + user association
                if (vm.NewGroups != null && vm.NewGroups.Any())
                {
                    foreach (var g in vm.NewGroups)
                    {
                        var newGroup = new Reports
                        {
                            title = g.Title,
                            commentaire = g.Comment
                        };

                        // Add group to EF change tracker
                        _context.Reports.Add(newGroup);

                        // Add its reports
                        if (g.Reports != null && g.Reports.Any())
                        {
                            foreach (var r in g.Reports)
                            {
                                _context.Reports_Reports_BI.Add(new Reports_Reports_BI
                                {
                                    title = r.Title,
                                    id_report_bi = r.Id,
                                    order_report = r.Order,
                                    id_web = r.IdWeb,
                                    report = r.Report,
                                    ReportGroup = newGroup // EF will set FK automatically
                                });
                            }
                        }

                        // Associate this new group with the user
                        _context.Users_Reports.Add(new Users_Reports
                        {
                            id_users = existingUser.ID,
                            Report = newGroup // let EF handle the foreign key
                        });

                    }
                }

                // Commit all changes at once
                await _context.SaveChangesAsync();

                return Json(new { success = true, userId = existingUser.ID });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(vm.Id))
                    return NotFound();
                else
                    throw;
            }
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

                    return RedirectToAction(nameof(Details), new { id = id });

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
       
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.is_active = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
        [HttpGet]
        public async Task<IActionResult> AssignGroup(int id)
        {
            var user = await _context.Users
                .Include(u => u.UsersReports)
                .FirstOrDefaultAsync(u => u.ID == id);

            if (user == null)
                return NotFound();

            var existingGroupIds = user.UsersReports
                .Select(ur => ur.id_reports)
                .ToList();

            var vm = new AssignGroupsToUserViewModel
            {
                Id = user.ID,
                SelectedExistingGroupIds = existingGroupIds
            };

            var groups = _context.Reports
                .Select(r => new SelectListItem { Value = r.ID.ToString(), Text = r.title + " (ID: " + r.ID + ")" })
                .ToList();
            ViewBag.ExistingGroups = groups;


            var catalogList = _context.Catalog
                .Select(c => new {
                    c.ItemID,
                    Display = c.Name + " (/" + c.Path + ")",
                    c.Name,
                    c.Path
                })
                .ToList();

            ViewData["id_report_bi"] = new SelectList(catalogList, "ItemID", "Display");
            ViewBag.CatalogMap = catalogList.ToDictionary(c => c.ItemID.ToString(), c => new { c.Name, c.Path });


            return PartialView("_AssignGroupModal",vm);
        }

        // POST: Users/AssignGroup/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignGroup(int id, [FromBody] AssignGroupsToUserViewModel vm)
        {
            if (vm == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

            var user = await _context.Users
                .Include(u => u.UsersReports)
                .FirstOrDefaultAsync(u => u.ID == id);
            if (user == null)
                return NotFound();
            // Clear existing group associations
            _context.Users_Reports.RemoveRange(user.UsersReports);
            // Add selected existing groups
            if (vm.SelectedExistingGroupIds != null)
            {
                foreach (var gid in vm.SelectedExistingGroupIds.Distinct())
                {
                    var existingGroup = await _context.Reports.FindAsync(gid);
                    if (existingGroup != null)
                    {
                        _context.Users_Reports.Add(new Users_Reports
                        {
                            id_users = user.ID,
                            id_reports = existingGroup.ID
                        });
                    }
                }
            }
            // Create new groups + their BI reports and link them
            if (vm.NewGroups != null && vm.NewGroups.Any())
            {
                foreach (var g in vm.NewGroups)
                {
                    var newGroup = new Reports
                    {
                        title = g.Title,
                        commentaire = g.Comment
                    };

                    // Track group, but don’t save yet
                    _context.Reports.Add(newGroup);

                    if (g.Reports != null && g.Reports.Any())
                    {
                        foreach (var r in g.Reports)
                        {
                            _context.Reports_Reports_BI.Add(new Reports_Reports_BI
                            {
                                title = r.Title,
                                id_report_bi = r.Id,
                                order_report = r.Order,
                                id_web = r.IdWeb,
                                report = r.Report,
                                ReportGroup = newGroup
                            });
                        }
                    }

                    // Use navigation instead of raw ID
                    _context.Users_Reports.Add(new Users_Reports
                    {
                        id_users = user.ID,
                        Report = newGroup
                    });
                }

                await _context.SaveChangesAsync();

            }
            return Json(new { success = true, userId = user.ID });
        }

        // POST: Users/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ID == id);
            if (user == null) return NotFound();
            user.is_active = true;
            user.DateModification = DateTime.Now;
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
