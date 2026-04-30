using UUIDNext;

namespace HospitalShared;

/// <summary>
/// Generates UUID v7 (time-sortable) for use as database primary keys.
/// Improves index performance by reducing B-tree fragmentation vs random UUIDs.
/// </summary>
public static class GuidV7
{
    public static Guid NewGuid() => Uuid.NewDatabaseFriendly(Database.PostgreSql);
}
