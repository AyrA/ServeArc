using System.Text.Encodings.Web;

namespace ServeArc.Utils;

public static class Html
{
    private const string Style = """
        body{font-family:sans-serif}
        td{padding:0.25em}
        #footer{margin-top:1em}
        """;
    private const string HtmlTemplate = "<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>{0}</title><style>{1}</style></head><body>{2}{3}</body></html>";

    private const string Footer = "<div id=\"footer\"><hr /><i>Copyright &copy; 2026 - <a href=\"https://github.com/AyrA/ServeArc\" target=\"_blank\">AyrA/ServeArc</a></i></div>";

    public static string Encode(string s) => HtmlEncoder.Default.Encode(s);

    public static string Body(string title, string encodedBody)
    {
        return string.Format(HtmlTemplate, Encode(title), Style, encodedBody, Footer);
    }
}
