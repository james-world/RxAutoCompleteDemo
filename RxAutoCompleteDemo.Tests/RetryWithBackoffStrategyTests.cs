using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace RxAutoCompleteDemo.Tests
{
    public class RetryWithBackoffStrategyTests : ReactiveTest
    {
        [Test]
        public void NoRetryWhenSuccessful()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnCompleted<int>(200));

            var sut = xs.RetryWithBackoffStrategy
                (3, scheduler: scheduler);

            var results = scheduler.CreateObserver<int>();

            sut.Subscribe(results);

            scheduler.Start();

            xs.Messages.AssertEqual(
                OnNext(100, 1),
                OnCompleted<int>(200));

        }
    }
}
