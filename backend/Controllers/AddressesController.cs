using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AddressesController(AppDbContext db) : ControllerBase
{
    private string UserId => ClaimsHelper.UserId(User);

    [HttpGet]
    public ActionResult<IEnumerable<Address>> GetAll() =>
        Ok(db.Addresses.Where(a => a.UserId == UserId).ToList());

    [HttpPost]
    public ActionResult<Address> Create(Address dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Detail) || string.IsNullOrWhiteSpace(dto.City))
            return BadRequest(new { error = "Şehir ve adres detayı zorunlu." });
        dto.Id = 0;
        dto.UserId = UserId;
        db.Addresses.Add(dto);
        db.SaveChanges();
        return Ok(dto);
    }

    [HttpPut("{id:int}")]
    public ActionResult<Address> Update(int id, Address dto)
    {
        var a = db.Addresses.FirstOrDefault(x => x.UserId == UserId && x.Id == id);
        if (a is null) return NotFound();
        a.Title = dto.Title; a.FullName = dto.FullName; a.Phone = dto.Phone;
        a.City = dto.City; a.District = dto.District; a.Detail = dto.Detail;
        db.SaveChanges();
        return Ok(a);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var a = db.Addresses.FirstOrDefault(x => x.UserId == UserId && x.Id == id);
        if (a is null) return NotFound();
        db.Addresses.Remove(a);
        db.SaveChanges();
        return NoContent();
    }
}
