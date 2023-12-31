﻿using HomeEdu.Core.Entities;
using HomeEdu.DataAccess.Context;
using HomeEdu.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;
using static HomeEdu.UI.Helpers.Utilities.AppUserRole;

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
                Blogs = await _context.Blogs.Where(s => s.Id == Id).ToListAsync(),
                BlogCatagories = await _context.BlogCatagories.ToListAsync(),
            };
            List<Blog> Blogs = await _context.Blogs.Include(c => c.BlogCatagory).ToListAsync();
            return View(homeVM);
        }
    }
}
