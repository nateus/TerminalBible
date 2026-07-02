using Spectre.Console;
using TerminalBible.Core.Domain;
using TerminalBible.Core.Importing;
using TerminalBible.Core.Navigation;
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
    private const int VersesPerPage = 18;

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

            var pageCount = Math.Max(1, (int)Math.Ceiling(chapter.Verses.Count / (double)VersesPerPage));
            page = Math.Clamp(page, 0, pageCount - 1);

            ShowChapterPage(book, chapter, page, pageCount);

            var choices = BuildReaderChoices(page, pageCount, BibleNavigator.GetPreviousChapter(books, current), BibleNavigator.GetNextChapter(books, current));
            var option = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Navegação[/]")
                    .AddChoices(choices));

            switch (option)
            {
                case "Página anterior":
                    page--;
                    break;
                case "Próxima página":
                    page++;
                    break;
                case "Capítulo anterior":
                    current = BibleNavigator.GetPreviousChapter(books, current)!;
                    page = 0;
                    break;
                case "Próximo capítulo":
                    current = BibleNavigator.GetNextChapter(books, current)!;
                    page = 0;
                    break;
                case "Escolher outro livro":
                    return;
                case "Sair":
                    AnsiConsole.Clear();
                    Environment.Exit(0);
                    return;
            }

            await Task.Yield();
        }
    }

    private static IReadOnlyList<string> BuildReaderChoices(int page, int pageCount, ChapterReference? previousChapter, ChapterReference? nextChapter)
    {
        var choices = new List<string>();
        if (page > 0)
        {
            choices.Add("Página anterior");
        }

        if (page + 1 < pageCount)
        {
            choices.Add("Próxima página");
        }

        if (previousChapter is not null)
        {
            choices.Add("Capítulo anterior");
        }

        if (nextChapter is not null)
        {
            choices.Add("Próximo capítulo");
        }

        choices.Add("Escolher outro livro");
        choices.Add("Sair");
        return choices;
    }

    private static void ShowChapterPage(BibleBook book, BibleChapter chapter, int page, int pageCount)
    {
        ShowHeader();

        var verses = chapter.Verses
            .Skip(page * VersesPerPage)
            .Take(VersesPerPage)
            .ToList();

        var content = string.Join(
            Environment.NewLine + Environment.NewLine,
            verses.Select(verse => $"[bold dim]{verse.Number}[/] {Markup.Escape(verse.Text)}"));

        var panel = new Panel(new Markup(content.Length == 0 ? "[dim]Sem versículos nesta página.[/]" : content))
            .Header($"{Markup.Escape(book.Name)} {chapter.Number}")
            .Border(BoxBorder.Rounded)
            .Padding(1, 1);

        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine($"[dim]Página {page + 1} de {pageCount}[/]");
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

    private sealed record ChapterChoice(BibleChapter? Chapter, string Label);
}
