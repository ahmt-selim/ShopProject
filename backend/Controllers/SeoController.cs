using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
public class SeoController(AppDbContext db, IConfiguration cfg) : ControllerBase
{
    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    public IActionResult Sitemap()
    {
        var baseUrl = cfg["Site:BaseUrl"] ?? "https://ihtiyacin3d.com";
        var products = db.Products.AsNoTracking().ToList();
        var categories = db.Categories.AsNoTracking().ToList();
        var sb = new System.Text.StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        sb.Append($"<url><loc>{baseUrl}/</loc><priority>1.0</priority></url>");
        foreach (var c in categories)
            sb.Append($"<url><loc>{baseUrl}/?category={c.Id}</loc><priority>0.8</priority></url>");
        foreach (var p in products)
        {
            var slug = string.IsNullOrWhiteSpace(p.Slug) ? p.Id.ToString() : p.Slug;
            sb.Append($"<url><loc>{baseUrl}/urunler/{slug}</loc><priority>0.9</priority></url>");
        }
        sb.Append("</urlset>");
        return Content(sb.ToString(), "application/xml");
    }

    [HttpGet("/robots.txt")]
    [Produces("text/plain")]
    public IActionResult Robots()
    {
        var baseUrl = cfg["Site:BaseUrl"] ?? "https://ihtiyacin3d.com";
        return Content($"User-agent: *\nAllow: /\nDisallow: /admin/\nDisallow: /account/\nSitemap: {baseUrl}/sitemap.xml\n", "text/plain");
    }
}