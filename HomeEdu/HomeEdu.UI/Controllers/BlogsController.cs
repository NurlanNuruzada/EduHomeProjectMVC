﻿using AutoMapper;
using HomeEdu.UI.Areas.Admin.ViewModels.BlogViewModels;
using HomeEdu.Core.Entities;
using HomeEdu.DataAccess.Context;
using HomeEdu.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Authorization;
using static HomeEdu.UI.Helpers.Utilities.AppUserRole;

namespace HomeEdu.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BlogsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        public BlogsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<IActionResult> Index()
        {
            PagesVM pagesVM = new()
            {
                Courses = await _context.Courses.Include(c => c.CourseDetail).ToListAsync(),
                Blogs = await _context.Blogs.ToListAsync()
            };
            if (pagesVM == null)
            {
                return NotFound();
            }
            return View(pagesVM);
        }
    }
}
