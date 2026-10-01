using System.Text.Json;
using ConnectFour.Models;

namespace ConnectFour.Services;

public sealed class GameRecordStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<GameRecordStore> _logger;

    public GameRecordStore(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<GameRecordStore> logger)
    {
        var dataDirectory = StoragePathResolver.GetDataDirectory(environment, configuration);
        _filePath = Path.Combine(dataDirectory, "matches.json");
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameRecord>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadRecordsAsync(cancellationToken);
            return records
                .Where(record => record.UserId == userId)
                .OrderByDescending(record => record.PlayedAtUtc)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read match history for user {UserId}.", userId);
            throw new InvalidOperationException("Match history could not be loaded.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<GameRecord?> GetByIdAsync(
        Guid userId,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadRecordsAsync(cancellationToken);
            return records.FirstOrDefault(record => record.UserId == userId && record.Id == recordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read match {RecordId} for user {UserId}.", recordId, userId);
            throw new InvalidOperationException("The match could not be loaded.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<GameRecord> AddAsync(
        GameRecord record,
        CancellationToken cancellationToken = default)
    {
        Normalize(record);
        Validate(record);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadRecordsAsync(cancellationToken);
            record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
            record.PlayedAtUtc = record.PlayedAtUtc == default ? DateTime.UtcNow : record.PlayedAtUtc;
            records.Add(record);
            await WriteRecordsAsync(records, cancellationToken);
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not add a match for user {UserId}.", record.UserId);
            throw new InvalidOperationException("The match could not be saved.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> UpdateAsync(
        GameRecord record,
        CancellationToken cancellationToken = default)
    {
        Normalize(record);
        Validate(record);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadRecordsAsync(cancellationToken);
            var index = records.FindIndex(item => item.Id == record.Id && item.UserId == record.UserId);
            if (index < 0)
            {
                return false;
            }

            record.PlayedAtUtc = records[index].PlayedAtUtc;
            records[index] = record;
            await WriteRecordsAsync(records, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update match {RecordId}.", record.Id);
            throw new InvalidOperationException("The match could not be updated.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadRecordsAsync(cancellationToken);
            var removed = records.RemoveAll(record => record.Id == recordId && record.UserId == userId) > 0;

            if (removed)
            {
                await WriteRecordsAsync(records, cancellationToken);
            }

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not delete match {RecordId}.", recordId);
            throw new InvalidOperationException("The match could not be deleted.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<GameRecord>> ReadRecordsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<GameRecord>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteRecordsAsync(List<GameRecord> records, CancellationToken cancellationToken)
    {
        var temporaryPath = $"{_filePath}.tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, records, JsonOptions, cancellationToken);
        }

        // Replace the target only after serialization succeeds to reduce the chance of a partial data file.
        File.Move(temporaryPath, _filePath, true);
    }

    private static void Normalize(GameRecord record)
    {
        record.PlayerOneName = record.PlayerOneName.Trim();
        record.PlayerTwoName = record.PlayerTwoName.Trim();
        record.Outcome = record.Outcome.Trim();
        record.Notes = string.IsNullOrWhiteSpace(record.Notes) ? null : record.Notes.Trim();
    }

    private static void Validate(GameRecord record)
    {
        if (record.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("A match must belong to a signed-in user.");
        }

        if (string.IsNullOrWhiteSpace(record.PlayerOneName) || string.IsNullOrWhiteSpace(record.PlayerTwoName))
        {
            throw new InvalidOperationException("Both player names are required.");
        }

        if (record.MoveCount is < 1 or > 42)
        {
            throw new InvalidOperationException("Move count must be between 1 and 42.");
        }

        var validOutcome = record.Outcome.Equals("Draw", StringComparison.OrdinalIgnoreCase)
            || record.Outcome.Equals(record.PlayerOneName, StringComparison.Ordinal)
            || record.Outcome.Equals(record.PlayerTwoName, StringComparison.Ordinal);

        if (!validOutcome)
        {
            throw new InvalidOperationException("Outcome must be one of the players or Draw.");
        }
    }
}
