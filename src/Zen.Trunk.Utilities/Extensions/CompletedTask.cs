//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: CompletedTask.cs
//
//--------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Zen.Trunk.Extensions
{
	/// <summary>Provides access to an already completed task.</summary>
	/// <remarks>A completed task can be useful for using ContinueWith overloads where there aren't StartNew equivalents.</remarks>
	public static class CompletedTask
	{
		/// <summary>Gets a completed Task.</summary>
		public readonly static Task Default = CompletedTask<bool>.Default;
	}

	/// <summary>Provides access to an already completed task.</summary>
	/// <remarks>A completed task can be useful for using ContinueWith overloads where there aren't StartNew equivalents.</remarks>
	public static class CompletedTask<TResult>
	{
		/// <summary>Initializes a Task.</summary>
		static CompletedTask()
		{
			Default = Task.FromResult(default(TResult));
		}

		/// <summary>Gets a completed Task.</summary>
		public readonly static Task<TResult> Default;
	}
}
