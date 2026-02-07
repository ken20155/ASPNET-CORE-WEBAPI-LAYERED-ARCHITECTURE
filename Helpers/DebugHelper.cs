using Microsoft.AspNetCore.Mvc;

namespace WebApiInterviewStatus.Helpers;  

public static class DebugHelper
{
    public static IActionResult dd(object obj)
    {
        return new JsonResult(obj)
        {
            StatusCode = 500
        };
    }
}
