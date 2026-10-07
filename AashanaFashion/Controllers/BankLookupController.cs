using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AashanaFashion.Services;

namespace AashanaFashion.Controllers;

[Authorize]
public class BankLookupController : Controller
{
    private readonly IBankLookupService _bankLookupService;

    public BankLookupController(IBankLookupService bankLookupService)
    {
        _bankLookupService = bankLookupService;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("api/bank/lookup-ifsc")]
    public async Task<IActionResult> LookupIfsc(string? ifsc)
    {
        if (string.IsNullOrWhiteSpace(ifsc))
        {
            return Json(new BankDetailsResult
            {
                Success = false,
                Message = "IFSC code is required."
            });
        }

        var result = await _bankLookupService.LookupIfscAsync(ifsc);
        return Json(result);
    }
}
