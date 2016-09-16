using System;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    internal static class TransactionContextTaskRequestExtensions
    {
        public static TResult ExecuteActionWithContext<TRequest, TResult>(this TRequest request, Func<TRequest, TResult> action)
            where TRequest : ITransactionContextTaskRequest
        {
            using (AmbientSessionContext.SwitchSessionContext(request.SessionContext))
            {
                using (TrunkTransactionContext.SwitchTransactionContext(request.TransactionContext))
                {
                    return action(request);
                }
            }
        }

        public static async Task<TResult> ExecuteActionWithContextAsync<TRequest, TResult>(this TRequest request, Func<TRequest, Task<TResult>> action)
            where TRequest : ITransactionContextTaskRequest
        {
            using (AmbientSessionContext.SwitchSessionContext(request.SessionContext))
            {
                using (TrunkTransactionContext.SwitchTransactionContext(request.TransactionContext))
                {
                    return await action(request).ConfigureAwait(false);
                }
            }
        }
    }
}