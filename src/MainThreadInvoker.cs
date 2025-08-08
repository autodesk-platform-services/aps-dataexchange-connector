using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Autodesk.DataExchange.UI.Core.Interfaces;

namespace SampleConnector
{
    internal class MainThreadInvoker : IMainThreadInvoker
    {
        private readonly Dispatcher dispatcher;

        public MainThreadInvoker(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public async Task<T> InvokeAsync<T>(Func<Task<T>> func)
        {
            if (this.dispatcher.CheckAccess())
            {
                return await func();
            }

            var operation = await this.dispatcher.InvokeAsync(func);
            return await operation;
        }
    }
}
