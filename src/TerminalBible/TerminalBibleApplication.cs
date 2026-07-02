using Spectre.Console;
using TerminalBible.Core.Domain;
using TerminalBible.Core.Importing;
using TerminalBible.Core.Navigation;
using TerminalBible.Core.Reading;
using TerminalBible.Core.Storage;

namespace TerminalBible;

public sealed class TerminalBibleApplication(
    BibleStorage storage,
    BibleInstallService installer)
{
    private const string ReadOption = "Ler Bíblia";
    private const string InstallOption = "Instalar/Atualizar Bíblia";
    private const string AboutOption = "Sobre";
    private const string ExitOption = "Sair";
    private const int ReaderChromeHeight = 4;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.Clear();
        ShowHeader();

        if (!storage.IsInstalled(BibleSource.PortugueseWorldBible.Id))
        {
            await OfferFirstInstallAsync(cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            ShowHeader();
            var option = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Menu principal[/]")
                    .PageSize(8)
                    .AddChoices(ReadOption, InstallOption, AboutOption, ExitOption));

            switch (option)
            {
                case ReadOption:
                    await ReadBibleAsync(cancellationToken);
                    break;
                case InstallOption:
                    await InstallBibleAsync(cancellationToken);
                    break;
                case AboutOption:
                    await ShowAboutAsync(cancellationToken);
                    break;
                case ExitOption:
                    return;
            }
        }
    }

    private async Task OfferFirstInstallAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]Nenhuma Bíblia offline foi encontrada.[/]");
        var shouldInstall = AnsiConsole.Confirm("Deseja baixar a Bíblia Portuguesa Mundial agora?");
        if (shouldInstall)
        {
            await InstallBibleAsync(cancellationToken);
            return;
        }

        AnsiConsole.MarkupLine("[dim]Você poderá instalar pelo menu principal quando quiser.[/]");
        Pause();
    }

    private async Task InstallBibleAsync(CancellationToken cancellationToken)
    {
        ShowHeader();

        try
        {
            var translation = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .StartAsync("Baixando e preparando a Bíblia para uso offline...", _ => installer.InstallAsync(BibleSource.PortugueseWorldBible, cancellationToken));

            AnsiConsole.MarkupLine($"[green]Instalação concluída:[/] {Markup.Escape(translation.Name)}");
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine("[red]Não foi possível baixar a Bíblia.[/]");
            AnsiConsole.MarkupLine(Markup.Escape(ex.Message));
            ExplainExistingCache();
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine("[red]A conexão demorou demais para responder.[/]");
            AnsiConsole.MarkupLine(Markup.Escape(ex.Message));
            ExplainExistingCache();
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine("[red]Não foi possível salvar os arquivos offline.[/]");
            AnsiConsole.MarkupLine(Markup.Escape(ex.Message));
            ExplainExistingCache();
        }
        catch (InvalidDataException ex)
        {
            AnsiConsole.MarkupLine("[red]O pacote baixado não parece ser um ZIP válido.[/]");
            AnsiConsole.MarkupLine(Markup.Escape(ex.Message));
            ExplainExistingCache();
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine("[red]O pacote baixado não pôde ser importado.[/]");
            AnsiConsole.MarkupLine(Markup.Escape(ex.Message));
            ExplainExistingCache();
        }

        Pause();
    }

    private async Task ReadBibleAsync(CancellationToken cancellationToken)
    {
        if (!storage.IsInstalled(BibleSource.PortugueseWorldBible.Id))
        {
            AnsiConsole.MarkupLine("[yellow]A Bíblia ainda não foi instalada para leitura offline.[/]");
            Pause();
            return;
        }

        IReadOnlyList<BibleBook> books;
        try
        {
            books = await storage.LoadBooksAsync(BibleSource.PortugueseWorldBible.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            AnsiConsole.MarkupLine("[red]Os dados offline parecem estar corrompidos.[/]");
            AnsiConsole.MarkupLine("Use [bold]Instalar/Atualizar Bíblia[/] para reinstalar a tradução.");
            Pause();
            return;
        }

        if (books.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Nenhum livro foi encontrado nos dados offline.[/]");
            Pause();
            return;
        }

        var selectedBook = SelectBook(books);
        if (selectedBook is null)
        {
            return;
        }

        var selectedChapter = SelectChapter(selectedBook);
        if (selectedChapter is null)
        {
            return;
        }

        await ShowChapterReaderAsync(books, new ChapterReference(selectedBook.Code, selectedChapter.Number), cancellationToken);
    }

    private static BibleBook? SelectBook(IReadOnlyList<BibleBook> books)
    {
        var back = new BibleBook("__BACK__", "Voltar", int.MaxValue, []);
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<BibleBook>()
                .Title("[bold]Escolha o livro[/]")
                .PageSize(12)
                .UseConverter(book => book.Code == "__BACK__" ? "Voltar" : $"{book.Order:00}. {book.Name}")
                .AddChoices(books.Concat([back])));

        return selected.Code == "__BACK__" ? null : selected;
    }

    private static BibleChapter? SelectChapter(BibleBook book)
    {
        var chapters = book.Chapters
            .OrderBy(chapter => chapter.Number)
            .Select(chapter => new ChapterChoice(chapter, $"Capítulo {chapter.Number}"))
            .Append(new ChapterChoice(null, "Voltar"))
            .ToList();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<ChapterChoice>()
                .Title($"[bold]{Markup.Escape(book.Name)}[/] - escolha o capítulo")
                .PageSize(12)
                .UseConverter(chapter => chapter.Label)
                .AddChoices(chapters));

        return choice.Chapter;
    }

    private static async Task ShowChapterReaderAsync(IReadOnlyList<BibleBook> books, ChapterReference start, CancellationToken cancellationToken)
    {
        var current = start;
        var page = 0;
        var cursorIndex = -1;
        var mode = ReaderNavigationMode.Shortcut;

        while (!cancellationToken.IsCancellationRequested)
        {
            var book = books.FirstOrDefault(book => string.Equals(book.Code, current.BookCode, StringComparison.OrdinalIgnoreCase));
            var chapter = book?.Chapters.FirstOrDefault(chapter => chapter.Number == current.ChapterNumber);
            if (book is null || chapter is null)
            {
                AnsiConsole.MarkupLine("[red]Capítulo não encontrado.[/]");
                Pause();
                return;
            }

            var pages = ReadingPaginator.CreatePages(chapter, GetReaderLayoutOptions());
            var pageCount = pages.Count;
            page = Math.Clamp(page, 0, pageCount - 1);

            var readingPage = pages[page];
            cursorIndex = ClampCursor(readingPage, cursorIndex);
            ShowChapterPage(book, chapter.Number, readingPage, page, pageCount, cursorIndex, mode);

            var previousChapter = BibleNavigator.GetPreviousChapter(books, current);
            var nextChapter = BibleNavigator.GetNextChapter(books, current);
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    cursorIndex = ReadingPaginator.MoveCursor(readingPage, cursorIndex, -1);
                    break;
                case ConsoleKey.RightArrow:
                    cursorIndex = ReadingPaginator.MoveCursor(readingPage, cursorIndex, 1);
                    break;
                case ConsoleKey.UpArrow:
                    cursorIndex = MoveCursorVertically(readingPage, cursorIndex, -1);
                    break;
                case ConsoleKey.DownArrow:
                    cursorIndex = MoveCursorVertically(readingPage, cursorIndex, 1);
                    break;
                case ConsoleKey.B:
                    mode = mode == ReaderNavigationMode.Shortcut ? ReaderNavigationMode.Buttons : ReaderNavigationMode.Shortcut;
                    break;
                case ConsoleKey.L:
                    return;
                case ConsoleKey.Q:
                    AnsiConsole.Clear();
                    Environment.Exit(0);
                    return;
            }

            switch (key.KeyChar)
            {
                case '<' when page > 0:
                    page--;
                    cursorIndex = -1;
                    break;
                case '>' when page + 1 < pageCount:
                    page++;
                    cursorIndex = -1;
                    break;
                case '[' when previousChapter is not null:
                    current = previousChapter;
                    page = 0;
                    cursorIndex = -1;
                    break;
                case ']' when nextChapter is not null:
                    current = nextChapter;
                    page = 0;
                    cursorIndex = -1;
                    break;
            }

            await Task.Yield();
        }
    }

    private static void ShowChapterPage(
        BibleBook book,
        int chapterNumber,
        ReadingPage readingPage,
        int page,
        int pageCount,
        int cursorIndex,
        ReaderNavigationMode mode)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(book.Name)} {chapterNumber}[/] [dim]Página {page + 1}/{pageCount}[/]");
        AnsiConsole.WriteLine();

        if (readingPage.Lines.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]Sem versículos nesta página.[/]");
        }
        else
        {
            var tokenIndex = 0;
            foreach (var line in readingPage.Lines)
            {
                var renderedTokens = line.Select(token => RenderToken(token, tokenIndex++ == cursorIndex));
                AnsiConsole.MarkupLine(string.Join(' ', renderedTokens));
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(mode == ReaderNavigationMode.Shortcut ? BuildShortcutBar() : BuildButtonBar());
    }

    private static ReadingLayoutOptions GetReaderLayoutOptions()
    {
        var width = Math.Max(20, Console.WindowWidth - 4);
        var height = Math.Max(1, Console.WindowHeight - ReaderChromeHeight);
        return new ReadingLayoutOptions(width, height);
    }

    private static string RenderToken(ReadingToken token, bool isCursor)
    {
        var text = Markup.Escape(token.Text);
        if (token.IsVerseNumber)
        {
            return $"[dim]{text}[/]";
        }

        return isCursor ? $"[black on yellow]{text}[/]" : text;
    }

    private static int ClampCursor(ReadingPage page, int cursorIndex)
    {
        if (page.Tokens.Count == 0)
        {
            return -1;
        }

        if (cursorIndex < 0 || cursorIndex >= page.Tokens.Count || page.Tokens[cursorIndex].IsVerseNumber)
        {
            return ReadingPaginator.GetFirstWordIndex(page);
        }

        return cursorIndex;
    }

    private static int MoveCursorVertically(ReadingPage page, int currentIndex, int direction)
    {
        if (currentIndex < 0)
        {
            return ReadingPaginator.GetFirstWordIndex(page);
        }

        if (!TryGetLinePosition(page, currentIndex, out var currentLineIndex, out var tokenOffset))
        {
            return ReadingPaginator.GetFirstWordIndex(page);
        }

        var targetLineIndex = currentLineIndex + direction;
        if (targetLineIndex < 0 || targetLineIndex >= page.Lines.Count)
        {
            return currentIndex;
        }

        var wordColumn = CountWordsBeforeIndex(page.Lines[currentLineIndex], tokenOffset);
        var targetLineStart = GetLineStart(page, targetLineIndex);
        var targetLine = page.Lines[targetLineIndex];
        var seenWords = 0;
        var lastWordIndex = -1;

        for (var index = 0; index < targetLine.Count; index++)
        {
            if (targetLine[index].IsVerseNumber)
            {
                continue;
            }

            lastWordIndex = targetLineStart + index;
            if (seenWords == wordColumn)
            {
                return lastWordIndex;
            }

            seenWords++;
        }

        return lastWordIndex >= 0 ? lastWordIndex : currentIndex;
    }

    private static bool TryGetLinePosition(ReadingPage page, int tokenIndex, out int lineIndex, out int tokenOffset)
    {
        var lineStart = 0;
        for (lineIndex = 0; lineIndex < page.Lines.Count; lineIndex++)
        {
            var lineEnd = lineStart + page.Lines[lineIndex].Count - 1;
            if (tokenIndex >= lineStart && tokenIndex <= lineEnd)
            {
                tokenOffset = tokenIndex - lineStart;
                return true;
            }

            lineStart += page.Lines[lineIndex].Count;
        }

        tokenOffset = -1;
        return false;
    }

    private static int GetLineStart(ReadingPage page, int targetLineIndex)
    {
        var lineStart = 0;
        for (var index = 0; index < targetLineIndex; index++)
        {
            lineStart += page.Lines[index].Count;
        }

        return lineStart;
    }

    private static int CountWordsBeforeIndex(IReadOnlyList<ReadingToken> line, int tokenOffset)
    {
        var count = 0;
        for (var index = 0; index < tokenOffset; index++)
        {
            if (!line[index].IsVerseNumber)
            {
                count++;
            }
        }

        return count;
    }

    private static string BuildShortcutBar()
    {
        return $"[dim]{Markup.Escape("< > páginas  [ ] capítulos  setas frase  b botões  l livro  q sair")}[/]";
    }

    private static string BuildButtonBar()
    {
        return $"[dim]{Markup.Escape("[< Página] [Página >] [[ Capítulo] [Capítulo ]] [b Atalho] [l Livro] [q Sair]")}[/]";
    }

    private async Task ShowAboutAsync(CancellationToken cancellationToken)
    {
        ShowHeader();
        var translation = await storage.LoadTranslationAsync(BibleSource.PortugueseWorldBible.Id, cancellationToken);
        var installedText = translation is null
            ? "Ainda não instalada"
            : $"Instalada em {translation.ImportedAt.ToLocalTime():dd/MM/yyyy HH:mm}";

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[bold]Aplicação[/]", "Terminal Bible");
        grid.AddRow("[bold]Tradução[/]", BibleSource.PortugueseWorldBible.Name);
        grid.AddRow("[bold]Fonte[/]", BibleSource.PortugueseWorldBible.Source);
        grid.AddRow("[bold]Licença[/]", BibleSource.PortugueseWorldBible.License);
        grid.AddRow("[bold]Status[/]", installedText);
        grid.AddRow("[bold]Pasta offline[/]", storage.GetTranslationPath(BibleSource.PortugueseWorldBible.Id));

        AnsiConsole.Write(new Panel(grid).Header("Sobre").Border(BoxBorder.Rounded).Padding(1, 1));
        Pause();
    }

    private static void ExplainExistingCache()
    {
        if (AnsiConsole.Profile.Capabilities.Interactive)
        {
            AnsiConsole.MarkupLine("[dim]Se já havia uma Bíblia instalada, ela foi mantida para uso offline.[/]");
        }
    }

    private static void ShowHeader()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Terminal Bible").Color(Color.Green));
        AnsiConsole.Write(new Rule("[dim]Leitura bíblica offline no terminal[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    private static void Pause()
    {
        AnsiConsole.MarkupLine("[dim]Pressione qualquer tecla para continuar...[/]");
        Console.ReadKey(intercept: true);
    }

    private enum ReaderNavigationMode
    {
        Shortcut,
        Buttons
    }

    private sealed record ChapterChoice(BibleChapter? Chapter, string Label);
}
