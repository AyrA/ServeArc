using System.Text.Encodings.Web;

namespace ServeArc.Utils;

public static class Html
{
    private const string Style = """
        body{font-family:sans-serif;}
        td{padding:0.25em}
        """;
    private const string HtmlTemplate = "<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>{0}</title><style>{1}</style></head><body>{2}</body></html>";

    public static string Encode(string s) => HtmlEncoder.Default.Encode(s);

    public static string Body(string title, string encodedBody)
    {
        return string.Format(HtmlTemplate, Encode(title), Style, encodedBody);
    }
}
