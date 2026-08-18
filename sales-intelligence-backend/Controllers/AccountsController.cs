using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AccountsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts([FromQuery] string? search, [FromQuery] string? sector)
        {
            var query = _db.Accounts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(s) || a.AccountId.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(sector) && !sector.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Sector == sector);
            }

            var items = await query.OrderBy(a => a.Name).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            var account = await _db.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            var deals = await _db.Deals.Where(d => d.Company == account.Name).ToListAsync();
            return Ok(new { account, deals });
        }
    }
}
