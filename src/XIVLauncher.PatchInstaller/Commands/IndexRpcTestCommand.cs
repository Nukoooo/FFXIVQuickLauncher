using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using XIVLauncher.Common.Patching.IndexedZiPatch;

namespace XIVLauncher.PatchInstaller.Commands;

public class IndexRpcTestCommand
{
    public static readonly Command COMMAND = new("index-rpc-test") { IsHidden = true };

    static IndexRpcTestCommand()
    {
        COMMAND.SetHandler(x => new IndexRpcTestCommand(x.ParseResult).Handle());
    }

    private IndexRpcTestCommand(ParseResult parseResult)
    {
    }

    private async Task<int> Handle()
    {
        const int MAX_CONCURRENT_CONNECTIONS_FOR_PATCH_SET = 1;
        const string BASE_DIR = @"Z:\tgame";

        // Cancel in 15 secs
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var availableSourceUrls = new Dictionary<string, string>
        {
            { "boot:D2013.06.18.0000.0000.patch", "http://patch-dl.ffxiv.com/boot/2b5cbc63/D2013.06.18.0000.0000.patch" },
            { "boot:D2021.11.16.0000.0001.patch", "http://patch-dl.ffxiv.com/boot/2b5cbc63/D2021.11.16.0000.0001.patch" },
        };

        var rootAndPatchPairs = new List<Tuple<string, string>>
        {
            Tuple.Create(@$"{BASE_DIR}\boot", @"Z:\patch-dl.ffxiv.com\boot\2b5cbc63\D2021.11.16.0000.0001.patch.index"),
        };

        // Run verifier as subprocess
        // using var verifier = new IndexedZiPatchIndexRemoteInstaller(System.Reflection.Assembly.GetExecutingAssembly().Location, true);
        // Run verifier as another thread
        using var verifier = new IndexedZiPatchIndexRemoteInstaller(null, true);

        foreach (var (gameRootPath, patchIndexFilePath) in rootAndPatchPairs)
        {
            var patchIndex = new IndexedZiPatchIndex(new BinaryReader(new DeflateStream(new FileStream(patchIndexFilePath, FileMode.Open, FileAccess.Read), CompressionMode.Decompress)));

            await verifier.ConstructFromPatchFile(patchIndex, 1000);

            void ReportCheckProgress(int index, long progress, long max)
            {
                Log.Information("[{0}/{1}] Checking file {2}... {3:0.00}/{4:0.00}MB ({5:00.00}%)", index + 1, patchIndex.Length, patchIndex[Math.Min(index, patchIndex.Length - 1)].RelativePath,
                                progress / 1048576.0, max / 1048576.0, 100.0 * progress / max);
            }

            void ReportInstallProgress(int index, long progress, long max, IndexedZiPatchInstaller.InstallTaskState state)
            {
                Log.Information("[{0}/{1}] {2} {3}... {4:0.00}/{5:0.00}MB ({6:00.00}%)", index + 1, patchIndex.Sources.Count, state, patchIndex.Sources[Math.Min(index, patchIndex.Sources.Count - 1)],
                                progress / 1048576.0, max / 1048576.0, 100.0 * progress / max);
            }

            verifier.OnVerifyProgress += ReportCheckProgress;
            verifier.OnInstallProgress += ReportInstallProgress;

            for (var attemptIndex = 0; attemptIndex < 5; attemptIndex++)
            {
                await verifier.SetTargetStreamsFromPathReadOnly(gameRootPath);
                // TODO: check one at a time if random access is slow?
                await verifier.VerifyFiles(attemptIndex > 0, Environment.ProcessorCount, cancellationToken);

                var missingPartIndicesPerTargetFile = await verifier.GetMissingPartIndicesPerTargetFile();
                if (missingPartIndicesPerTargetFile.All(x => !x.Any()))
                    break;

                var missingPartIndicesPerPatch = await verifier.GetMissingPartIndicesPerPatch();
                await verifier.SetTargetStreamsFromPathReadWriteForMissingFiles(gameRootPath);
                var prefix = patchIndex.ExpacVersion == IndexedZiPatchIndex.EXPAC_VERSION_BOOT ? "boot:" : $"ex{patchIndex.ExpacVersion}:";

                for (var i = 0; i < patchIndex.Sources.Count; i++)
                {
                    if (!missingPartIndicesPerPatch[i].Any())
                        continue;

                    await verifier.QueueInstall(i, new Uri(availableSourceUrls[prefix + patchIndex.Sources[i]]), null, MAX_CONCURRENT_CONNECTIONS_FOR_PATCH_SET);
                    // await verifier.QueueInstall(i, new FileInfo(availableSourceUrls[prefix + patchIndex.Sources[i]].Replace("http:/", "Z:")));
                }

                await verifier.Install(MAX_CONCURRENT_CONNECTIONS_FOR_PATCH_SET, cancellationToken);
                await verifier.WriteVersionFiles(gameRootPath);
            }

            verifier.OnVerifyProgress -= ReportCheckProgress;
            verifier.OnInstallProgress -= ReportInstallProgress;
        }

        return 0;
    }
}
