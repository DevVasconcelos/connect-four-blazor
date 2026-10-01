using System.Text.Json;
using System.Text.RegularExpressions;
using ConnectFour.Models;
using Microsoft.AspNetCore.Identity;

namespace ConnectFour.Services;

public sealed class UserStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly PasswordHasher<AppUser> _passwordHasher = new();
    private readonly ILogger<UserStore> _logger;

    public UserStore(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<UserStore> logger)
    {
        var dataDirectory = StoragePathResolver.GetDataDirectory(environment, configuration);
        _filePath = Path.Combine(dataDirectory, "users.json");
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, AppUser? User)> CreateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        username = username.Trim();

        if (username.Length is < 3 or > 30)
        {
            return (false, "Username must be between 3 and 30 characters.", null);
        }

        if (!Regex.IsMatch(username, "^[A-Za-z0-9._-]+$"))
        {
            return (false, "Username can only contain letters, numbers, dots, underscores, and hyphens.", null);
        }

        if (password.Length < 8)
        {
            return (false, "Password must contain at least 8 characters.", null);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await ReadUsersAsync(cancellationToken);
            var normalizedUsername = username.ToUpperInvariant();

            if (users.Any(user => user.NormalizedUsername == normalizedUsername))
            {
                return (false, "That username is already in use.", null);
            }

            var newUser = new AppUser
            {
                Username = username,
                NormalizedUsername = normalizedUsername
            };

            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);
            users.Add(newUser);
            await WriteUsersAsync(users, cancellationToken);

            return (true, null, newUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not create user {Username}.", username);
            return (false, "The account could not be created. Please try again.", null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AppUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await ReadUsersAsync(cancellationToken);
            var normalizedUsername = username.ToUpperInvariant();
            var user = users.FirstOrDefault(item => item.NormalizedUsername == normalizedUsername);

            if (user is null)
            {
                return null;
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await WriteUsersAsync(users, cancellationToken);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not validate credentials for {Username}.", username);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<AppUser>> ReadUsersAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<AppUser>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteUsersAsync(List<AppUser> users, CancellationToken cancellationToken)
    {
        var temporaryPath = $"{_filePath}.tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, users, JsonOptions, cancellationToken);
        }

        // Replace the target only after serialization succeeds to reduce the chance of a partial data file.
        File.Move(temporaryPath, _filePath, true);
    }
}
