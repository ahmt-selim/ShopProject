using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Authorize(Roles = "admin")]
[ApiController]
[Route("api/[controller]")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public ActionResult<object> GetAll()
    {
        var result = db.Users.OrderBy(u => u.Email).ToList().Select(u => new
        {
            u.Id,
            u.Email,
            u.Name,
            u.Phone,
            u.Role,
            orderCount = db.Orders.Count(o => o.UserId == u.Id),
            totalSpent = db.Orders.Where(o => o.UserId == u.Id).Sum(o => (decimal?)o.Total) ?? 0
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public ActionResult<object> GetById(string id)
    {
        var u = db.Users.Find(id);
        if (u is null) return NotFound();
        var addresses = db.Addresses.Where(a => a.UserId == id).ToList();
        var orders = db.Orders.Where(o => o.UserId == id)
            .OrderByDescending(o => o.Id)
            .ToList()
            .Select(o => new { o.Id, o.OrderNo, o.Total, o.ItemsCount, o.Status, o.CreatedAt });
        return Ok(new
        {
            user = new { u.Id, u.Email, u.Name, u.Phone, u.Role },
            addresses,
            orders
        });
    }
}
