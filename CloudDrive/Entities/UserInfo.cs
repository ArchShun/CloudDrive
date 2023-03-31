namespace CloudDrive.Entities;

public record UserInfo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
}
