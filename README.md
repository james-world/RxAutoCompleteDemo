#RxAutoCompleteDemo

This repository demonstrates a typical [*Reactive Extensions*](http://reactivex.io) (Rx) client-side scenario. We will use Rx to create a robust pipeline for calling a service.

The repository has a number of lightweight tags that move through each stage of the demo - these tags appear at the start of each numbered section below. Once you have cloned the repo, you can checkout each tag in sequence to follow the demo along with `git checkout <tag>`. However, if you are tweaking the code for experimental purposes, use `git reset --hard <tag>` to throw away any changes as you move to each tag.

Although this demo uses .NET 4.6.1, WPF and Rx.NET 2.2.5, the concepts demonstrated are application to all of the many platforms to which Reactive Extensions has been translated. The use of WPF here is not really important.

We use the `Humanizer` nuget package in this demo which has some nice extension methods, including allowing us to write `2.Seconds()` instead of the unwieldy `TimeSpan.FromSeconds(2)`.

###1. `tag: demo_start`

A simple WPF application with a TextBox (`Input`) and a ListView (`Results`) below it. You can type into the TextBox, but nothing happens.

###2. `tag: echo_text`

Add `Observable.FromEventPattern` to convert the TextBox's `TextChanged` event into an `IObservable<TextChangedEventArgs>` stream.

Each `OnNext` event is triggered by a text change contains the `TextChanged` event arguments.

The `Select` following converts the stream to an `IObservable<string>` by fetching the content of the textbox. Finally, we `Subscribe` to the steam and echo the text into the `Results` ListView. Run the code and try typing some text. 

###3. `tag: call_service`

Now we will call our `InMemoryAutoCompleteService`. This service fulfils the following contract:

    public interface IAutoCompleteService
    {
        Task<AutoCompleteResult> Query(string term);
    }

    public class AutoCompleteResult
    {
        public string Term { get; set; }
        public IList<string> Matches { get; set; }
	}

You can imagine that this could be fetching suggestions from a remote service to aid completing a word in the `Input` TextBox. The actual implementation runs in-memory and simply returns a list of three items: the original text; the original text with a random letter added; and the original text with two random letters added. It will serve our demonstration purposes.

In order to call the service, we use a `SelectMany`:

    .SelectMany(_autoCompleteService.Query)

This is actually shorthand for the following:

    .SelectMany(text => _autoCompleteService.Query(text).ToObservable())

Most asynchronous calls in .NET are represented as `Task<T>`. Similarly, in JavaScript we find the use of `Promise`.  Similar non-Rx abstractions are found in other languages. Therefore, Rx implementations commonly provide conversions from these other abstractions. `ToObservable()` here converts a `Task<T>` into an `IObservable<T>` and will cause the result of the task to be emitted as an `OnNext` followed by an `OnCompleted` or if the task fails, an `OnError` contains the exception is emitted. `SelectMany` has an overload that will accept a `Task<T>` and convert it using `ToObservable()`.

`SelectMany` is actually performing two steps. First it does a `Select` which projects each `string` event into a new observable stream `IObservable<AutoCompleteResult>`. At this point our `IObservable<string>` is an `IObservable<IObservable<AutoCompleteResult>>` or stream of streams. The second step is to flatten this stream of streams into a single stream of results. `SelectMany` is called `flatMap` in most other Rx implementations.

    Text Stream    ------o-------o--------o------
    Project to Streams    \---o+  \---o+   \---o+
	Flattened      -----------o-------o--------o-

Then we call `ObserveOnDispatcher()`. This is necessary WPF requires all GUI work to be dispatched (run) on a unique thread. Because the service call is asynchronous, the result is returned on a different thread. `ObserveOnDispatcher` schedules the result to be called the GUI thread. Try commenting this line out, and you will receive an `InvalidOperationException` with the message *'The calling thread cannot access this object because a different thread owns it.'*

Finally we subscribe to the result:

    .Subscribe(DisplayMatches);

Again, a bit of shorthand using a method group, this is equivalent to:

	.Subscribe(result => DisplayMatches(result));


###4. `tag: overlapping_problem`

Now we are going to modify the behaviour of the `IAutoCompleteService` so that alternating calls will take 1 and 4 seconds respectively:

    private readonly IAutoCompleteService _autoCompleteService =
        new InMemoryAutoCompleteService(
            new RoundRobinDelayStrategy(1.Seconds(), 4.Seconds()));

The implementation isn't important here, but have a look if you are interested. What is important is that this set up demonstrates a bug in our implementation. Follow these steps carefully. Run the code and enter an `a`. Wait for the result, which will take about a second. Now, enter a `b` and `c` quickly. The result for `ab` will take about 4 seconds, but the result for `abc` will only take a second. So you will end up with the `Results` for `ab` showing when the `Input` is `abc` - this is a bug!

    Text Stream    -------1--------2---------------
    Project to Streams    \--------|-------1+ 
                                   \---2+
	Flattened      --------------------2---1--------

###5. `tag: handle_overlapping`

We can solve this by causing each new text event to trigger cancelling the subscription of the previous service call:

    Text Stream    -------1--------2---------------
    Project to Streams    \-------X| 
                                   \---2+
	Flattened      --------------------2------------

    As soon as 2 is emitted, we cancel the subscription due to 1.

This is exactly what the `Select` + `Switch` combination does. Recall previously that `Select` will give us a stream of streams. `Switch` consumes this and provides a flattened stream containing only the events of the most recent stream, as shown above.

Other versions of Rx present a combined operator called `switchMap` which achieves the same result.

Notice also that the `ObserveOnDispatcher()` has moved inside the `Select`. More on this later.

###6. `tag: errors_problem`

Service calls can unfortunately fail from time to time. Here, we have modified `IAutoCompleteService` so that it will throw an exception after a second:

    private readonly IAutoCompleteService _autoCompleteService =
        new InMemoryAutoCompleteService(
            new RoundRobinDelayStrategy(1.Seconds()),
            new AlwaysFailReliabilityStrategy());

Try calling the service, and the application will crash with an `ApplicationException`.

###7. `tag:handle_errors`

To address this, we could add an `OnError` handler to our `Subscribe` method:

    .Subscribe(
        DisplayMatches, //OnNext
        e => DisplayMatches(AutoCompleteResult.ErrorResult("Error"))); // OnError 

This will handle the error, since the exception from the service task is propagated as an `OnError` event - however, recall that a stream is terminated by an `OnError`. This approach will mean an end to our events and we will need to subscribe to a new observable. Our stream needs to be more robust and handle service errors as events.

Instead, we introduce the Rx equivalent of a try...catch... It's a bit different though. The `Catch` operator can be given a stream that will be substituted in place of it's observable. Here, we replace the failed Query stream with an `AutoCompleteResult` containing an error message:

       
    .Select(term => _autoCompleteService.Query(term)
        .ToObservable()
        .Catch(Observable.Return(AutoCompleteResult.ErrorResult(term)))
        .ObserveOnDispatcher()
    )
    .Switch()
    .Subscribe(DisplayMatches);

Run the code now, and you'll get a message about the error in the `Results` ListView.


###8. `tag: timeout_problem`

Services can misbehave in other ways - they might take too long to respond, or never respond. Here we adjust the service so that it alternates between responding it 1 and 10 seconds respectively:

    private readonly IAutoCompleteService _autoCompleteService =
        new InMemoryAutoCompleteService(
            new RoundRobinDelayStrategy(1.Seconds(), 10.Seconds()),
            new NeverFailReliabilityStrategy());

Now try running the code and issuing a couple of queries. Waiting indefinitely for a response makes for a frustrating user experience; we can do better!

###9. `tag: handle_timeout`

We can use the aptly named `Timeout` operator to solve this. Similar to the `Catch` operator, we provide an alternative stream to return in the event the observed stream takes too long to emit an event:

    .Select(term => _autoCompleteService.Query(term)
        .ToObservable()
        .Timeout(
            2.Seconds(),
            Observable.Return(
                 AutoCompleteResult.ErrorResult(
			     term, "timed out")))
        .Catch(Observable.Return(AutoCompleteResult.ErrorResult(term)))
        .ObserveOnDispatcher()
    )

Run this and see the difference. Queries return in 2 seconds, or an appropriate message is shown.

###10. `tag: transient_problem`

Sometimes services fail once in a while. We now set up our service to succeed only on every third attempt:

    private readonly IAutoCompleteService _autoCompleteService =
        new InMemoryAutoCompleteService(
            new RoundRobinDelayStrategy(1.Seconds()),
            new SucceedEveryNTriesReliabilityStrategy(3));

Run this code, and enter in `a`, wait for the response (an error), then add `b`, wait again, and then add `c` which finally returns.

In the event of failure, we might like to resubmit the query instead.

###11. `tag: handle_transient_attempt`

Rx offers the `Retry` operator. This accepts a number and will resubscribe to it's observable up to that many times when it encounters an `OnError`, before giving up and letting the `OnError` through. We can apply it like this:

    .Select(term => _autoCompleteService.Query(term).ToObservable()
        .Timeout(2.Seconds(),
                 Observable.Return(
                      AutoCompleteResult.ErrorResult(
                      term, "timed out")))
        .Retry(3)
        .Catch(Observable.Return(AutoCompleteResult.ErrorResult(term)))
        .ObserveOnDispatcher()
    )

If you run the code now though, nothing has changed! What's going on? What's happened is that the `ToObservable()` is returning the *same* result stream to each subscriber. This is by design - in the event that multiple subscriptions are made, every subscriber shares the result. It's not want we want here though. We want the task itself to be reissued on each subscription.

###12. `tag: handle_transient`

We can do this by using `Observable.FromAsync` to convert our task instead:

    .Select(term => Observable.FromAsync(() => _autoCompleteService.Query(term))

Run the code now to see that even the first query will return a successful response.

###13. `tag: overload_problem`

Things are starting to look good. Let's make our service reliable again, with a 1 second response time:

    private readonly IAutoCompleteService _autoCompleteService =
        new InMemoryAutoCompleteService(
            new RoundRobinDelayStrategy(1.Seconds()));

Notice that as things stand, we issue a request to the service every time the text changes. If our users are typing at 30 WPM, that could quickly add up to a lot of calls. We should simmer things down a bit by throttling the response.

###14. `tag: handle_overload`

We can use the `Throttle` operator for this:

    .Select(@event => ((TextBox) @event.Sender).Text)
    .Throttle(0.5.Seconds()).ObserveOnDispatcher()
    .Select(term => Observable.FromAsync(() => _autoCompleteService.Query(term))

This accepts a `TimeSpan`. Now when an event appears in the observable of a `Throttle`, it is only emitted if no other event appears in the given time frame. If an event does appear, this process is repeated. That means in this case, a text change will only be passed on after half a second of no typing.

Run the code and type continuously for a bit, noticing that a result appears only when you stop.

The observant amongst you will have noticed that `ObserveOnDispatcher` has been appended to the `Throttle` operator. As well as having a benefit we'll discuss in a moment, it is necessary if the `ObserveOnDispatcher` that appears after the `Catch` is to work. The throttle operates asynchronously, and if we don't bring the stream back to the dispatch thread, there is no dispatcher on the thread the second `ObserveOnDispatcher` runs from - and this causes an exception.

Note that `Throttle` is called `Debounce` in other Rx implementations (which is a better name for it), and in those implementations `Throttle` acts as a rate limiter, with an event being released no faster than the rate of the given `TimeSpan` rather than with continual suppression. See a demo of the difference [here](http://demo.nimius.net/debounce_throttle/). In this demo `Debounce` is like the `Throttle` we use here.

###15. `tag: handle_filtering`

Finally, a bit of tidying up. When a text event clears the throttle, we'd like to clear out the Results from the previous query. We can do this with a side effect operator, `Do`. This needs the `ObserveOnDispatcher` that precedes it, because we are updating the UI. Also, we  use a `Where` filter to suppress text events with 2 or less characters - supposing that these might return too many results in a real service. Finally, if an event passes the `Where` filter we set a waiting message in the UI. Our final stream looks like this:

    Observable.FromEventPattern<TextChangedEventArgs>(Input, "TextChanged")
        .Select(@event => ((TextBox) @event.Sender).Text)
        .Throttle(0.5.Seconds()).ObserveOnDispatcher()
        .Do(_ => ClearMatches())
        .Where(term => term?.Length > 2)
        .Do(_ => SetWaiting())
        .Select(term => Observable.FromAsync(() => _autoCompleteService.Query(term))
            .Timeout(2.Seconds(), Observable.Return(AutoCompleteResult.ErrorResult(term, "timed out")))
            .Retry(3)
            .Catch(Observable.Return(AutoCompleteResult.ErrorResult(term)))
            .ObserveOnDispatcher()
        )
        .Switch()
        .Subscribe(DisplayMatches);

What an amazing throttling, filtering, time-out-aware, error-aware, retrying, latest-results-only query it is. Imaging writing that with imperative code if you dare!

James World, September 2017



