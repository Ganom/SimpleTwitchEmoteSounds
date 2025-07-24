#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;
using SimpleTwitchEmoteSounds.Common;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

#endregion

namespace SimpleTwitchEmoteSounds.Services.Core;

public class ExtendedUpdateInfo(
    VelopackAsset targetFullRelease,
    bool isDowngrade,
    string combinedReleaseNotes,
    List<VelopackAsset> intermediateReleases
) : UpdateInfo(targetFullRelease, isDowngrade)
{
    public string CombinedReleaseNotes { get; } = combinedReleaseNotes;
    public List<VelopackAsset> IntermediateReleases { get; } = intermediateReleases;
}

public class CombinedChangelogUpdateManager(
    IUpdateSource source,
    UpdateOptions? options = null,
    IVelopackLocator? locator = null,
    SerilogConsoleLogger? logger = null
) : UpdateManager(source, options, locator)
{
    public override async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        var baseResult = await base.CheckForUpdatesAsync();

        if (baseResult == null)
            return null;

        try
        {
            var installedVer = CurrentVersion!;
            var stagedUserId = Locator.GetOrCreateStagedUserId();
            var latestLocalFull = Locator.GetLatestLocalFullPackage();

            logger?.WriteLine("Retrieving release feed for combined changelog.");
            var feedObj = await Source
                .GetReleaseFeed(Log, AppId, Channel, stagedUserId, latestLocalFull)
                .ConfigureAwait(false);
            var feed = feedObj.Assets;

            var intermediateReleases = GetIntermediateReleases(
                feed,
                installedVer,
                baseResult.TargetFullRelease.Version
            );

            var combinedReleaseNotes = CombineReleaseNotes(intermediateReleases);

            return new ExtendedUpdateInfo(
                baseResult.TargetFullRelease,
                baseResult.IsDowngrade,
                combinedReleaseNotes,
                intermediateReleases
            );
        }
        catch (Exception ex)
        {
            logger?.WriteError(
                $"Failed to generate combined release notes, falling back to standard UpdateInfo: {ex.Message}"
            );
            return baseResult;
        }
    }

    private List<VelopackAsset> GetIntermediateReleases(
        IEnumerable<VelopackAsset> feed,
        SemanticVersion currentVersion,
        SemanticVersion targetVersion
    )
    {
        return feed.Where(r => r.Type == VelopackAssetType.Full)
            .Where(r => r.Version > currentVersion && r.Version <= targetVersion)
            .OrderBy(r => r.Version)
            .ToList();
    }

    private string CombineReleaseNotes(List<VelopackAsset> intermediateReleases)
    {
        if (intermediateReleases.Count == 0)
            return string.Empty;

        var combinedNotes = new StringBuilder();
        var allChanges = new Dictionary<string, List<string>>();

        var releases = intermediateReleases
            .Where(r => !string.IsNullOrWhiteSpace(r.NotesMarkdown))
            .Reverse()
            .ToList();

        var oldestVersion = releases.LastOrDefault()?.Version?.ToString() ?? "Unknown";
        var newestVersion = releases.FirstOrDefault()?.Version?.ToString() ?? "Unknown";

        if (releases.Count == 1)
        {
            combinedNotes.AppendLine($"**Updating to v{newestVersion}**");
        }
        else
        {
            combinedNotes.AppendLine($"**Updating from v{oldestVersion} to v{newestVersion}**");
        }
        combinedNotes.AppendLine();

        foreach (
            var changes in releases
                .Select(release => CleanReleaseNotes(release.NotesMarkdown!))
                .Select(ExtractChanges)
        )
        {
            foreach (var (category, items) in changes)
            {
                if (!allChanges.ContainsKey(category))
                    allChanges[category] = [];

                allChanges[category].AddRange(items);
            }
        }

        var categoryOrder = new[]
        {
            "✨ New Features",
            "🐛 Bug Fixes",
            "🔧 Other Changes",
            "📝 Documentation",
        };

        foreach (var category in categoryOrder)
        {
            if (!allChanges.TryGetValue(category, out var value) || value.Count == 0)
                continue;

            combinedNotes.AppendLine($"## {category}");
            combinedNotes.AppendLine();

            var uniqueChanges = allChanges[category].Distinct().OrderBy(x => x).ToList();

            foreach (var change in uniqueChanges)
            {
                combinedNotes.AppendLine($"- {change}");
            }
            combinedNotes.AppendLine();
        }

        var uncategorized = allChanges
            .Where(kv => !categoryOrder.Contains(kv.Key))
            .SelectMany(kv => kv.Value)
            .Distinct()
            .ToList();

        if (uncategorized.Count != 0)
        {
            combinedNotes.AppendLine("## 📋 Other Changes");
            combinedNotes.AppendLine();
            foreach (var change in uncategorized)
            {
                combinedNotes.AppendLine($"- {change}");
            }
            combinedNotes.AppendLine();
        }

        combinedNotes.AppendLine("---");
        combinedNotes.AppendLine($"*This summary includes {releases.Count} release(s)*");

        return combinedNotes.ToString().Trim();
    }

    private string CleanReleaseNotes(string rawNotes)
    {
        if (string.IsNullOrWhiteSpace(rawNotes))
            return string.Empty;

        var cleaned = rawNotes.Replace("\r\n", "\n").Replace("\r", "\n");

        cleaned = RegexPatterns.ExcessiveWhitespaceRegex().Replace(cleaned, " ");
        cleaned = RegexPatterns.FullChangelogRegex().Replace(cleaned, "");
        cleaned = RegexPatterns.ChangelogLinkRegex().Replace(cleaned, "");
        cleaned = RegexPatterns.HorizontalRuleRegex().Replace(cleaned, "");
        cleaned = RegexPatterns.MultipleNewlinesRegex().Replace(cleaned, "\n\n");

        return cleaned.Trim();
    }

    private Dictionary<string, List<string>> ExtractChanges(string notes)
    {
        var changes = new Dictionary<string, List<string>>();
        var currentCategory = "🔧 Other Changes";

        var lines = notes.Split(['\r', '\n'], StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (IsCategory(trimmed))
            {
                currentCategory = NormalizeCategory(trimmed);
                continue;
            }

            if (!IsChangeItem(trimmed))
                continue;
            var cleanChange = CleanChangeItem(trimmed);

            if (string.IsNullOrEmpty(cleanChange))
                continue;
            if (!changes.ContainsKey(currentCategory))
                changes[currentCategory] = new List<string>();

            changes[currentCategory].Add(cleanChange);
        }

        return changes;
    }

    private bool IsCategory(string line)
    {
        return line.StartsWith('#')
            || line.Contains("New Features")
            || line.Contains("Bug Fixes")
            || line.Contains("Other Changes")
            || line.Contains("What's Changed")
            || RegexPatterns.CategoryHeaderRegex().IsMatch(line);
    }

    private string NormalizeCategory(string category)
    {
        var lower = category.ToLowerInvariant();
        if (lower.Contains("new features") || lower.Contains('✨'))
            return "✨ New Features";
        if (lower.Contains("bug fixes") || lower.Contains("🐛"))
            return "🐛 Bug Fixes";
        if (lower.Contains("documentation") || lower.Contains("📝"))
            return "📝 Documentation";

        return "🔧 Other Changes";
    }

    private bool IsChangeItem(string line)
    {
        return line.StartsWith('-') || line.StartsWith('*') || line.StartsWith('•');
    }

    private string CleanChangeItem(string item)
    {
        var cleaned = RegexPatterns.ChangeItemPrefixRegex().Replace(item, "");
        cleaned = RegexPatterns.AuthorAttributionRegex().Replace(cleaned, "");

        if (cleaned.Length > 0 && char.IsLower(cleaned[0]))
        {
            cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        }

        return cleaned.Trim();
    }
}
