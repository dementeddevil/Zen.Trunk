using System.Diagnostics;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.Utils
{
    /// <summary>
    /// 
    /// </summary>
    [DebuggerStepThrough]
	public static class TaskExtra
	{
        /// <summary>
        /// Whens all or empty.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        /// <returns></returns>
        public static Task WhenAllOrEmpty(params Task[] tasks)
		{
			if (tasks.Length == 0)
			{
				return CompletedTask.Default;
			}
			else
			{
				return Task.WhenAll(tasks);
			}
		}

        /// <summary>
        /// Whens all or empty.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="tasks">The tasks.</param>
        /// <returns></returns>
        public static Task<TResult[]> WhenAllOrEmpty<TResult>(params Task<TResult>[] tasks)
		{
			if (tasks.Length == 0)
			{
				return CompletedTask<TResult[]>.Default;
			}
			else
			{
				return Task.WhenAll(tasks);
			}
		}

        /// <summary>
        /// Whens any or empty.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        /// <returns></returns>
        public static Task WhenAnyOrEmpty(params Task[] tasks)
		{
			if (tasks.Length == 0)
			{
				return CompletedTask.Default;
			}
			else
			{
				return Task.WhenAny(tasks);
			}
		}

        /// <summary>
        /// Whens any or empty.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="tasks">The tasks.</param>
        /// <returns></returns>
        public static Task<Task<TResult>> WhenAnyOrEmpty<TResult>(params Task<TResult>[] tasks)
		{
			if (tasks.Length == 0)
			{
				return CompletedTask<Task<TResult>>.Default;
			}
			else
			{
				return Task.WhenAny(tasks);
			}
		}
	}
}
