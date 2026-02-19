using Contracts.Domain.Database;

namespace Contracts.Domain.Database.Content;

public class UserContentProgress : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public HashSet<Guid> CompletedItemIds { get; set; } = new();
    public HashSet<Guid> BookmarkedItemIds { get; set; } = new();

    /// <summary>
    /// Most-recent-first list.
    /// </summary>
    public List<Guid> RecentItemIds { get; set; } = new();

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
