public class Note
{
    public Note() { }
    public Note(double x , double y)
    {
        Id = Guid.NewGuid();
        LastEdited = DateTimeOffset.UtcNow;
        X = x;
        Y = y;
    }

    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string? LastLockingUser { get; set; }
    public DateTimeOffset LastEdited { get; set; }
}