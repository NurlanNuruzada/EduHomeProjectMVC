﻿using HomeEdu.Core.Entities;
using HomeEdu.DataAccess.Context;
using HomeEdu.UI.Helpers.Utilities;
using HomeEdu.UI.Services.EmailService;
using HomeEdu.UI.ViewModels.AuthViewModels;
using HomeEdu.UI.ViewModels.EmailViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NuGet.Protocol;
using System.Net;
using System.Net.Sockets;

namespace HomeEdu.UI.Controllers
{
    public class AuthController
        : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _singInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _mailService;
        private readonly TimeSpan _tokenLifespan;

        public AuthController(UserManager<AppUser> userManager,
                              SignInManager<AppUser> singInManager,
                              RoleManager<IdentityRole> roleManager,
                              AppDbContext context,
                              IEmailService mailService,
                              IOptions<DataProtectionTokenProviderOptions> tokenOptions)
        {
            _userManager = userManager;
            _singInManager = singInManager;
            _roleManager = roleManager;
            _context = context;
            _mailService = mailService;
            _tokenLifespan = tokenOptions.Value.TokenLifespan;
        }

        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Register(RegisterVM newUser)
        {
            if (!ModelState.IsValid) return View(newUser);
            AppUser user = new()
            {
                UserName = newUser.UserName,
                Email = newUser.Email,
                FullName = string.Concat(newUser.UserName, " ", newUser.LastName),
            };
            IdentityResult result = await _userManager.CreateAsync(user, newUser.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(newUser);
            }
            await _userManager.AddToRoleAsync(user, AppUserRole.Admin);
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Login()
        {
            return View();

        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Login(LoginVM User)
        {
            if (!ModelState.IsValid) return View(User);
            AppUser UserName = await _userManager.FindByNameAsync(User.UserName);
            if (User is null)
            {
                ModelState.AddModelError("", "Invalid Login!");
                return View(User);
            }
            if (UserName is null)
            {
                ModelState.AddModelError("", "Invalid Login!");
                return View(User);
            }
            var signInResult = await _singInManager.PasswordSignInAsync(UserName, User.Password, User.RememberMe, true);
            if (signInResult.IsLockedOut)
            {
                ModelState.AddModelError("", "Your accound Is Locked Try Later!");
                return View(User);
            }
            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError("", "Invalid Login!");
                return View(User);
            }
            return RedirectToAction("Index", "Home");
        }
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                await _singInManager.SignOutAsync();
            }
            return RedirectToAction("Index", "Home");
        }
    
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }// bunu accond controllerder yaz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgetPasswordViewModel ForgetPaVM)
        {
            if (!ModelState.IsValid)
                return View(ForgetPaVM);

            var user = await _userManager.FindByEmailAsync(ForgetPaVM.Email);
            if (user == null)
                return RedirectToAction(nameof(ForgotPasswordConfirmation));

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var ResetPasswordUrl = $"<a href=\"{Url.Action(nameof(ResetPassword), "Auth", new { token, email = user.Email }, Request.Scheme)}\">reset your password</a>";

            var time = DateTime.Now.ToString();
            var userIp = GetUserIP().ToString();
            var Username = user.UserName.ToString();
            var Fullname = user.FullName.ToString();
            string pathToHtmlFile = "Views\\Auth\\ResetPasswordLetter.cshtml";
            string htmlContent = System.IO.File.ReadAllText(pathToHtmlFile);
            string tokenLifetimeString = FormatTokenLifetime(_tokenLifespan);

            htmlContent = htmlContent.Replace("{{action_url}}", ResetPasswordUrl);
            htmlContent = htmlContent.Replace("{{Time}}", time);
            htmlContent = htmlContent.Replace("{{IpAddress}}", userIp);
            htmlContent = htmlContent.Replace("{{Username}}", Username);
            htmlContent = htmlContent.Replace("{{Fullname}}", Fullname);
            htmlContent = htmlContent.Replace("{{TokenLifetime}}", tokenLifetimeString);

            var message = new EmailVM(user.Email, htmlContent, "reset password");
            _mailService.SendEmail(message);

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            var user = _userManager.FindByNameAsync(email);
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordModel)
        {
            if (!ModelState.IsValid)
                return View(resetPasswordModel);
            var user = await _userManager.FindByEmailAsync(resetPasswordModel.Email);
            if (user == null)
                RedirectToAction(nameof(ResetPasswordConfirmation));
            var resetPassResult = await _userManager.ResetPasswordAsync(user, resetPasswordModel.Token, resetPasswordModel.Password);
            if (!resetPassResult.Succeeded)
            {
                foreach (var error in resetPassResult.Errors)
                {
                    ModelState.TryAddModelError(error.Code, error.Description);
                }
                return View();
            }
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return Ok();
        }
        public static string GetUserIP()
        {
            string ip = string.Empty;

            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] addresses = hostEntry.AddressList;

                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ip = address.ToString();
                        break;
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error retrieving IP address: " + ex.Message);
            }

            return ip;
        }
        private static string FormatTokenLifetime(TimeSpan tokenLifetime)
        {
            string formattedLifetime = "";

            if (tokenLifetime.Days > 0)
            {
                formattedLifetime += $"{tokenLifetime.Days} days ";
            }

            if (tokenLifetime.Hours > 0)
            {
                formattedLifetime += $"{tokenLifetime.Hours} hours";
            }

            return formattedLifetime.Trim();
        }

    }
}

