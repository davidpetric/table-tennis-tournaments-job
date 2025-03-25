namespace TenisDeMasa.Worker.Data.Tournaments;

public class Tournament : IEquatable<Tournament>
{
    public string Title { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? Date { get; set; }
    public string? Time { get; set; }
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? Author { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? Replies { get; set; }
    public string? LastPostBy { get; set; }
    public DateTime LastPostDate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return $"{Title} - {Category} - {Url}";
    }

    public bool Equals(Tournament? other)
    {
        return other is not null && Id == other.Id &&
               string.Equals(Title, other.Title) &&
               string.Equals(Category, other.Category) &&
               string.Equals(Location, other.Location) &&
               string.Equals(Date, other.Date) &&
               string.Equals(Time, other.Time) &&
               string.Equals(Url, other.Url) &&
               string.Equals(Author, other.Author) &&
               CreatedDate == other.CreatedDate &&
               string.Equals(LastPostBy, other.Author);
    }

    public override bool Equals(object? obj) => Equals(obj as Tournament);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Id);
        hash.Add(Title);
        hash.Add(Category);
        hash.Add(Location);
        hash.Add(Date);
        hash.Add(Time);
        hash.Add(Url);
        hash.Add(Author);
        hash.Add(CreatedDate);
        hash.Add(LastPostBy);
        return hash.ToHashCode();
    }

    public static bool operator ==(Tournament? left, Tournament? right)
    {
        return ReferenceEquals(left, right)
            || (left is not null && right is not null && left.Equals(right));
    }

    public static bool operator !=(Tournament? left, Tournament? right)
    {
        return !(left == right);
    }

    public static Dictionary<string, (object? Original, object? Updated)> GetChanges(Tournament original, Tournament updated)
    {
        var changes = new Dictionary<string, (object? Original, object? Updated)>();

        if (original is null || updated is null)
            return changes;

        if (original.Id != updated.Id)
            changes.Add(nameof(Id), (original.Id, updated.Id));

        if (!string.Equals(original.Title, updated.Title))
            changes.Add(nameof(Title), (original.Title, updated.Title));

        if (!string.Equals(original.Category, updated.Category))
            changes.Add(nameof(Category), (original.Category, updated.Category));

        if (!string.Equals(original.Location, updated.Location))
            changes.Add(nameof(Location), (original.Location, updated.Location));

        if (!string.Equals(original.Date, updated.Date))
            changes.Add(nameof(Date), (original.Date, updated.Date));

        if (!string.Equals(original.Time, updated.Time))
            changes.Add(nameof(Time), (original.Time, updated.Time));

        if (!string.Equals(original.Url, updated.Url))
            changes.Add(nameof(Url), (original.Url, updated.Url));

        if (!string.Equals(original.Author, updated.Author))
            changes.Add(nameof(Author), (original.Author, updated.Author));

        if (original.CreatedDate != updated.CreatedDate)
            changes.Add(nameof(CreatedDate), (original.CreatedDate, updated.CreatedDate));

        if (!string.Equals(original.LastPostBy, updated.LastPostBy))
            changes.Add(nameof(LastPostBy), (original.LastPostBy, updated.LastPostBy));

        if (original.LastPostDate != updated.LastPostDate)
            changes.Add(nameof(LastPostDate), (original.LastPostDate, updated.LastPostDate));

        return changes;
    }
}
