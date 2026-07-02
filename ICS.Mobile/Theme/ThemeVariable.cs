namespace ICS.Mobile.Theme;
public class ThemeVariable
{
    public static ThemeVariable Create(ThemeIds id, string cssValue)
    {
        return new ThemeVariable(id, $"--{id}", cssValue);
    }

    public ThemeVariable(ThemeIds id, string cssName, string cssValue)
    {
        Id = id;
        CssName = cssName;
        CssValue = cssValue;
    }

    /// <summary>
    /// Identifies the theme item
    /// </summary>
    public ThemeIds Id { get; }

    /// <summary>
    /// Name in css - 
    /// </summary>
    public string CssName { get; }
    public string CssValue { get; }

}
