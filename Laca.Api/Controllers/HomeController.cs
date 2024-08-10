using Microsoft.AspNetCore.Mvc;

namespace Laca.Api.Controllers;

[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public string Index()
    {
        return "Hello World";
    }
}