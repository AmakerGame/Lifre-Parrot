using System.IO;
using System.Reflection;

namespace Lifre.Persistence;

/// <summary>
/// Текстові ресурси: фрази, підказки, підписи меню, помилки.
/// Усі .txt файли вбудовані в сам exe (EmbeddedResource) — жодних папок і файлів на диску.
/// </summary>
public sealed class TextStore
{
    private List<string> _phrases = new();
    private Dictionary<string, string> _menu = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Поточні фрази папуги.</summary>
    public IReadOnlyList<string> Phrases => _phrases;

    /// <summary>Завантажує тексти, вбудовані в exe.</summary>
    public void Reload()
    {
        _phrases = ReadLines("phrases.txt", new[] { "Карр. Я тут." });
        _menu = ReadMenu("menu.txt");
    }

    /// <summary>Повертає підпис меню за ключем або сам ключ, якщо відсутній.</summary>
    public string MenuLabel(string key) =>
        _menu.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : key;

    private static string? ReadResource(string file)
    {
        try
        {
            var asm = typeof(TextStore).Assembly;
            var name = "Lifre.Texts." + file;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null)
                return null;
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ReadLines(string file, string[] fallback)
    {
        var text = ReadResource(file);
        if (text is not null)
        {
            var lines = text.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith('#'))
                .ToList();
            if (lines.Count > 0)
                return lines;
        }
        return new List<string>(fallback);
    }

    private static Dictionary<string, string> ReadMenu(string file)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = ReadResource(file);
        if (text is null)
            return result;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var idx = line.IndexOf('=');
            if (idx > 0)
                result[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }
        return result;
    }
}
