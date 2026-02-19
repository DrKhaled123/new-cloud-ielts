using Contracts.Domain.Database;
using Contracts.Domain.Enums;

namespace Contracts.Domain.Database.Content;

public class ContentItem : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public ContentItemType Type { get; set; }

    /// <summary>
    /// Markdown body.
    /// </summary>
    public string BodyMarkdown { get; set; } = string.Empty;

    public List<string> CategorySlugs { get; set; } = new();
    public List<string> TagSlugs { get; set; } = new();

    public List<Guid> RelatedItemIds { get; set; } = new();

    public int? Difficulty { get; set; }
    public string? EssayType { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
