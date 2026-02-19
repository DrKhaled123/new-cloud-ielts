using Contracts.Domain.Database;

namespace Contracts.Domain.Database.Content;

public class ContentTag : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
