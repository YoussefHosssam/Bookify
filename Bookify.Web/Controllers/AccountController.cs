using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Bookify.Services.Interfaces;
using Bookify.Web.Models;

namespace Bookify.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IJwtService jwtService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // If accessing from Identity area, redirect to our custom login
        if (returnUrl != null && returnUrl.Contains("/admin", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["IsAdminLogin"] = true;
        }
        else
        {
            ViewData["ReturnUrl"] = returnUrl;
        }
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromQuery] string? returnUrl = null)
    {
        if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { success = false, message = "Invalid request. Email and password are required." });
        }

        return await HandleLoginAsync(request.Email, request.Password, returnUrl, request.RememberMe);
    }

    private async Task<IActionResult> HandleLoginAsync(string email, string password, string? returnUrl = null, bool rememberMe = false)
    {
        // Try to find user by email or username
        var user = await _userManager.FindByEmailAsync(email) 
            ?? await _userManager.FindByNameAsync(email);
        
        if (user == null)
        {
            return ReturnLoginError("Invalid login attempt.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, password, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in.");
            
            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            
            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Id, user.Email ?? string.Empty, roles);
            
            // Set token in cookie for MVC views
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddDays(1)
            });
            
            // Also set in response header for API calls
            Response.Headers.Append("Authorization", $"Bearer {token}");
            
            // Return JSON response with token
            return Ok(new { 
                success = true, 
                token = token,
                userId = user.Id,
                email = user.Email,
                userName = user.UserName,
                roles = roles,
                returnUrl = returnUrl ?? Url.Content("~/")
            });
        }

        if (result.RequiresTwoFactor)
        {
            return BadRequest(new { success = false, message = "Two-factor authentication required" });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out.");
            return ReturnLoginError("This account has been locked out. Please try again later.");
        }

        return ReturnLoginError("Invalid login attempt.");
    }

    private IActionResult ReturnLoginError(string message)
    {
        return Unauthorized(new { success = false, message = message });
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, [FromQuery] string? returnUrl = null)
    {
        if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.UserName))
        {
            return BadRequest(new { success = false, message = "Invalid request. Username, email, and password are required." });
        }

        return await HandleRegisterAsync(request.UserName, request.Email, request.Password, returnUrl);
    }

    private async Task<IActionResult> HandleRegisterAsync(string userName, string email, string password, string? returnUrl = null)
    {
        var user = new IdentityUser
        {
            UserName = userName,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");

            // Add user to Customer role
            await _userManager.AddToRoleAsync(user, "Customer");

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            
            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Id, user.Email ?? string.Empty, roles);
            
            // Set token in cookie for MVC views
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            });
            
            // Also set in response header for API calls
            Response.Headers.Append("Authorization", $"Bearer {token}");
            
            _logger.LogInformation("User logged in after registration.");

            // Return JSON response with token
            return Ok(new { 
                success = true, 
                token = token,
                userId = user.Id,
                email = user.Email,
                userName = user.UserName,
                roles = roles,
                returnUrl = returnUrl ?? Url.Content("~/")
            });
        }

        var errors = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(new { success = false, message = "Registration failed", errors = errors });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken] // Allow both form and JSON requests
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        
        // Remove JWT token cookie
        Response.Cookies.Delete("jwt_token");
        
        _logger.LogInformation("User logged out.");
        
        // If JSON request, return JSON; otherwise redirect
        if (Request.Headers["Content-Type"].ToString().Contains("application/json") || 
            Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            return Ok(new { success = true, message = "Logged out successfully" });
        }
        
        if (returnUrl != null)
        {
            return LocalRedirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
