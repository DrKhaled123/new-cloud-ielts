using Contracts.Domain.Database;

namespace Contracts.Domain.Database.Content;

public class ContentCategory : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
