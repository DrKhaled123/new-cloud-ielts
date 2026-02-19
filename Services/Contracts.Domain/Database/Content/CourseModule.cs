using Contracts.Domain.Database;

namespace Contracts.Domain.Database.Content;

public class CourseModule : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public List<Guid> ContentItemIds { get; set; } = new();
}
