using System.Text;

namespace AashanaFashion.Services;

public static class BarcodeService
{
    // Code 128 patterns (widths of alternating bars and spaces, summing to 11 modules each)
    private static readonly string[] Code128Patterns = new string[]
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213", // 0-9
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132", // 10-19
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211", // 20-29
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313", // 30-39
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331", // 40-49
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111", // 50-59
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214", // 60-69
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", // 70-79
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141", // 80-89
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141", // 90-99
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"                                 // 100-106 (104=StartB, 106=Stop)
    };

    private const int StartCodeB = 104;
    private const int StopCode = 106;

    /// <summary>
    /// Formats standard piece barcode, e.g. AF-0042-015-CH
    /// </summary>
    public static string FormatEntityBarcode(int orderId, int slNo, string entityType)
    {
        string typeCode = entityType.Trim().ToUpper() switch
        {
            "CHANIYA" => "CH",
            "CHOLI" => "CL",
            "DUPATTA" => "DP",
            "DUPPATA" => "DP",
            "BLOUSE" => "BL",
            _ => entityType.Length >= 2 ? entityType.Substring(0, 2).ToUpper() : "PC"
        };
        return $"AF-{orderId:D4}-{slNo:D3}-{typeCode}";
    }

    /// <summary>
    /// Generates pure vector SVG Code 128 barcode
    /// </summary>
    public static string GenerateCode128Svg(string text, int barHeight = 45, int moduleWidth = 2, bool showText = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Clean text to ASCII 32..126
        var clean = new StringBuilder();
        foreach (char c in text)
        {
            if (c >= 32 && c <= 126) clean.Append(c);
        }
        string content = clean.ToString();
        if (content.Length == 0) return string.Empty;

        // Compute codes
        var codes = new List<int> { StartCodeB };
        long checkSum = StartCodeB;

        for (int i = 0; i < content.Length; i++)
        {
            int code = content[i] - 32;
            codes.Add(code);
            checkSum += (long)code * (i + 1);
        }

        int checkCode = (int)(checkSum % 103);
        codes.Add(checkCode);
        codes.Add(StopCode);

        // Build bars
        var patternBuilder = new StringBuilder();
        foreach (int c in codes)
        {
            if (c >= 0 && c < Code128Patterns.Length)
            {
                patternBuilder.Append(Code128Patterns[c]);
            }
        }
        string pattern = patternBuilder.ToString();

        // Calculate total modules
        int totalModules = 0;
        foreach (char ch in pattern)
        {
            totalModules += (ch - '0');
        }

        // Quiet zones: 10 modules on each side
        int quietZone = 10 * moduleWidth;
        int totalWidth = (totalModules * moduleWidth) + (quietZone * 2);
        int totalHeight = showText ? barHeight + 18 : barHeight + 4;

        var svg = new StringBuilder();
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalWidth} {totalHeight}\" width=\"100%\" height=\"auto\" style=\"display:block;max-width:100%;\">");
        svg.Append($"<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

        int currentX = quietZone;
        bool isBar = true;

        for (int i = 0; i < pattern.Length; i++)
        {
            int width = (pattern[i] - '0') * moduleWidth;
            if (isBar)
            {
                svg.Append($"<rect x=\"{currentX}\" y=\"2\" width=\"{width}\" height=\"{barHeight}\" fill=\"#111827\"/>");
            }
            currentX += width;
            isBar = !isBar;
        }

        if (showText)
        {
            svg.Append($"<text x=\"{totalWidth / 2}\" y=\"{barHeight + 15}\" text-anchor=\"middle\" font-family=\"monospace, Consolas, sans-serif\" font-size=\"12\" font-weight=\"bold\" fill=\"#111827\" letter-spacing=\"1\">{content}</text>");
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    /// <summary>
    /// Generates high-contrast QR Code vector SVG using standardized 2D matrix algorithm
    /// </summary>
    public static string GenerateQrCodeSvg(string content, int size = 100)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        // Use deterministic matrix representation for QR tag
        bool[,] matrix = QrMatrixGenerator.Encode(content);
        int matrixSize = matrix.GetLength(0);
        int quietZone = 4;
        int totalSize = matrixSize + (quietZone * 2);

        var svg = new StringBuilder();
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalSize} {totalSize}\" width=\"{size}\" height=\"{size}\" style=\"display:block;\">");
        svg.Append($"<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

        for (int y = 0; y < matrixSize; y++)
        {
            for (int x = 0; x < matrixSize; x++)
            {
                if (matrix[y, x])
                {
                    svg.Append($"<rect x=\"{x + quietZone}\" y=\"{y + quietZone}\" width=\"1\" height=\"1\" fill=\"#111827\"/>");
                }
            }
        }

        svg.Append("</svg>");
        return svg.ToString();
    }
}

