using System.Diagnostics;

namespace System.Threading.Tasks
{
    [DebuggerStepThrough]
	public static class TaskExtra
	{
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

		public static Task<TResult[]> WhenAllOrEmpty<TResult>(params Task<TResult>[] tasks)
		{
			if (tasks.Length == 0)
			{
				return CompletedTask<TResult[]>.Default;
			}
			else
			{
				return Task.WhenAll<TResult>(tasks);
			}
		}

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

		public static Task<Task<TResult>> WhenAnyOrEmpty<TResult>(params Task<TResult>[] tasks)
		{
			if (tasks.Length == 0)
			{
				return CompletedTask<Task<TResult>>.Default;
			}
			else
			{
				return Task.WhenAny<TResult>(tasks);
			}
		}
	}
}
