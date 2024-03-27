namespace Raileasy.DataProtection.Gcs;

public record GcsDataProtectionConfiguration
{
    public required string StorageBucket { get; init; }
    public string? ObjectPrefix { get; init; }
}
