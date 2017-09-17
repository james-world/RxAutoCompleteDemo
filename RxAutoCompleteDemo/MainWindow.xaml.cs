using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Humanizer;
using RxAutoCompleteDemo.Model;
using RxAutoCompleteDemo.Services;

namespace RxAutoCompleteDemo
{
    /// <inheritdoc cref="Window" />
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IAutoCompleteService _autoCompleteService =
            new InMemoryAutoCompleteService(
                new RoundRobinDelayStrategy(1.Seconds()));

        public MainWindow()
        {
            InitializeComponent();

            Observable.FromEventPattern<TextChangedEventArgs>(Input, "TextChanged")
                .Select(@event => ((TextBox) @event.Sender).Text)                
                .SelectMany(term => Observable.FromAsync(() => _autoCompleteService.Query(term))
                    .Timeout(2.Seconds(), Observable.Return(AutoCompleteResult.ErrorResult(term, "timed out")))
                    .Retry(3)
                    .Catch(Observable.Return(AutoCompleteResult.ErrorResult(term)))
                    .ObserveOnDispatcher()
                )
                .Subscribe(DisplayMatches);
        }

        private void DisplayMatches(AutoCompleteResult result)
        {
            Results.Items.Clear();
            foreach (var match in result.Matches)
                Results.Items.Add(match);
        }

        private void ClearMatches()
        {
            Results.Items.Clear();
        }

        private void SetWaiting()
        {
            Results.Items.Clear();
            Results.Items.Add("Querying...");
        }
    }
}
