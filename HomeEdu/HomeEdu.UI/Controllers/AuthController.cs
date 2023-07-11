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
            if (!ModelState.IsValid)
            {
                return View(newUser);
            }
            AppUser user = new AppUser
            {
                UserName = newUser.UserName,
                Email = newUser.Email,
                FullName = string.Concat(newUser.Name, " ", newUser.LastName),
                EmailConfirmed = false
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

            await _userManager.AddToRoleAsync(user, AppUserRole.Member);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
            var message = new EmailVM(user.Email, $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.", "Confirm your email");
            _mailService.SendEmail(message);
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Login()
        {
            return View();

        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Login(LoginVM user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            AppUser? appUser = null;
            if (!string.IsNullOrEmpty(user.LoginIdentifier))
            {
                appUser = await _userManager.FindByEmailAsync(user.LoginIdentifier);

                if (appUser == null)
                {
                    appUser = await _userManager.FindByNameAsync(user.LoginIdentifier);
                }
            }

            if (appUser == null)
            {
                ModelState.AddModelError("", "Invalid login!");
                return View(user);
            }

            var signInResult = await _singInManager.PasswordSignInAsync(appUser, user.Password, user.RememberMe, true);

            if (signInResult.IsLockedOut)
            {
                ModelState.AddModelError("", "Your account is locked. Please try again later.");
                return View(user);
            }

            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login!");
                return View(user);
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
        }
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
            var ResetPasswordUrl = $"<a Class=\"btn btn-danger\" href=\"{Url.Action(nameof(ResetPassword), "Auth", new { token, email = user.Email }, Request.Scheme)}\">reset your password</a>";

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
                    ModelState.AddModelError("", error.Description);
                }

                return View();
            }
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
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

