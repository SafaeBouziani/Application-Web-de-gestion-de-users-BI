using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UserManagementPBI.Data;
using UserManagementPBI.Models;
using UserManagementPBI.ViewModels;

namespace UserManagementPBI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            return View(await _context.Reports.ToListAsync());
        }

        // GET: Users/All
        public async Task<IActionResult> IndexAll()
        {
            return View("Index", await _context.Reports.IgnoreQueryFilters().ToListAsync());
        }

        public async Task<IActionResult> IndexInactivated()
        {
            // Fetch only inactive users

            return View("Index", await _context.Reports.IgnoreQueryFilters()
                .Where(u => !u.is_active)
                .ToListAsync());
        }


        // GET: Reports/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // Load the group with child reports and user joins
            var group = await _context.Reports
                .Include(r => r.ReportsBIs)
                .Include(r => r.UsersReports)
                .ThenInclude(ur => ur.User)
                .FirstOrDefaultAsync(r => r.ID == id);

            if (group == null)
            {
                return NotFound();
            }

            // Build VM
   

            // Build Users map for the view (fallback if window.__usersMap is missing)
            // Assumes you have a Users DbSet with Id (int) and Name (string) or similar
            var usersMap = await _context.Users
                .OrderBy(u => u.userName) // adapt property names if different
                .ToDictionaryAsync(u => u.ID, u => u.userName);

            ViewBag.UsersMap = usersMap;



            return View(group);
        }

        // GET: Reports/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Reports/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,title,commentaire")] Reports reports)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reports);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reports);
        }

        [HttpGet]
        public IActionResult CreateModal()
        {
            var users = _context.Users
                        .Select(r => new SelectListItem { Value = r.ID.ToString(), Text = r.userName })
                        .ToList();
            ViewBag.ExistingUsers = users;
            ViewBag.UsersMap = users.ToDictionary(u => u.Value, u => u.Text);


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
            var vm = new CreateGroupAjaxViewModel();
            return PartialView("_CreateReportModal", vm);
        }

        // POST: Users/CreateUserAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReportAjax([FromBody] CreateGroupAjaxViewModel vm)
        {
            if (vm == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

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
                    _context.Reports.Add(newGroup);
                    await _context.SaveChangesAsync();

                    // Reports inside the group
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
                                report = r.Report,
                                ReportGroup = newGroup
                            };
                            _context.Reports_Reports_BI.Add(rr);
                        }
                    }

                    // Users assigned to group
                    if (g.SelectedExistingUserIds != null && g.SelectedExistingUserIds.Any())
                    {
                        foreach (var uid in g.SelectedExistingUserIds)
                        {
                            var groupUser = new Users_Reports
                            {
                                id_reports = newGroup.ID,
                                id_users = uid
                            };
                            _context.Users_Reports.Add(groupUser);
                        }
                    }
                }
            }

            // Final save
            await _context.SaveChangesAsync();

            // Return success JSON
            return Json(new { success = true });
        }

        // GET: Reports/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reports = await _context.Reports.FindAsync(id);
            if (reports == null)
            {
                return NotFound();
            }
            return View(reports);
        }

        // POST: Reports/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,title,commentaire")] Reports reports)
        {
            if (id != reports.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reports);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReportsExists(reports.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(reports);
        }
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            // Load the group with child reports and user joins
            var group = await _context.Reports
                .Include(r => r.ReportsBIs)
                .Include(r => r.UsersReports)
                .FirstOrDefaultAsync(r => r.ID == id);

            if (group == null)
            {
                return NotFound();
            }

            // Build VM
            var vm = new GroupCreateViewModel
            {
                Id = group.ID,
                Title = group.title,
                Comment = group.commentaire,
                Reports = group.ReportsBIs?
                    .OrderBy(c => c.order_report)
                    .Select(c => new ReportCreateViewModel
                    {
                        Title = c.title,
                        Id = c.id_report_bi,
                        IdWeb = c.id_web,
                        Order = c.order_report ?? 1,
                        Report = c.report
                    }).ToList() ?? new List<ReportCreateViewModel>(),
                SelectedExistingUserIds = group.UsersReports?.Select(u => u.id_users).ToList() ?? new List<int>()
            };

            // Build Users map for the view (fallback if window.__usersMap is missing)
            // Assumes you have a Users DbSet with Id (int) and Name (string) or similar
            var usersMap = await _context.Users
                .OrderBy(u => u.userName) // adapt property names if different
                .ToDictionaryAsync(u => u.ID, u => u.userName);

            ViewBag.UsersMap = usersMap;

            return PartialView("_EditReportModal", vm);
        }

        // POST: /Reports/EditReportAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReportAjax([FromBody] GroupCreateViewModel model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Invalid payload." });
            }

            var group = await _context.Reports
                .Include(r => r.ReportsBIs)
                .Include(r => r.UsersReports)
                .FirstOrDefaultAsync(r => r.ID == model.Id);

            if (group == null)
            {
                return NotFound(new { success = false, message = "Group not found." });
            }

            // Update group fields
            group.title = model.Title?.Trim();
            group.commentaire = model.Comment;

            // ---- Sync Reports (compare by id_report_bi) ----
            var incomingReports = (model.Reports ?? new List<ReportCreateViewModel>())
                                  .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                                  .ToList();

            var existingChildren = group.ReportsBIs?.ToList() ?? new List<Reports_Reports_BI>();
            var incomingIds = new HashSet<string>(incomingReports.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);

            // Remove children not present anymore
            foreach (var child in existingChildren.Where(c => !incomingIds.Contains(c.id_report_bi)).ToList())
            {
                child.is_active = false;
            }

            // Update existing & add new
            foreach (var incoming in incomingReports)
            {
                var match = existingChildren.FirstOrDefault(c => string.Equals(c.id_report_bi, incoming.Id, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    match.title = incoming.Title;
                    match.id_web = incoming.IdWeb;
                    match.order_report = incoming.Order;
                    match.report = incoming.Report;
                }
                else
                {
                    var newChild = new Reports_Reports_BI
                    {
                        id_report_bi = incoming.Id,
                        title = incoming.Title,
                        id_web = incoming.IdWeb,
                        order_report = incoming.Order,
                        report = incoming.Report,
                        ReportGroup = group // set FK via navigation
                    };
                    _context.Reports_Reports_BI.Add(newChild);
                }
            }

            // ---- Sync Users (join table) ----
            var incomingUserIds = new HashSet<int>(model.SelectedExistingUserIds ?? new List<int>());
            var existingJoins = group.UsersReports?.ToList() ?? new List<Users_Reports>();

            // Remove unselected
            foreach (var join in existingJoins.Where(j => !incomingUserIds.Contains(j.id_users)).ToList())
            {
                _context.Users_Reports.Remove(join);
            }

            // Add newly selected
            var existingUserIds = new HashSet<int>(existingJoins.Select(j => j.id_users));
            foreach (var uid in incomingUserIds.Where(id => !existingUserIds.Contains(id)))
            {
                _context.Users_Reports.Add(new Users_Reports
                {
                    id_reports = group.ID,
                    id_users = uid
                });
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, groupId = group.ID });
        }
        // GET: Reports/GetAddReportBIModal/5
        [HttpGet]
        public IActionResult GetAddReportBIModal(int id)
        {
            // Build catalog selectlist
            var catalogList = _context.Catalog
                .Select(c => new {
                    c.ItemID,
                    Display = c.Name + " (/" + c.Path + ")",
                    c.Name,
                    c.Path
                }).ToList();

            ViewData["id_report_bi"] = new SelectList(catalogList, "ItemID", "Display");
            ViewBag.CatalogMap = catalogList.ToDictionary(c => c.ItemID.ToString(), c => new { c.Name, c.Path });
            ViewBag.ReportGroupId = id;

            return PartialView("_AddReportBIModal", new ReportsReportsBIFormViewModel());
        }

        // POST: Reports/AddReportBI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReportBI(ReportsReportsBIFormViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid model" });

            var newRep = new Reports_Reports_BI
            {
                id_report_bi = model.id_report_bi,
                title = model.title,
                id_web = model.id_web,
                order_report = model.order_report,
                report = model.report,
                id_report = model.id_report
            };

            _context.Reports_Reports_BI.Add(newRep);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: Reports/GetAddUserModal/5
        [HttpGet("Reports/GetAddUserModal/{groupId}")]
        public async Task<IActionResult> GetAddUserModal(int groupId)
        {
            // Load the group with child reports and user joins
            var group = await _context.Reports
                .Include(r => r.UsersReports)
                .FirstOrDefaultAsync(r => r.ID == groupId);

            if (group == null)
            {
                return NotFound();
            }

            // Build VM
            var vm = new AddUserToGroupViewModel
            {
                Id = group.ID,
                SelectedExistingUserIds = group.UsersReports?.Select(u => u.id_users).ToList() ?? new List<int>()
            };

            // Build Users map for the view (fallback if window.__usersMap is missing)
            // Assumes you have a Users DbSet with Id (int) and Name (string) or similar
            var usersMap = await _context.Users
                .OrderBy(u => u.userName) // adapt property names if different
                .ToDictionaryAsync(u => u.ID, u => u.userName);

            ViewBag.UsersMap = usersMap;


            var selectedUserIds = group.UsersReports.Select(ur => ur.id_users).ToList();

            ViewBag.SelectedUserIds = selectedUserIds;

            return PartialView("_AddUserModal",vm);
        }

        // POST: Reports/AddUserToGroup
        [HttpPost]
        public async Task<IActionResult> AddUserToGroup([FromBody] AddUserToGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false });
            var group = await _context.Reports
                .Include(r => r.ReportsBIs)
                .Include(r => r.UsersReports)
                .FirstOrDefaultAsync(r => r.ID == model.Id);
            var incomingUserIds = new HashSet<int>(model.SelectedExistingUserIds ?? new List<int>());
            var existingJoins = group.UsersReports?.ToList() ?? new List<Users_Reports>();
            var existingUserIds = new HashSet<int>(existingJoins.Select(j => j.id_users));
            foreach (var uid in incomingUserIds.Where(id => !existingUserIds.Contains(id)))
            {
                _context.Users_Reports.Add(new Users_Reports
                {
                    id_reports = group.ID,
                    id_users = uid
                });
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }




        // GET: Reports/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reports = await _context.Reports
                .FirstOrDefaultAsync(m => m.ID == id);
            if (reports == null)
            {
                return NotFound();
            }

            return View(reports);
        }

        // POST: Reports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var user = await _context.Reports.FindAsync(id);
            if (user == null) return NotFound();

            user.is_active = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }

        // POST: Reports/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.Reports.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ID == id);
            if (user == null) return NotFound();
            user.is_active = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReportsExists(int id)
        {
            return _context.Reports.Any(e => e.ID == id);
        }
    }
}
