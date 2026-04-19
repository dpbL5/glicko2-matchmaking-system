using System.ComponentModel.DataAnnotations;

namespace QueueProcessService.Infrastructure;

public sealed class QueueDatabaseOptions
{
    public const string SectionName = "QueueDatabase";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 3306;

    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string User { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}