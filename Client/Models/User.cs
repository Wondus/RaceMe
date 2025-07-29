using System.Text.Json.Serialization;

namespace Client.Models;

public class User
{
    public int isAdmin { get; init; }
    public int Id { get; set; }   
    public string? Username { get; set; }
    public byte[]? ImageBytes { get; set; }

    [JsonPropertyName("bio")]
    public string Bio { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}