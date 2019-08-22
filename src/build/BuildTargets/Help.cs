using System;
using System.Threading.Tasks;

namespace ElastiBuild.BuildTarget
{
    public class Help : BuildTargetBase<Help>
    {
        public Task Build()
        {
            return Console.Out.WriteLineAsync(@"-= ElastiBuild =-

./build [target [target [...]]] [target options]

List available targets:
    ./build --list-tree

Target options:
    --skip-tests            Do not run tests
    --cert-file             Path to certificate file to sign .msi
    --cert-pass             Certificate password

Bullseye options:
 -n, --dry-run              Do a dry run without executing actions
 -T, --list-targets         List all (or specified) targets, then exit
 -t, --list-tree            List all (or specified) targets and dependency trees, then exit
 -N, --no-color             Disable colored output
 -p, --parallel             Run targets in parallel
 -s, --skip-dependencies    Do not run targets' dependencies
 -v, --verbose              Enable verbose output
 -h, --help, -?             Show this help, then exit (case insensitive)
");
        }
    }
}
