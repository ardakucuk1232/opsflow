namespace OpsFlow.Application.Features.Attachments;

public static class FileTypes
{
    public const int HeaderLength = 16;

    private static readonly byte[] Pdf = "%PDF"u8.ToArray();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Gif = "GIF8"u8.ToArray();
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04];

    private static readonly Dictionary<string, (string ContentType, Func<byte[], bool> Matches)> ByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ("application/pdf", header => StartsWith(header, Pdf)),
            [".png"] = ("image/png", header => StartsWith(header, Png)),
            [".jpg"] = ("image/jpeg", header => StartsWith(header, Jpeg)),
            [".jpeg"] = ("image/jpeg", header => StartsWith(header, Jpeg)),
            [".gif"] = ("image/gif", header => StartsWith(header, Gif)),
            [".webp"] = ("image/webp", header => StartsWith(header, Riff) && header.Length >= 12 && header.AsSpan(8, 4).SequenceEqual(Webp)),
            [".txt"] = ("text/plain", IsText),
            [".csv"] = ("text/csv", IsText),
            [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", header => StartsWith(header, Zip)),
            [".xlsx"] = ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", header => StartsWith(header, Zip)),
            [".pptx"] = ("application/vnd.openxmlformats-officedocument.presentationml.presentation", header => StartsWith(header, Zip)),
            [".zip"] = ("application/zip", header => StartsWith(header, Zip))
        };

    public static IReadOnlyCollection<string> AllowedExtensions => ByExtension.Keys;

    public static bool TryGetContentType(string fileName, out string contentType)
    {
        if (ByExtension.TryGetValue(Path.GetExtension(fileName), out var type))
        {
            contentType = type.ContentType;
            return true;
        }

        contentType = string.Empty;
        return false;
    }

    public static bool HeaderMatches(string fileName, byte[] header) =>
        ByExtension.TryGetValue(Path.GetExtension(fileName), out var type) && type.Matches(header);

    private static bool StartsWith(byte[] header, byte[] signature) => header.AsSpan().StartsWith(signature);

    private static bool IsText(byte[] header) => !header.Contains((byte)0);
}
