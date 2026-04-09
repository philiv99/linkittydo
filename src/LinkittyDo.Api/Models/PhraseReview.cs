namespace LinkittyDo.Api.Models;

public class PhraseReview
{
    public int Id { get; set; }
    public string PhraseUniqueId { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ReviewNotes { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public GamePhrase? Phrase { get; set; }
}
