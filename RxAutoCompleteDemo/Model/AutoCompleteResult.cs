using System;
using System.Collections.Generic;

namespace RxAutoCompleteDemo.Model
{
    public class AutoCompleteResult
    {
        public string Term { get; set; }
        public IList<string> Matches { get; set; }

        public static AutoCompleteResult ErrorResult(string term, string reason = "failed")
        {
            return new AutoCompleteResult {Term = term, Matches = new List<String> { $"Query({term}) {reason}"}};
        }

        public static AutoCompleteResult EchoResult(string term)
        {
            return new AutoCompleteResult { Term = term, Matches = new List<String> { term } };
        }
    }
}
