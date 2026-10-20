using System.Text.Json.Serialization;

namespace MSLX.Plugin.MapRender.Models;

public class InstanceMapConfig
{
    [JsonPropertyName("serverId")]
    public uint ServerId { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("allowedDimensions")]
    public List<string> AllowedDimensions { get; set; } = ["overworld", "the_nether", "the_end"];

    [JsonPropertyName("passwordEnabled")]
    public bool PasswordEnabled { get; set; } = false;

    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;
}

public class InstanceMapConfigDto
{
    [JsonPropertyName("serverId")]
    public uint ServerId { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("allowedDimensions")]
    public List<string> AllowedDimensions { get; set; } = [];

    [JsonPropertyName("passwordEnabled")]
    public bool PasswordEnabled { get; set; }

    [JsonPropertyName("hasPassword")]
    public bool HasPassword { get; set; }

    [JsonPropertyName("newPasswordHash")]
    public string? NewPasswordHash { get; set; }
}

public class PublicMapInfoDto
{
    [JsonPropertyName("serverId")]
    public uint ServerId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("allowedDimensions")]
    public List<string> AllowedDimensions { get; set; } = [];

    [JsonPropertyName("requirePassword")]
    public bool RequirePassword { get; set; }

    [JsonPropertyName("hasAccess")]
    public bool HasAccess { get; set; }
}

public class PublicAuthRequest
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("serverId")]
    public uint? ServerId { get; set; }

    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;
}
