namespace TerminalBible.Core.Importing;

public static class BibleBookCatalog
{
    private static readonly IReadOnlyDictionary<string, (int Order, string Name)> Books = new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase)
    {
        ["GEN"] = (1, "Gênesis"),
        ["EXO"] = (2, "Êxodo"),
        ["LEV"] = (3, "Levítico"),
        ["NUM"] = (4, "Números"),
        ["DEU"] = (5, "Deuteronômio"),
        ["JOS"] = (6, "Josué"),
        ["JDG"] = (7, "Juízes"),
        ["RUT"] = (8, "Rute"),
        ["1SA"] = (9, "1 Samuel"),
        ["2SA"] = (10, "2 Samuel"),
        ["1KI"] = (11, "1 Reis"),
        ["2KI"] = (12, "2 Reis"),
        ["1CH"] = (13, "1 Crônicas"),
        ["2CH"] = (14, "2 Crônicas"),
        ["EZR"] = (15, "Esdras"),
        ["NEH"] = (16, "Neemias"),
        ["EST"] = (17, "Ester"),
        ["JOB"] = (18, "Jó"),
        ["PSA"] = (19, "Salmos"),
        ["PRO"] = (20, "Provérbios"),
        ["ECC"] = (21, "Eclesiastes"),
        ["SNG"] = (22, "Cânticos"),
        ["ISA"] = (23, "Isaías"),
        ["JER"] = (24, "Jeremias"),
        ["LAM"] = (25, "Lamentações"),
        ["EZK"] = (26, "Ezequiel"),
        ["DAN"] = (27, "Daniel"),
        ["HOS"] = (28, "Oseias"),
        ["JOL"] = (29, "Joel"),
        ["AMO"] = (30, "Amós"),
        ["OBA"] = (31, "Obadias"),
        ["JON"] = (32, "Jonas"),
        ["MIC"] = (33, "Miqueias"),
        ["NAM"] = (34, "Naum"),
        ["HAB"] = (35, "Habacuque"),
        ["ZEP"] = (36, "Sofonias"),
        ["HAG"] = (37, "Ageu"),
        ["ZEC"] = (38, "Zacarias"),
        ["MAL"] = (39, "Malaquias"),
        ["MAT"] = (40, "Mateus"),
        ["MRK"] = (41, "Marcos"),
        ["LUK"] = (42, "Lucas"),
        ["JHN"] = (43, "João"),
        ["ACT"] = (44, "Atos"),
        ["ROM"] = (45, "Romanos"),
        ["1CO"] = (46, "1 Coríntios"),
        ["2CO"] = (47, "2 Coríntios"),
        ["GAL"] = (48, "Gálatas"),
        ["EPH"] = (49, "Efésios"),
        ["PHP"] = (50, "Filipenses"),
        ["COL"] = (51, "Colossenses"),
        ["1TH"] = (52, "1 Tessalonicenses"),
        ["2TH"] = (53, "2 Tessalonicenses"),
        ["1TI"] = (54, "1 Timóteo"),
        ["2TI"] = (55, "2 Timóteo"),
        ["TIT"] = (56, "Tito"),
        ["PHM"] = (57, "Filemom"),
        ["HEB"] = (58, "Hebreus"),
        ["JAS"] = (59, "Tiago"),
        ["1PE"] = (60, "1 Pedro"),
        ["2PE"] = (61, "2 Pedro"),
        ["1JN"] = (62, "1 João"),
        ["2JN"] = (63, "2 João"),
        ["3JN"] = (64, "3 João"),
        ["JUD"] = (65, "Judas"),
        ["REV"] = (66, "Apocalipse")
    };

    public static int GetOrder(string code, int fallbackOrder)
    {
        return Books.TryGetValue(code, out var book) ? book.Order : fallbackOrder;
    }

    public static string GetName(string code)
    {
        return Books.TryGetValue(code, out var book) ? book.Name : code;
    }
}
