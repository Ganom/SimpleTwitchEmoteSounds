using System.Text.RegularExpressions;

namespace SimpleTwitchEmoteSounds.Common;

public static partial class RegexPatterns
{
    [GeneratedRegex(
        @"(Cheer|DoodleCheer|BibleThump|cheerwhal|Corgo|Scoops|uni|ShowLove|Party|SeemsGood|Pride|Kappa|FrankerZ|HeyGuys|DansGame|EleGiggle|TriHard|Kreygasm|4Head|SwiftRage|NotLikeThis|FailFish|VoHiYo|PJSalt|MrDestructoid|bday|RIPCheer|Shamrock|BitBoss|Streamlabs|Muxy|HolidayCheer|Goal|Anon|Charity)\d+( |$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    )]
    public static partial Regex CheerPattern();

    [GeneratedRegex(@"\bjuh\b", RegexOptions.Compiled)]
    public static partial Regex JuhPattern();

    [GeneratedRegex(@"@([\w\s]+)\s+WDance", RegexOptions.Compiled)]
    public static partial Regex MentionPattern();

    [GeneratedRegex(@"^!setvoice\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    public static partial Regex SetVoiceRegex();

    [GeneratedRegex("^!clearvoice$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    public static partial Regex ClearVoiceRegex();

    [GeneratedRegex("^(Skip)$", RegexOptions.Compiled)]
    public static partial Regex SkipVoteRegex();

    [GeneratedRegex("^(Stay)$", RegexOptions.Compiled)]
    public static partial Regex StayVoteRegex();

    [GeneratedRegex(@"(\s|^)-(\d+[a-zA-Z]*)", RegexOptions.Compiled)]
    public static partial Regex MinusRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    public static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^!test\s+(\d+):(.+)$", RegexOptions.IgnoreCase)]
    public static partial Regex TestCommandRegex();

    [GeneratedRegex(@"\[([^\]]+)\]")]
    public static partial Regex VoiceRegex();

    [GeneratedRegex(@":([\w]+(?:_[\w]+)*):")]
    public static partial Regex EmojiAliasRegex();

    [GeneratedRegex(@"[ \t]+", RegexOptions.Compiled)]
    public static partial Regex ExcessiveWhitespaceRegex();

    [GeneratedRegex(@"\*\*Full Changelog\*\*:.*?(?=\n|$)", RegexOptions.Compiled | RegexOptions.Multiline)]
    public static partial Regex FullChangelogRegex();

    [GeneratedRegex(@"\[.*?Full Changelog.*?\]\(.*?\)", RegexOptions.Compiled)]
    public static partial Regex ChangelogLinkRegex();

    [GeneratedRegex(@"^---+\s*$", RegexOptions.Compiled | RegexOptions.Multiline)]
    public static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.Compiled)]
    public static partial Regex MultipleNewlinesRegex();

    [GeneratedRegex(@"###\s*\?+\s*(New Features|Bug Fixes|Other Changes)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    public static partial Regex CategoryHeaderRegex();

    [GeneratedRegex(@"^[-*•]\s*", RegexOptions.Compiled)]
    public static partial Regex ChangeItemPrefixRegex();

    [GeneratedRegex(@"\s+by\s+@\w+\s*$", RegexOptions.Compiled)]
    public static partial Regex AuthorAttributionRegex();
}

