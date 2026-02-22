using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace backend.Controllers
{
    public static class ControllerExtension
    {
        public async static Task<IActionResult> Run(this ControllerBase controller, Func<Task<IActionResult>> function)
        {
            try
            {
                return await function();
            }
            catch (Exception ex)
            {
#if DEBUG
                return controller.BadRequest(new {message = ex.Message});
#else
                return controller.BadRequest(new {message = "Váratlan hiba!"});
#endif
            }
        }
    }
}
