using System.Threading.Tasks;

namespace Zen.Trunk.Utils
{
    /// <summary>
    /// <c>ITaskRequest</c> defines the base contract for a request object.
    /// </summary>
    public interface ITaskRequest
    {
        /// <summary>
        /// Gets the task.
        /// </summary>
        /// <value>
        /// The task.
        /// </value>
        Task Task
        {
            get;
        }
    }
}