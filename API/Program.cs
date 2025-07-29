using API;
using API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
Database db = new Database();

/// Endpoint for a login request
/// returns:
///     the logged in user when ok
///     403 otherwise
app.MapPost("/login", (LoginRequest request) =>
    {
        var user = db.GetUser(request);

        if (user != null)
        {
            return Results.Json(user);
        }

        return Results.StatusCode(403);
    })
    .WithName("PostLogin");

/// Endpoint for a register request
/// returns:
///     200 when ok
///     402 when username or email is taken
///     403 when something else fails
app.MapPost("/register", async (RegisterRequest request) =>
    {
        if (db.Exists(request.Username))
        {
            return Results.StatusCode(402);
        }
        if (db.Create(request))
        {
            return Results.StatusCode(200);
        }

        return Results.StatusCode(403);
    })
    .WithName("PostRegister");

/// Endpoint for an update photo request
/// returns:
///     200 when ok
///     402 when a valid photo hasn't been provided
///     400 when something else fails
///     403 when a valid user ID is missing
app.MapPut("/update/photo", async (HttpRequest req) =>
    {
        if (!int.TryParse(req.Query["userId"], out var userId))
        {
            return Results.StatusCode(403);
        }
        var form = await req.ReadFormAsync();
        var file = form.Files.GetFile("photoFile");
        if (file is null)
        {
            return Results.StatusCode(402);
        }
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var photo = ms.ToArray();

        if (db.UpdateProfilePhoto(userId, photo))
        {
            return Results.StatusCode(200);
        }
        else
        {
            return Results.StatusCode(400);
        }
    })
    .WithName("PutUpdateProfilePhoto");

/// Endpoint for an update username request
/// returns:
///     200 when ok
///     402 when a valid request for a new username hasn't been provided
///     400 when something else fails
///     403 when a valid user ID is missing
app.MapPut("/update/username", async (HttpRequest req) =>
    {
        if (!int.TryParse(req.Query["userId"], out var userId))
        {
            return Results.StatusCode(403);
        }

        var changeRequest = await req.ReadFromJsonAsync<ChangeUsernameRequest>();
        
        if (changeRequest is null)
        {
            return Results.StatusCode(402);
        }
        
        if (db.UpdateUsername(userId, changeRequest.newUsername))
        {
            return Results.StatusCode(200);
        }
        return Results.StatusCode(400);
    })
    .WithName("PutUpdateUsername");

/// Endpoint for an update password request
/// returns:
///     200 when ok
///     402 when a valid request for a new password hasn't been provided
///     400 when something else fails
///     403 when a valid user ID is missing
app.MapPut("/update/password", async (HttpRequest req) =>
    {
        if (!int.TryParse(req.Query["userId"], out var userId))
        {
            return Results.StatusCode(403);
        }

        var changeRequest = await req.ReadFromJsonAsync<ChangePasswordRequest>();
        if (changeRequest is null)
        {
            return Results.StatusCode(402);
        }
        
        if (db.UpdatePassword(userId, changeRequest.newPassword))
        {
            return Results.StatusCode(200);
        }
        return Results.StatusCode(400);
    })
    .WithName("PutUpdatePassword");

/// Endpoint for an update bio request
/// returns:
///     200 when ok
///     402 when a valid request for a new bio hasn't been provided
///     400 when something else fails
///     403 when a valid user ID is missing
app.MapPut("/update/bio", async (HttpRequest req) =>
    {
        if (!int.TryParse(req.Query["userId"], out var userId))
        {
            return Results.StatusCode(403);
        }

        var changeRequest = await req.ReadFromJsonAsync<ChangeBioRequest>();
        
        if (changeRequest is null)
        {
            return Results.StatusCode(402);
        }
        
        if (db.UpdateBio(userId, changeRequest.newBio))
        {
            return Results.StatusCode(200);
        }
        return Results.StatusCode(400);
    })
    .WithName("PutUpdateBio");

/// Endpoint for a load feed request
/// returns:
///     json of unseen users when ok
///     403 when a valid user ID is missing
app.MapGet("/feed", (HttpRequest req) =>
{
    if (!int.TryParse(req.Query["userId"], out var userId))
    {
        return Results.StatusCode(403);
    }
    var unseenUsers = db.GetUnseenUsers(userId);
    return Results.Json(unseenUsers);
}).WithName("GetFeed");

/// Endpoint for an interact request (like or dislike another user)
/// returns:
///     json containing a bool describing whether a match has happened when ok
///     403 when an invalid request was sent
app.MapPost("/feed/interact", async (HttpRequest req) =>
{
    var match = await req.ReadFromJsonAsync<Interaction>();
    if (match is null)
    {
        return Results.StatusCode(403);
    }

    db.MarkUserSeen(match.UserId, match.SeenUserId, match.Liked);
    var isMutual = match.Liked && db.IsMutualMatch(match.UserId, match.SeenUserId);
    return Results.Json(new { matched = isMutual });
}).WithName("PostInteract");

/// Endpoint to get all of the matches for the current user
/// returns:
///     json containing all of matched users when ok
app.MapGet("/matches", (int userId) =>
    {
        var matches = db.GetMatches(userId);
        return Results.Json(matches);
    })
    .WithName("GetMatches");

/// Endpoint to report a user by their ID
/// returns:
///     200 when report was successfully recorded (or was already reported)
///     403 when the reported user ID is missing or invalid
app.MapPost("/feed/report", async (HttpRequest req) =>
    {
        var report = await req.ReadFromJsonAsync<User>();
        if (report is null) return Results.StatusCode(403);

        var success = db.ReportUser(report);
        return success ? Results.StatusCode(200) : Results.StatusCode(403);
    })
    .WithName("PostFeedReport");

/// Endpoint to get users that need moderation
/// returns:
///     json list of reported users
app.MapGet("/admin/users-to-moderate", () =>
{
    var reportedUsers = db.GetReportedUsers();
    return Results.Json(reportedUsers);
}).WithName("GetUsersToModerate");

/// Endpoint to ban reported users
/// returns:
///     OK/NOK
/// TODO only doable by admin
app.MapDelete("/admin/ban/{userId:int}", (int userId) =>
    {
        var success = db.DeleteUser(userId);
        return success
            ? Results.Ok($"User {userId} and related data deleted.")
            : Results.NotFound($"User {userId} not found.");
    })
    .WithName("DeleteBanUser");

/// Endpoint to unreport users
/// returns:
///     OK/NOK
/// TODO only doable by admin
app.MapDelete("/admin/unban/{userId:int}", (int userId) =>
    {
        var success = db.RemoveReportedUser(userId);
        return success
            ? Results.Ok($"User {userId} removed from reported_users.")
            : Results.NotFound($"User {userId} not in reported_users.");
    })
    .WithName("DeleteUnbanUser");


app.Run();
