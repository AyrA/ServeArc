namespace ServeArc.Utils;

public static class Path
{
    public static bool IsDirectChild(string item)
    {
        return !item.Contains('/') || (item.EndsWith('/') && item.IndexOf('/') == item.Length - 1);
    }

    public static string? GetCommonRoot(string[] paths, bool firstLevelOnly)
    {
        if (paths.Length == 0)
        {
            return null;
        }
        string root = paths[0];
        foreach (var p in paths.Skip(1))
        {
            if (!p.StartsWith('/'))
            {
                throw new ArgumentException($"Path '{p}' is not rooted", nameof(paths));
            }
            while (!p.StartsWith(root) && root.Length > 0)
            {
                root = root[..^1];
            }
        }
        if (string.IsNullOrEmpty(root) || root == "/")
        {
            return null;
        }
        if (firstLevelOnly)
        {
            return "/" + root[1..].Split('/')[0] + "/";
        }
        else
        {
            return root;
        }
    }
}
