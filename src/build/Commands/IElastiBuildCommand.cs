using System.Threading.Tasks;

namespace ElastiBuild.Commands
{
    public interface IElastiBuildCommand
    {
        Task RunAsync();
    }
}
