using System;
using System.Threading;

namespace RxAutoCompleteDemo.Services
{
    interface IReliabilityStrategy
    {
        void PossiblyFail();
    }

    class SucceedEveryNTriesReliabilityStrategy : IReliabilityStrategy
    {
        private int _count;
        private readonly int _n;

        public SucceedEveryNTriesReliabilityStrategy(int n)
        {
            _n = n;
        }

        public void PossiblyFail()
        {
            var count = Interlocked.Increment(ref _count);

            if(count % _n != 0)
                throw new ApplicationException($"I succeed only every {_n} times");
        }
    }

    class AlwaysFailReliabilityStrategy : IReliabilityStrategy
    {
        public void PossiblyFail()
        {
            throw new ApplicationException("I always fail");
        }
    }

    class NeverFailReliabilityStrategy : IReliabilityStrategy
    {
        public void PossiblyFail()
        {

        }
    }
}