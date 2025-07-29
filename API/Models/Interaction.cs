namespace API.Models;

public record Interaction(int UserId, int SeenUserId, bool Liked);