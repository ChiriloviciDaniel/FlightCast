using FlightCast.Data;
using FlightCast.Models;
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


    public IActionResult Login()
    {
        return View();
    }


    [HttpPost]
    public IActionResult Login(User user)
    {
        var authenticatedUser=_userService.Authenticate(user.Username, user.Password);
        if (authenticatedUser == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(user);
        }
        return RedirectToAction("Index", "Home");
    }
 

    // Additional actions for creating, editing, deleting expenses can be added here.
}
