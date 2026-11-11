namespace Backend.Utils;

public static class SlugHelper
{
    public static string Slugify(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToLowerInvariant();
        // Turkish chars
        s = s.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
             .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        // remove non-alphanumeric
        var chars = s.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray();
        s = new string(chars);
        s = string.Join("-", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return s;
    }
}