using System;
using System.Collections.Generic;
using System.Reactive.Linq;
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
            new InMemoryAutoCompleteService();

        public MainWindow()
        {
            InitializeComponent();

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
