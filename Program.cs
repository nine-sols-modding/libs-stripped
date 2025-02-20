// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BepInEx.AssemblyPublicizer;

var gamePath = args.Length > 0 ? args[0] : DetectGameDir();
if (gamePath == null) {
    throw new Exception("Game path not found");
}

Console.WriteLine($"Game Path: {gamePath}");

var ignored = GetIgnorelist();
var gameVersion = ExtractGameVersion(gamePath);
var dlls = Directory.GetFiles(Path.Join(gamePath, "NineSols_Data/Managed"));

var jobs = (from path in dlls
    where !ignored.Contains(Path.GetFileName(path))
    select path).ToList();

var outDir = gameVersion;
Directory.CreateDirectory(outDir);

var stopwatch = Stopwatch.StartNew();
Parallel.ForEach(jobs, path => {
    var filename = Path.GetFileName(path);
    var outPath = Path.Combine(outDir, filename);
    Console.WriteLine(outPath);
    AssemblyPublicizer.Publicize(path, outPath, new AssemblyPublicizerOptions {
        Target = PublicizeTarget.None,
        Strip = true,
    });
});
Console.WriteLine($"Took {stopwatch.Elapsed.TotalSeconds:0.00} seconds to strip {jobs.Count} files.");

return;

string ExtractGameVersion(string s) {
    var configString = File.ReadAllText(Path.Join(s, "NineSols_Data/StreamingAssets/Config/config.json"));
    var config = JsonSerializer.Deserialize<Config>(configString);
    if (config == null) {
        throw new Exception($"failed to deserialize {configString}");
    }

    return config.Version.TrimStartMatches("Ver.").TrimEndMatches("-steam-app-packing/win-production")
        .ToString();
}

HashSet<string> GetIgnorelist() {
    var x = typeof(Program).Assembly.GetManifestResourceStream("libs_stripped_generator.ignored.txt")!;
    using var reader = new StreamReader(x, Encoding.UTF8);
    var hashSet = reader.ReadToEnd().Split("\n").ToHashSet();
    return hashSet;
}

string? DetectGameDir() {
    List<string> candidates = [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/share/Steam/steamapps/common/Nine Sols"),
        "Program Files (x86)/Steam/steamapps/common/Nine Sols/",
    ];

    return candidates.FirstOrDefault(Directory.Exists);
}

internal class Config {
    public required string Version { get; init; }
}

internal static class StringExtensions {
    public static ReadOnlySpan<char> TrimEndMatches(this string str, ReadOnlySpan<char> substr) =>
        str.AsSpan().TrimEnd(substr);

    public static ReadOnlySpan<char> TrimStartMatches(this string str, ReadOnlySpan<char> substr) =>
        str.AsSpan().TrimStartMatches(substr);

    public static ReadOnlySpan<char> TrimEndMatches(this ReadOnlySpan<char> span, ReadOnlySpan<char> substr) =>
        span.LastIndexOf(substr) is var idx and >= 0 ? span[..idx] : span;

    private static ReadOnlySpan<char> TrimStartMatches(this ReadOnlySpan<char> span, ReadOnlySpan<char> substr) =>
        span.LastIndexOf(substr) is var idx and >= 0 ? span[(idx + substr.Length)..] : span;
}