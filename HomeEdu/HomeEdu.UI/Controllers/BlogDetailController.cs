﻿using HomeEdu.Core.Entities;
using HomeEdu.DataAccess.Context;
using HomeEdu.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;

namespace HomeEdu.UI.Controllers
{
    public class BlogDetailController : Controller
    {
        private readonly AppDbContext _context;

        public BlogDetailController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int Id)
        {
            HomeVM homeVM = new()
            {
                Blogs = await _context.Blogs.Where(s => s.Id == Id).ToListAsync()
            };
            ViewBag.Catagories = await _context.Blogs.Include(c => c.BlogCatagory).ToListAsync();
            List<Blog> Blogs = await _context.Blogs.Include(c => c.BlogCatagory).ToListAsync();
            return View(homeVM);
        }
    }
}
