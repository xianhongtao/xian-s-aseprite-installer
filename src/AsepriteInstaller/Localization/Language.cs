namespace AsepriteInstaller.Localization;

/// <summary>Supported UI languages.</summary>
public enum Language
{
    /// <summary>简体中文 (Simplified Chinese)</summary>
    ZhCN,

    /// <summary>English</summary>
    En,
}

/// <summary>
/// Two-letter ISO code helpers.
/// </summary>
public static class LanguageExtensions
{
    public static string ToCode(this Language lang) => lang switch
    {
        Language.ZhCN => "zh-CN",
        Language.En => "en",
        _ => "en",
    };

    public static Language FromCode(string? code) => code?.ToLowerInvariant() switch
    {
        "zh-cn" or "zh" => Language.ZhCN,
        "en" or "" or null => Language.En,
        _ => Language.En,
    };

    /// <summary>Display name in the language itself.</summary>
    public static string DisplayName(this Language lang) => lang switch
    {
        Language.ZhCN => "中文",
        Language.En => "English",
        _ => "English",
    };
}
