using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            // Redirect ShopOwner to their dashboard
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "ShopOwner" || role == "2")
            {
                return RedirectToAction("Dashboard", "ShopOwner");
            }
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        // Double-check: if ShopOwner somehow reaches Admin dashboard, redirect them
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "ShopOwner" || role == "2")
        {
            return RedirectToAction("Dashboard", "ShopOwner");
        }
        return View();
    }
}
