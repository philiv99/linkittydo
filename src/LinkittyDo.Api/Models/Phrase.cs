namespace LinkittyDo.Api.Models;

public class Phrase
{
    public int Id { get; set; }
    public string FullText { get; set; } = string.Empty;
    public List<PhraseWord> Words { get; set; } = new();
}

public class PhraseWord
{
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public string? ClueSearchTerm { get; set; }
}
