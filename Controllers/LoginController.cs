using System.Security.Claims;
using FlightCast.Data;
using FlightCast.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightCast.Controllers;

public class LoginController : Controller
{
    private readonly IUserService _userService;
    public LoginController(IUserService userService)
    {
        _userService = userService;
    }


    public IActionResult Index()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Index(User user)
    {
        var authenticatedUser = _userService.Authenticate(user.Username, user.Password);
        if (authenticatedUser == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(user);
        }
        //Set the authenticated user in cookie

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, authenticatedUser.Username),
            new Claim(ClaimTypes.Role, authenticatedUser.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // This will keep the user logged in across sessions
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // Set the cookie to expire in 1 day
        };
        await HttpContext.SignInAsync(
            "MyCookieAuth",
            new ClaimsPrincipal(claimsIdentity),
            authProperties);



        return RedirectToAction("Index", "Home");
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(User user)
    {
        var plainPassword = user.Password; //Store the plain password before hashing
        _userService.RegisterUser(user);
        var authenticatedUser = _userService.Authenticate(user.Username, plainPassword);
        if (authenticatedUser == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(user);
        }
        //Set the authenticated user in cookie

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, authenticatedUser.Username),
            new Claim(ClaimTypes.Role, authenticatedUser.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // This will keep the user logged in across sessions
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // Set the cookie to expire in 1 day
        };
        await HttpContext.SignInAsync(
            "MyCookieAuth",
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToAction("Index", "Home");
    }


    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Index", "Home");
    }


    // Additional actions for creating, editing, deleting expenses can be added here.
}
