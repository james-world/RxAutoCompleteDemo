using System;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;

namespace RxAutoCompleteDemo.Services
{
    interface IDelayStrategy
    {
        Task Delay();
    }

    class RoundRobinDelayStrategy : IDelayStrategy
    {
        private int _count;
        private readonly TimeSpan[] _delays;

        public RoundRobinDelayStrategy(params TimeSpan[] delays)
        {
            _delays = delays;
        }

        public async Task Delay()
        {
            var count = Interlocked.Increment(ref _count) - 1;
            var delay = _delays[count % _delays.Length];

            await Task.Delay(delay);
        }
    }

    class NoDelayStrategy : IDelayStrategy
    {
        public async Task Delay()
        {
            // No delay, but force onto the task pool
            await Task.Run(() => { });
        }
    }
}