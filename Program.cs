using System.Security.Claims;
using ConnectFour.Components;
using ConnectFour.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "ConnectFour.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });
builder.Services.AddAntiforgery();

builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<GameRecordStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapPost("/auth/register", async (
    HttpContext context,
    UserStore users,
    IAntiforgery antiforgery) =>
{
    if (!await IsValidAntiforgeryRequestAsync(context, antiforgery))
    {
        return Results.Redirect("/register?error=" + Uri.EscapeDataString("Your form expired. Please try again."));
    }

    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (password != confirmPassword)
    {
        return Results.Redirect("/register?error=" + Uri.EscapeDataString("Passwords do not match."));
    }

    var result = await users.CreateAsync(username, password, context.RequestAborted);
    if (!result.Success || result.User is null)
    {
        return Results.Redirect("/register?error=" + Uri.EscapeDataString(result.Error ?? "Account creation failed."));
    }

    await SignInAsync(context, result.User.Id, result.User.Username);
    return Results.Redirect("/");
}).AllowAnonymous();

app.MapPost("/auth/login", async (
    HttpContext context,
    UserStore users,
    IAntiforgery antiforgery) =>
{
    if (!await IsValidAntiforgeryRequestAsync(context, antiforgery))
    {
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Your form expired. Please try again."));
    }

    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var user = await users.ValidateCredentialsAsync(username, password, context.RequestAborted);
    if (user is null)
    {
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Invalid username or password."));
    }

    await SignInAsync(context, user.Id, user.Username);
    return Results.Redirect("/");
}).AllowAnonymous();

app.MapPost("/auth/logout", async (
    HttpContext context,
    IAntiforgery antiforgery) =>
{
    if (!await IsValidAntiforgeryRequestAsync(context, antiforgery))
    {
        return Results.BadRequest("Invalid antiforgery token.");
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
}).RequireAuthorization();

app.MapPost("/matches/save-game", async (
    HttpContext context,
    GameRecordStore matches,
    IAntiforgery antiforgery) =>
{
    if (!await IsValidAntiforgeryRequestAsync(context, antiforgery))
    {
        return Results.BadRequest("The save request expired. Return to the game and try again.");
    }

    var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdValue, out var userId))
    {
        return Results.Redirect("/login");
    }

    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    var playerOneName = form["playerOneName"].ToString();
    var playerTwoName = form["playerTwoName"].ToString();
    var outcome = form["outcome"].ToString();

    if (!int.TryParse(form["moveCount"].ToString(), out var moveCount))
    {
        return Results.BadRequest("The match move count is invalid.");
    }

    try
    {
        await matches.AddAsync(new ConnectFour.Models.GameRecord
        {
            UserId = userId,
            PlayerOneName = playerOneName,
            PlayerTwoName = playerTwoName,
            Outcome = outcome,
            MoveCount = moveCount,
            PlayedAtUtc = DateTime.UtcNow
        }, context.RequestAborted);

        return Results.Redirect("/matches");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task SignInAsync(HttpContext context, Guid userId, string username)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Name, username)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var properties = new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
    };

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
}

static async Task<bool> IsValidAntiforgeryRequestAsync(HttpContext context, IAntiforgery antiforgery)
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
        return true;
    }
    catch (AntiforgeryValidationException)
    {
        return false;
    }
}
