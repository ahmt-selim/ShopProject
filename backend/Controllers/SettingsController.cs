using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet("public")]
    public ActionResult<object> Public()
    {
        var map = db.Settings.ToDictionary(s => s.Key, s => s.Value);
        string Get(string k, string fallback = "") =>
            map.TryGetValue(k, out var v) ? v : fallback;
        return Ok(new
        {
            storeName = Get("storeName", "MiniShop"),
            announcement = Get("announcement"),
            currency = Get("currency", "₺")
        });
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public ActionResult<object> GetAll() =>
        Ok(db.Settings.OrderBy(s => s.Key).ToList()
            .Select(s => new { s.Key, s.Value }));

    [Authorize(Roles = "admin")]
    [HttpPut]
    public ActionResult<object> SaveAll(Dictionary<string, string> values)
    {
        foreach (var kv in values)
        {
            var s = db.Settings.Find(kv.Key);
            if (s is null) db.Settings.Add(new Setting { Key = kv.Key, Value = kv.Value ?? "" });
            else s.Value = kv.Value ?? "";
        }
        db.SaveChanges();
        return Ok();
    }
}
