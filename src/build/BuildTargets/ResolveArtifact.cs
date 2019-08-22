using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

using static SimpleExec.Command;

namespace ElastiBuild.BuildTarget
{
    public class ResolveArtifact : BuildTargetBase<ResolveArtifact>
    {
        string url = @"https://artifacts.elastic.co/downloads/beats/winlogbeat/winlogbeat-7.3.0-windows-x86_64.zip";

        public async Task Build()
        {
            await Download();
            await Unpack();
        }

        async Task Download()
        {
            var fname = Path.Combine(Context.InDir, Path.GetFileName(url));
            if (File.Exists(fname))
            {
                Console.WriteLine($"----> Downloading skipped, zip present");
                return;
            }

            Directory.CreateDirectory(Context.InDir);

            Console.WriteLine($"----> Downloading {url}");
            using (var http = new HttpClient())
            {
                using (var stm = await http.GetStreamAsync(url))
                using (var fs = File.OpenWrite(fname))
                {
                    // Buffer size just shy of one that would get onto LOH
                    // TODO: use ArrayPool
                    var bytes = new byte[81920];
                    int bytesRead = 0;

                    while (true)
                    {
                        if ((bytesRead = await stm.ReadAsync(bytes, 0, bytes.Length)) <= 0)
                        {
                            Console.Out.Flush();
                            return;
                        }

                        await fs.WriteAsync(bytes, 0, bytesRead);
                        //Console.Out.Write("o");
                    }
                }
            }
        }

        Task Unpack()
        {
            var unpackedDir = Path.Combine(Context.InDir, Path.GetFileNameWithoutExtension(url));
            if (Directory.Exists(unpackedDir))
            {
                Console.WriteLine($"----> Unpacking skipped, zip unpacked (probably, we only check for directory, atm)");
                return Task.CompletedTask;
            }

            Console.WriteLine($"----> Unpacking " + unpackedDir);
            return Task.Run(() => ZipFile.ExtractToDirectory(
                Path.Combine(Context.InDir, Path.GetFileName(url)),
                Path.Combine(Context.InDir),
                overwriteFiles: true));
        }
    }
}
