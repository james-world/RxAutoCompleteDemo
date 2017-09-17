using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RxAutoCompleteDemo.Model;

namespace RxAutoCompleteDemo.Services
{
    public class InMemoryAutoCompleteService : IAutoCompleteService
    {
        private readonly Random _random = new Random();

        private readonly IDelayStrategy _delayStrategy;
        private readonly IReliabilityStrategy _reliabilityStrategy;

        internal InMemoryAutoCompleteService() :
            this(new NoDelayStrategy(), new NeverFailReliabilityStrategy())
        {
            
        }

        internal InMemoryAutoCompleteService(IReliabilityStrategy reliabilityStrategy) :
            this(new NoDelayStrategy(), reliabilityStrategy)
        {

        }

        internal InMemoryAutoCompleteService(IDelayStrategy delayStrategy) :
            this(delayStrategy, new NeverFailReliabilityStrategy())
        {

        }

        internal InMemoryAutoCompleteService(IDelayStrategy delayStrategy, IReliabilityStrategy reliabilityStrategy)
        {
            _reliabilityStrategy = reliabilityStrategy;
            _delayStrategy = delayStrategy;
        }

        public async Task<AutoCompleteResult> Query(string term)
        {
            await _delayStrategy.Delay();

            _reliabilityStrategy.PossiblyFail();

            return new AutoCompleteResult
            {
                Term = term,
                Matches = GenerateRandomMatches(term, 3)
            };
        }

        private List<string> GenerateRandomMatches(string term, int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => Enumerable.Range(0, i)
                    .Select(_ => _random.Next(0, 26))
                    .Select(c => (char) ('a' + c))
                    .ToArray())
                .Select(a => term + new string(a))
                .ToList();
        }        
    }
}