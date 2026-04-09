namespace LinkittyDo.Api.Models;

public class PhraseCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PhraseCategoryAssignment
{
    public string PhraseUniqueId { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    public GamePhrase? Phrase { get; set; }
    public PhraseCategory? Category { get; set; }
}
