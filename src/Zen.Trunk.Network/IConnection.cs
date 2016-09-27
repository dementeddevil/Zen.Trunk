using System;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Query;

namespace Zen.Trunk.Network
{
    // TODO: We need to do the following to support protocol stack
    // 1. PipelineManager - handles the creation and escalation of pipeline
    // 2. Pipeline - handles the communication from an endpoint to a connection
    //               this will function like Katana with send and receive lines
    //               and will need the stream support lurking in Aero Online

    public class ConnectionPipeline
    {
        //private 
    }

    public class ConnectionPipelineStage
    {
        
    }

    /// <summary>
    /// <c>IConnection</c>
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Cancels the executon of the current batch.
        /// </summary>
        void CancelExecution();

        /// <summary>
        /// Resets this instance and optionally change the active database to
        /// the master database.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task ResetAsync(bool switchToMasterDatabase = false);

        /// <summary>
        /// Executes the specified action the under session context associated
        /// with this connection instance.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task ExecuteUnderSessionAsync(Func<QueryExecutionContext, Task> action);

        /// <summary>
        /// Executes the specified action the under session context associated
        /// with this connection instance.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="action">The action to be executed.</param>
        /// <returns>
        /// A <see cref="Task{TResult}" /> representing the asynchronous operation.
        /// </returns>
        Task<TResult> ExecuteUnderSessionAsync<TResult>(Func<QueryExecutionContext, Task<TResult>> action);
    }
}