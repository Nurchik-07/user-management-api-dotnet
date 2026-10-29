using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var users = new Dictionary<int, User>();
var sync = new object();
var nextId = 1;

// Log request method, path, response status and duration without logging user data.
app.Use(async (context, next) =>
{
    var started = System.Diagnostics.Stopwatch.StartNew();
    try { await next(context); }
    finally
    {
        app.Logger.LogInformation("{Method} {Path} returned {Status} in {Elapsed}ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, started.ElapsedMilliseconds);
    }
});

app.MapGet("/users", IResult () =>
{
    lock (sync) return Results.Ok(users.Values.ToArray());
});

app.MapGet("/users/{id:int}", IResult (int id) =>
{
    lock (sync) return users.TryGetValue(id, out var user)
        ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/users", IResult (UserInput input) =>
{
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    lock (sync)
    {
        if (users.Values.Any(u => string.Equals(u.Email, input.Email!.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Email"] = ["Email already exists."] });
        var user = new User(nextId++, input.Name!.Trim(), input.Email!.Trim());
        users.Add(user.Id, user);
        return Results.Created($"/users/{user.Id}", user);
    }
});

app.MapPut("/users/{id:int}", IResult (int id, UserInput input) =>
{
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    lock (sync)
    {
        if (!users.ContainsKey(id)) return Results.NotFound();
        if (users.Values.Any(u => u.Id != id && string.Equals(u.Email, input.Email!.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Email"] = ["Email already exists."] });
        var user = new User(id, input.Name!.Trim(), input.Email!.Trim());
        users[id] = user;
        return Results.Ok(user);
    }
});

app.MapDelete("/users/{id:int}", IResult (int id) =>
{
    lock (sync) return users.Remove(id) ? Results.NoContent() : Results.NotFound();
});

app.Run();

static Dictionary<string, string[]> Validate(UserInput input)
{
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(input, new ValidationContext(input), results, true);
    return results.SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(member => (member, r.ErrorMessage ?? "Invalid value.")))
        .GroupBy(item => item.member)
        .ToDictionary(group => group.Key, group => group.Select(item => item.Item2).ToArray());
}

public record User(int Id, string Name, string Email);
public sealed class UserInput
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public string? Email { get; init; }
}
