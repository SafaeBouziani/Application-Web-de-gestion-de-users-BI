using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UserManagementPBI.Data;
using UserManagementPBI.Models;
using UserManagementPBI.ViewModels;

namespace UserManagementPBI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class Reports_Reports_BIController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Reports_Reports_BIController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports_Reports_BI
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Reports_Reports_BI.Include(r => r.ReportGroup);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Users/All
        public async Task<IActionResult> IndexAll()
        {
            return View("Index", await _context.Reports_Reports_BI.IgnoreQueryFilters().ToListAsync());
        }

        public async Task<IActionResult> IndexInactivated()
        {
            // Fetch only inactive users

            return View("Index", await _context.Reports_Reports_BI.IgnoreQueryFilters()
                .Where(u => !u.is_active)
                .ToListAsync());
        }
        // GET: Reports_Reports_BI/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reports_Reports_BI = await _context.Reports_Reports_BI
                .Include(r => r.ReportGroup)
                .FirstOrDefaultAsync(m => m.ID_Reports_Reports_BI == id);
            if (reports_Reports_BI == null)
            {
                return NotFound();
            }

            return View(reports_Reports_BI);
        }

        // GET: Reports_Reports_BI/Create
        public IActionResult Create()
        {
            ViewData["id_report"] = new SelectList(_context.Reports, "ID", "title");
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
            return View(new ReportsReportsBIFormViewModel());
        }

        // POST: Reports_Reports_BI/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportsReportsBIFormViewModel vm)
        {

            if (ModelState.IsValid)
            {
                var reports_Reports_BI = new Reports_Reports_BI
                {
                    id_report_bi = vm.id_report_bi,
                    title = vm.title,
                    id_web = vm.id_web,
                    id_report = vm.id_report,
                    report = vm.report,
                    order_report = vm.order_report,
                    ReportGroup = await _context.Reports.FindAsync(vm.id_report)
                };
                _context.Add(reports_Reports_BI);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["id_report"] = new SelectList(_context.Reports, "ID", "ID", vm.id_report);
            var catalogList = _context.Catalog
                .Select(c => new {
                    c.ItemID,
                    Display = c.Name + " (/" + c.Path + ")",
                    c.Name,
                    c.Path
                })
                .ToList();

            // Populate dropdown with custom display
            ViewData["id_report_bi"] = new SelectList(catalogList, "ItemID", "Display", vm.id_report_bi);

            // Map ItemID to Name and Path for JS
            ViewBag.CatalogMap = catalogList.ToDictionary(c => c.ItemID.ToString(), c => new { c.Name, c.Path });
            return View(vm);
        }

        // GET: Reports_Reports_BI/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reports_Reports_BI = await _context.Reports_Reports_BI
                .Include(r => r.ReportGroup)
                .FirstOrDefaultAsync(r => r.ID_Reports_Reports_BI == id);

            if (reports_Reports_BI == null)
            {
                return NotFound();
            }

            var vm = new ReportsReportsBIFormViewModel
            {
                ID_Reports_Reports_BI = reports_Reports_BI.ID_Reports_Reports_BI,
                id_report_bi = reports_Reports_BI.id_report_bi,
                title = reports_Reports_BI.title,
                id_web = reports_Reports_BI.id_web,
                id_report = reports_Reports_BI.id_report,
                report = reports_Reports_BI.report,
                order_report = reports_Reports_BI.order_report
            };
            ViewData["id_report"] = new SelectList(_context.Reports, "ID", "title", vm.id_report);

            return View(vm);
        }

        // POST: Reports_Reports_BI/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST: Reports_Reports_BI/Edit/5
        public async Task<IActionResult> Edit(int id, ReportsReportsBIFormViewModel vm)
        {
            if (id != vm.ID_Reports_Reports_BI)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.Reports_Reports_BI.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }

                // Do NOT modify id_report_bi
                entity.title = vm.title;
                entity.id_web = vm.id_web;
                entity.id_report = vm.id_report;
                entity.report = vm.report;
                entity.order_report = vm.order_report;
                entity.ReportGroup = await _context.Reports.FindAsync(vm.id_report);

                try
                {
                    _context.Update(entity);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Reports_Reports_BIExists(vm.ID_Reports_Reports_BI))
                        return NotFound();
                    throw;
                }
            }

            ViewData["id_report"] = new SelectList(_context.Reports, "ID", "ID", vm.id_report);

            return View(vm);
        }


        // GET: Reports_Reports_BI/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reports_Reports_BI = await _context.Reports_Reports_BI
                .Include(r => r.ReportGroup)
                .FirstOrDefaultAsync(m => m.ID_Reports_Reports_BI == id);
            if (reports_Reports_BI == null)
            {
                return NotFound();
            }

            return View(reports_Reports_BI);
        }

        // POST: Reports_Reports_BI/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var report = await _context.Reports_Reports_BI.FindAsync(id);
            if (report == null) return NotFound();

            report.is_active = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.Reports_Reports_BI.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ID_Reports_Reports_BI == id);
            if (user == null) return NotFound();
            user.is_active = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Reports_Reports_BIExists(int id)
        {
            return _context.Reports_Reports_BI.Any(e => e.ID_Reports_Reports_BI == id);
        }
    }
}
