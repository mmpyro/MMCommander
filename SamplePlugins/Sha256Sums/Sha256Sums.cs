using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommanderPlugin;

namespace Sha256Sums
{
    // Writes SHA256SUMS in the current folder for the selected files, in the format `sha256sum -c` reads.
    public class Sha256Sums : ICommanderPlugin
    {
        private const string OutputName = "SHA256SUMS";

        public string Name => "SHA-256 sums";

        // Called on the UI thread, so the hashing runs on the thread pool.
        public Task<Response> InvokeAsync(string dir, IEnumerable<string> files) => Task.Run(async () =>
        {
            var targets = files.Where(File.Exists).Where(f => Path.GetFileName(f) != OutputName).ToList();
            if (targets.Count == 0)
                return new Response("Select one or more files");

            var lines = new List<string>();
            foreach (var file in targets)
            {
                using var stream = File.OpenRead(file);
                var hash = await SHA256.HashDataAsync(stream);
                lines.Add(System.Convert.ToHexStringLower(hash) + "  " + Path.GetRelativePath(dir, file));
            }
            await File.WriteAllLinesAsync(Path.Combine(dir, OutputName), lines);
            return new Response(OperationStatus.Success);
        });
    }
}
