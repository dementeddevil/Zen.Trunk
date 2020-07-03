// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MultiThreadAccessReadOnlySeekableStream.cs" company="Zen Design Software">
//   © Zen Design Software 2015
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#if NETCOREAPP5_0
using System.Collections.Concurrent;
using System.Threading;

namespace Zen.Trunk.IO
{
    public static class CallContext
    {
        private static readonly ConcurrentDictionary<string, AsyncLocal<object>> _context =
            new ConcurrentDictionary<string, AsyncLocal<object>>();
        
        public static void LogicalSetData(string key, object value)
        {
            _context.GetOrAdd(key, _ => new AsyncLocal<object>()).Value = value;
        }

        public static object LogicalGetData(string key)
        {
            return _context.TryGetValue(key, out var state) ? state.Value : null;
        }

        public static void FreeNamedDataSlot(string key)
        {
            _context.TryRemove(key, out var temp);
        }
    }
}
#endif
