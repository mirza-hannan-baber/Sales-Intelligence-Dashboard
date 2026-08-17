using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesIntelligence.Api.DTOs;
using SalesIntelligence.Api.Services;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PredictionsController : ControllerBase
    {
        private readonly IPredictionService _predictionService;

        public PredictionsController(IPredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        [HttpPost("company-revenue")]
        public async Task<IActionResult> PredictCompanyRevenue([FromBody] CompanyRevenuePredictRequest req)
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
            var result = await _predictionService.PredictCompanyRevenueAsync(req, username);
            return Ok(result);
        }

        [HttpPost("win-rate")]
        public async Task<IActionResult> PredictWinRate([FromBody] WinRatePredictRequest req)
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
            var result = await _predictionService.PredictWinRateAsync(req, username);
            return Ok(result);
        }

        [HttpPost("employee-performance")]
        public async Task<IActionResult> PredictEmployeePerformance([FromBody] EmployeePerformancePredictRequest req)
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
            var result = await _predictionService.PredictEmployeePerformanceAsync(req, username);
            return Ok(result);
        }

        [HttpPost("employee-revenue")]
        public async Task<IActionResult> PredictEmployeeRevenue([FromBody] EmployeeRevenuePredictRequest req)
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
            var result = await _predictionService.PredictEmployeeRevenueAsync(req, username);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _predictionService.GetHistoryAsync();
            return Ok(history);
        }
    }
}
