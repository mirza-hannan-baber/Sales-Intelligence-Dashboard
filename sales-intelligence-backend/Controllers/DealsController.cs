using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DealsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DealsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetDeals(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? product,
            [FromQuery] string? owner,
            [FromQuery] string? sector,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _db.Deals.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(s) ||
                                         d.Company.ToLower().Contains(s) ||
                                         d.Owner.ToLower().Contains(s) ||
                                         d.Product.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(product) && !product.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Product == product);
            }

            if (!string.IsNullOrWhiteSpace(owner) && !owner.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Owner == owner);
            }

            if (!string.IsNullOrWhiteSpace(sector) && !sector.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Sector == sector);
            }

            var totalDeals = await query.CountAsync();
            var totalPipeline = totalDeals > 0 ? await query.SumAsync(d => d.Value) : 0;
            var avgProb = totalDeals > 0 ? await query.AverageAsync(d => d.Probability) : 0;

            var deals = await query
                .OrderByDescending(d => d.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var products = await _db.Deals.Select(d => d.Product).Distinct().OrderBy(p => p).ToListAsync();
            var owners = await _db.Deals.Select(d => d.Owner).Distinct().OrderBy(o => o).ToListAsync();
            var sectors = await _db.Deals.Select(d => d.Sector).Distinct().OrderBy(s => s).ToListAsync();

            return Ok(new
            {
                totalDeals,
                totalPipeline = totalPipeline >= 1_000_000m
                    ? $"${totalPipeline / 1_000_000m:F2}M"
                    : $"${totalPipeline / 1_000m:F0}K",
                totalPipelineRaw = totalPipeline,
                avgProbability = $"{avgProb:F1}%",
                page,
                pageSize,
                filterOptions = new { products, owners, sectors },
                items = deals
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDealById(int id)
        {
            var deal = await _db.Deals.FindAsync(id);
            if (deal == null) return NotFound();
            return Ok(deal);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDeal([FromBody] Deal deal)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            deal.OpportunityId = $"OPP_{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
            deal.CreatedDate = DateTime.UtcNow;
            _db.Deals.Add(deal);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDealById), new { id = deal.Id }, deal);
        }
    }
}