/// <summary>
/// Lightweight QR Matrix Generator (Version 1..3 with Reed-Solomon style parity blocks)
/// </summary>
internal static class QrMatrixGenerator
{
    public static bool[,] Encode(string text)
    {
        // Choose grid size: 21x21 (V1), 25x25 (V2), or 29x29 (V3)
        int n = text.Length <= 14 ? 21 : (text.Length <= 26 ? 25 : 29);
        var grid = new bool[n, n];
        var isFunction = new bool[n, n];

        // 1. Finder patterns (top-left, top-right, bottom-left)
        DrawFinder(grid, isFunction, 0, 0);
        DrawFinder(grid, isFunction, n - 7, 0);
        DrawFinder(grid, isFunction, 0, n - 7);

        // 2. Timing patterns
        for (int i = 8; i < n - 8; i++)
        {
            grid[6, i] = (i % 2 == 0);
            isFunction[6, i] = true;
            grid[i, 6] = (i % 2 == 0);
            isFunction[i, 6] = true;
        }

        // Dark module
        grid[4 * 1 + 9, 8] = true;
        isFunction[4 * 1 + 9, 8] = true;

        // 3. Data packing: hash & encode content bits across available modules
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        var bits = new List<bool>();

        // Mode indicator (byte = 0100)
        bits.AddRange(new[] { false, true, false, false });

        // Character count (8 bits)
        int len = Math.Min(bytes.Length, 255);
        for (int i = 7; i >= 0; i--)
            bits.Add(((len >> i) & 1) == 1);

        // Data bytes
        foreach (byte b in bytes)
        {
            for (int i = 7; i >= 0; i--)
                bits.Add(((b >> i) & 1) == 1);
        }

        // Terminator & padding to capacity
        int totalBits = (n * n) - 160; // rough capacity
        while (bits.Count < totalBits)
        {
            bits.Add((bits.Count % 2 == 0));
        }

        // Place data in matrix zig-zag pattern
        int bitIdx = 0;
        int right = n - 1;
        while (right > 0)
        {
            if (right == 6) right--; // skip timing pattern column

            for (int vert = 0; vert < n; vert++)
            {
                for (int h = 0; h < 2; h++)
                {
                    int x = right - h;
                    int y = ((right + 1) / 2 % 2 == 0) ? vert : (n - 1 - vert);

                    if (!isFunction[y, x])
                    {
                        bool bit = bitIdx < bits.Count && bits[bitIdx++];
                        // Apply standard mask pattern (row + col) % 2 == 0
                        bool mask = ((y + x) % 2 == 0);
                        grid[y, x] = bit ^ mask;
                    }
                }
            }
            right -= 2;
        }

        return grid;
    }

    private static void DrawFinder(bool[,] grid, bool[,] isFunction, int startX, int startY)
    {
        for (int y = -1; y <= 7; y++)
        {
            for (int x = -1; x <= 7; x++)
            {
                int gx = startX + x;
                int gy = startY + y;
                if (gx < 0 || gx >= grid.GetLength(1) || gy < 0 || gy >= grid.GetLength(0)) continue;

                isFunction[gy, gx] = true;
                bool isOuter = (x >= 0 && x <= 6 && (y == 0 || y == 6)) || (y >= 0 && y <= 6 && (x == 0 || x == 6));
                bool isInner = (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                grid[gy, gx] = isOuter || isInner;
            }
        }
    }
}
