using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Lab.Intelligence;

public sealed record KnowledgeEntry(string Id, string Examples, string Answer);
public sealed record AssistantProduct(int Id, string Name, decimal Price, int Stock);
public sealed record KnowledgeReply(string Topic, string Answer, double Confidence);

public static class KnowledgeAssistant
{
    private static readonly HashSet<string> StopWords = new("a an the is are i my me to of for and in on can how what please toi cua la co cho ve voi gi nay".Split(' '));
    public static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var clean = new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(clean, @"[^a-z0-9., ]", " ");
    }
    private static string[] Tokens(string text) => Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => !StopWords.Contains(x)).ToArray();

    public static KnowledgeReply Ask(string question, IEnumerable<KnowledgeEntry> knowledge)
    {
        var entries = knowledge.ToList();
        var query = Tokens(question);
        const string fallback = "I couldn't find a reliable answer. Try a product name, stock, order status, payment, or printing.";
        if (query.Length == 0 || entries.Count == 0) return new("unknown", fallback, 0);
        var documents = entries.Select(e => Tokens(e.Examples)).ToArray();
        var vocabulary = documents.SelectMany(x => x).Concat(query).Distinct();
        var idf = vocabulary.ToDictionary(word => word, word => 1 + Math.Log((1d + documents.Length) / (1 + documents.Count(d => d.Contains(word)))));
        Dictionary<string,double> Vector(string[] words)
        {
            var values = words.GroupBy(w => w).ToDictionary(g => g.Key, g => (1 + Math.Log(g.Count())) * idf[g.Key]);
            var length = Math.Sqrt(values.Values.Sum(v => v * v));
            return values.ToDictionary(p => p.Key, p => p.Value / Math.Max(length, 1e-9));
        }
        var q = Vector(query); double best = 0; int match = -1;
        for (var i = 0; i < documents.Length; i++)
        {
            var d = Vector(documents[i]); var score = q.Sum(p => p.Value * d.GetValueOrDefault(p.Key));
            if (score > best) { best = score; match = i; }
        }
        return match < 0 || best < .16 ? new("unknown", fallback, best) : new(entries[match].Id, entries[match].Answer, best);
    }

    public static IReadOnlyList<AssistantProduct> Recommend(string question, IEnumerable<AssistantProduct> products)
    {
        var text = Normalize(question);
        var match = Regex.Match(text, @"(?:under|below|duoi|budget|toi da)\s*(\d+(?:[.,]\d+)?)\s*(k|nghin|ngan|trieu|m)?");
        decimal? budget = null;
        if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            budget = value * (match.Groups[2].Value is "k" or "nghin" or "ngan" ? 1000 : match.Groups[2].Value is "m" or "trieu" ? 1000000 : 1);
        var aliases = new Dictionary<string,string> { ["ca phe"]="coffee", ["tra"]="tea", ["hat dieu"]="cashew", ["gao"]="rice", ["dua"]="coconut", ["xoai"]="mango", ["duong"]="sugar", ["muoi"]="salt", ["dau an"]="oil", ["mi"]="noodles" };
        foreach (var alias in aliases) if (Regex.IsMatch(text, @"\b" + Regex.Escape(alias.Key) + @"\b")) text += " " + alias.Value;
        var productWords = new[] { "coffee", "tea", "cashew", "rice", "coconut", "mango", "sugar", "salt", "oil", "noodles", "sauce" }.Where(w => Regex.IsMatch(text, @"\b" + w + @"\b")).ToArray();
        if (budget is null && productWords.Length == 0) return [];
        return products.Where(p => p.Stock > 0 && (!budget.HasValue || p.Price <= budget) &&
            (productWords.Length == 0 || productWords.Any(w => Normalize(p.Name).Contains(w))))
            .OrderBy(p => p.Price).ThenBy(p => p.Id).Take(3).ToList();
    }
}
