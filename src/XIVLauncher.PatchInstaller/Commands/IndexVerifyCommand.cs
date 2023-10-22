using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using XIVLauncher.Common.Patching.IndexedZiPatch;

namespace XIVLauncher.PatchInstaller.Commands;

public class IndexVerifyCommand
{
    public static readonly Command COMMAND = new("index-verify", "Verify and optionally repair a game installation.");

    private static readonly Argument<string> PatchIndexFileArgument = new("patch-index-file", "Path to a patch index file. (*.patch.index)");

    private static readonly Argument<string> GameRootPathArgument = new(
        "game-path",
        "Root folder of a game installation, such as \"C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\"");

    private static readonly Option<int> ThreadCountOption = new(
        new[] { "-t", "--threads" },
        () => Math.Min(Environment.ProcessorCount, 8),
        "Number of threads. Specifying 0 will use all available cores.");

    static IndexVerifyCommand()
    {
        COMMAND.AddArgument(PatchIndexFileArgument);
        COMMAND.AddArgument(GameRootPathArgument);
        ThreadCountOption.AddValidator(x => x.ErrorMessage = x.GetValueOrDefault<int>() >= 0 ? null : "Must be 0 or more");
        COMMAND.AddOption(ThreadCountOption);
        COMMAND.SetHandler(x => new IndexVerifyCommand(x.ParseResult).Handle(x.GetCancellationToken()));
    }

    private readonly string patchIndexFile;
    private readonly string gameRootPath;
    private readonly int threadCount;

    private IndexVerifyCommand(ParseResult parseResult)
    {
        this.patchIndexFile = parseResult.GetValueForArgument(PatchIndexFileArgument);
        this.gameRootPath = parseResult.GetValueForArgument(GameRootPathArgument);
        this.threadCount = parseResult.GetValueForOption(ThreadCountOption);
        if (this.threadCount == 0)
            this.threadCount = Environment.ProcessorCount;
        Debug.Assert(this.threadCount > 0);
    }

    private async Task<int> Handle(CancellationToken cancellationToken)
    {
        await IndexedZiPatchOperations.VerifyFromZiPatchIndex(this.patchIndexFile, this.gameRootPath, this.threadCount, cancellationToken);
        return 0;
    }
}
