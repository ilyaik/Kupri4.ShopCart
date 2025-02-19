﻿using Kupri4.ShopCart.Infrastructure;
using Kupri4.ShopCart.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kupri4.ShopCart.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ShopCartDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoriesController(ShopCartDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
        }

        //GET /admin/categories
        public async Task<ViewResult> Index()
        {
            var categories = await _dbContext.Categories
                .OrderBy(x => x.Sorting)
                .ToListAsync();

            return View(categories);
        }

        //GET /admin/categories/create
        public ViewResult Create() =>
            View();

        //POST /admin/categories/create
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.Slug = Regex.Replace(category.Name.ToLower().Trim(), @"\s+", "-");

            if (await _dbContext.Pages.FirstOrDefaultAsync(x => x.Slug == category.Slug) != null)
            {
                ModelState.AddModelError("", " The page already exists");
                return View(category);
            }

            category.Sorting = 100;

            TempData["Success"] = $"Category \"{category.Name}\' has been added";

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET /admin/categories/edit/2
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);

            if (category != null)
            {
                return View(category);
            }

            return NotFound();
        }

        //POST /admin/categories/edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.Slug = Regex.Replace(category.Name.ToLower().Trim(), @"\s+", "-");

            if (await _dbContext.Categories.Where(x => x.Id != category.Id).FirstOrDefaultAsync(x => x.Slug == category.Slug) != null)
            {
                ModelState.AddModelError("", " The category already exists");
                return View(category);
            }

            TempData["Success"] = $"Category \"{category.Name}\' has been edited";

            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Get /admin/categories/delete/3
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _dbContext.Categories.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
            {
                TempData["Error"] = $"Category does not exist";
                return RedirectToAction(nameof(Index));
            }

            string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media", "products");
            foreach (var product in category.Products)
            {
                if (product.Image != "noimage.png")
                {
                    string imgOldPath = Path.Combine(uploadsDir, product.Image);
                    if (System.IO.File.Exists(imgOldPath))
                    {
                        System.IO.File.Delete(imgOldPath);
                    }
                }
            }


            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = $"Category \"{category.Name}\' has been deleted";

            return RedirectToAction(nameof(Index));
        }

        //POST /admin/categories/reorder
        [HttpPost]
        public async Task<IActionResult> Reorder(int[] id)
        {
            int count = 1;

            foreach (int categoryId in id)
            {
                Category category = await _dbContext.Categories.FindAsync(categoryId);
                if (category != null)
                {
                    category.Sorting = count++;
                    await _dbContext.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
