using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;

namespace ElastiBuild.BullseyeTargets
{
    public class UnpackPackageTarget : BullseyeTargetBase<UnpackPackageTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            var ap = ctx.GetArtifactPackage();

            var unpackedDir = new DirectoryInfo(
                Path.Combine(
                    ctx.InDir, Path.GetFileNameWithoutExtension(ap.FileName)));

            if (unpackedDir.Exists)
            {
			    // Simply deleting the directory introduces timing issues when directory is open 
			    // in explorer for example, with subsequent failure to create it again.
                var randomDir = Path.Combine(unpackedDir.Parent.FullName, Path.GetRandomFileName());
                unpackedDir.MoveTo(randomDir);
                Directory.Delete(randomDir, true);
            }

            await Task.Run(() =>
                ZipFile.ExtractToDirectory(
                    Path.Combine(ctx.InDir, Path.GetFileName(ap.FileName)),
                    Path.Combine(ctx.InDir),
                    overwriteFiles: true));
        }
    }
}
