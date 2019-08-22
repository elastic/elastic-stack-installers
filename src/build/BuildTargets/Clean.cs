using System;
using System.Threading.Tasks;

using static SimpleExec.Command;

namespace ElastiBuild.BuildTarget
{
    public class Clean : BuildTargetBase<Clean>
    {
        public async Task Build()
        {
            Console.WriteLine($"----> Cleaning");
            await Task.Yield();
        }
    }
}
