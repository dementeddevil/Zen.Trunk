using System.Threading.Tasks;

namespace Zen.Trunk.Utils
{
    public interface ITaskRequest
    {
        Task Task
        {
            get;
        }
    }
}