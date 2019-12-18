using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using Ionic.Zip;

namespace ElastiBuild.BullseyeTargets
{
    public class UnpackPackageTarget : BullseyeTargetBase<UnpackPackageTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var ap = ctx.GetArtifactPackage();

            var destDir = Path.Combine(ctx.InDir, Path.GetFileNameWithoutExtension(ap.FileName));

            using var zf = ZipFile.Read(Path.Combine(ctx.InDir, ap.FileName));

            var firstEntryPath = zf.Entries.First().FileName;

            var archiveRootDir = firstEntryPath
                .Substring(0, firstEntryPath.IndexOfAny(new[] { '/', '\\' }));

            bool allDirsRooted = zf.Entries
                .All(itm => itm.FileName.StartsWith(archiveRootDir));

            if (!allDirsRooted)
                throw new InvalidDataException(
                    $"Unexpected non-uniform root directory in product archive '{ap.FileName}'");

            Directory.CreateDirectory(destDir);

            int totalItems = zf.Count;
            int currentItem = 0;

            foreach (var itm in zf.Entries)
            {
                var fname = itm.FileName.Substring(archiveRootDir.Length + 1);

                if (itm.IsDirectory)
                {
                    Directory.CreateDirectory(
                        Path.Combine(destDir, fname));
                }
                else
                {
                    using var fs = File.Open(
                        Path.Combine(destDir, fname),
                        FileMode.Create,
                        FileAccess.Write);

                    itm.Extract(fs);
                }

                double progress = ((++currentItem * 100.0) / totalItems);
                if (progress % 10 == 0)
                    Console.WriteLine((int) progress + "%");
            }

            await Console.Out.WriteLineAsync($"Extracted to: {destDir}");
        }
    }
}
