namespace ServeArc;

public class Arguments
{
    private const StringComparison CI = StringComparison.InvariantCultureIgnoreCase;

    public bool SkipRoot { get; set; }

    public bool NoBrowse { get; set; }

    public bool AutoSkipRoot { get; set; }

    public bool Help { get; set; }

    public List<string> ArchiveFiles { get; set; } = [];

    public Arguments(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("/?", CI))
            {
                Help = true;
                break;
            }
            else if (args[i].Equals("/SR", CI))
            {
                if (AutoSkipRoot)
                {
                    throw new ArgumentException("Cannot use /SR when /AS has already been used", nameof(args));
                }
                SkipRoot = true;
            }
            else if (args[i].Equals("/AS", CI))
            {
                if (SkipRoot)
                {
                    throw new ArgumentException("Cannot use /AS when /SR has already been used", nameof(args));
                }
                AutoSkipRoot = true;
            }
            else if (args[i].Equals("/NB", CI))
            {
                NoBrowse = true;
            }
            else
            {
                ArchiveFiles.Add(args[i]);
            }
        }
    }
}
