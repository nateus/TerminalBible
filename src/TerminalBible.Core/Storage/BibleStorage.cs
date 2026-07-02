using System.Text.Json;
using TerminalBible.Core.Domain;

namespace TerminalBible.Core.Storage;

public sealed class BibleStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootPath;

    public BibleStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public string GetTranslationPath(string translationId)
    {
        return Path.Combine(_rootPath, "bibles", translationId);
    }

    public bool IsInstalled(string translationId)
    {
        var translationPath = GetTranslationPath(translationId);
        return File.Exists(Path.Combine(translationPath, "translation.json"))
            && Directory.Exists(Path.Combine(translationPath, "books"))
            && Directory.EnumerateFiles(Path.Combine(translationPath, "books"), "*.json").Any();
    }

    public async Task SaveAsync(BibleTranslation translation, IReadOnlyList<BibleBook> books, CancellationToken cancellationToken = default)
    {
        var translationPath = GetTranslationPath(translation.Id);
        var parentPath = Path.GetDirectoryName(translationPath) ?? _rootPath;
        var stagingPath = Path.Combine(parentPath, $"{translation.Id}.staging-{Guid.NewGuid():N}");
        var backupPath = Path.Combine(parentPath, $"{translation.Id}.backup-{Guid.NewGuid():N}");
        var booksPath = Path.Combine(stagingPath, "books");
        Directory.CreateDirectory(parentPath);
        Directory.CreateDirectory(booksPath);

        await WriteJsonAsync(Path.Combine(stagingPath, "translation.json"), translation, cancellationToken);

        foreach (var book in books.OrderBy(book => book.Order))
        {
            var fileName = $"{book.Order:00}-{book.Code}.json";
            await WriteJsonAsync(Path.Combine(booksPath, fileName), book, cancellationToken);
        }

        try
        {
            if (Directory.Exists(translationPath))
            {
                Directory.Move(translationPath, backupPath);
            }

            Directory.Move(stagingPath, translationPath);
            TryDeleteDirectory(backupPath);
        }
        catch
        {
            if (!Directory.Exists(translationPath) && Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, translationPath);
            }

            throw;
        }
        finally
        {
            TryDeleteDirectory(stagingPath);
        }
    }

    public async Task<BibleTranslation?> LoadTranslationAsync(string translationId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(GetTranslationPath(translationId), "translation.json");
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<BibleTranslation>(stream, JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<BibleBook>> LoadBooksAsync(string translationId, CancellationToken cancellationToken = default)
    {
        var booksPath = Path.Combine(GetTranslationPath(translationId), "books");
        if (!Directory.Exists(booksPath))
        {
            return [];
        }

        var books = new List<BibleBook>();
        foreach (var file in Directory.EnumerateFiles(booksPath, "*.json").OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
        {
            var book = await LoadBookFileAsync(file, cancellationToken);
            if (book is not null)
            {
                books.Add(book);
            }
        }

        return books.OrderBy(book => book.Order).ToList();
    }

    public async Task<BibleBook?> LoadBookAsync(string translationId, string bookCode, CancellationToken cancellationToken = default)
    {
        var booksPath = Path.Combine(GetTranslationPath(translationId), "books");
        if (!Directory.Exists(booksPath))
        {
            return null;
        }

        var file = Directory.EnumerateFiles(booksPath, $"*-{bookCode}.json").FirstOrDefault();
        return file is null ? null : await LoadBookFileAsync(file, cancellationToken);
    }

    private static async Task<BibleBook?> LoadBookFileAsync(string file, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<BibleBook>(stream, JsonOptions, cancellationToken);
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
