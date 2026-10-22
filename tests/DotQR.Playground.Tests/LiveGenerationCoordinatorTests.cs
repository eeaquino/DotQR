using DotQR.Playground.Generation;

namespace DotQR.Playground.Tests;

public sealed class LiveGenerationCoordinatorTests
{
    [Fact]
    public async Task OlderResultCannotReplaceNewerResultAsync()
    {
        var first = new TaskCompletionSource<GenerationOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        await using var coordinator = new LiveGenerationCoordinator((_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            { started.SetResult(); return first.Task; }
            return Task.FromResult(new GenerationOutcome(null, "newest"));
        }, TimeProvider.System);
        var older = coordinator.SubmitAsync(QrGenerationServiceTests.Request(), true);
        await started.Task;
        await coordinator.SubmitAsync(QrGenerationServiceTests.Request(), true);
        first.SetResult(new(null, "old"));
        await older;
        Assert.Equal("newest", coordinator.Current!.Error);
        Assert.False(coordinator.IsBusy);
    }

    [Fact]
    public async Task InvalidEditorValueInvalidatesPendingRenderAsync()
    {
        var pending = new TaskCompletionSource<GenerationOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var coordinator = new LiveGenerationCoordinator((_, _) => pending.Task, TimeProvider.System);
        var task = coordinator.SubmitAsync(QrGenerationServiceTests.Request(), true);
        coordinator.Reject("Scale must be a number");
        pending.SetResult(new(null, "old result"));
        await task;
        Assert.Equal("Scale must be a number", coordinator.Current!.Error);
        Assert.False(coordinator.IsBusy);
    }

    [Fact]
    public async Task SubmissionInvalidatesCurrentOutputImmediatelyAsync()
    {
        var clock = new ManualTimeProvider();
        await using var coordinator = new LiveGenerationCoordinator((_, _) => Task.FromResult(new GenerationOutcome(null, "invalid")), clock);
        await coordinator.SubmitAsync(QrGenerationServiceTests.Request(), true);
        var pending = coordinator.SubmitAsync(QrGenerationServiceTests.Request());
        Assert.Null(coordinator.Current);
        Assert.True(coordinator.IsBusy);
        clock.Advance();
        await pending;
        Assert.Equal("invalid", coordinator.Current!.Error);
    }

    [Fact]
    public async Task TextChangesCoalesceAndDisposalSuppressesPublicationAsync()
    {
        var clock = new ManualTimeProvider();
        var calls = 0;
        var coordinator = new LiveGenerationCoordinator((_, _) => { calls++; return Task.FromResult(new GenerationOutcome(null, "result")); }, clock);
        var first = coordinator.SubmitAsync(QrGenerationServiceTests.Request());
        var second = coordinator.SubmitAsync(QrGenerationServiceTests.Request());
        clock.Advance();
        await Task.WhenAll(first, second);
        Assert.Equal(1, calls);
        var third = coordinator.SubmitAsync(QrGenerationServiceTests.Request());
        await coordinator.DisposeAsync();
        await third;
        Assert.Null(coordinator.Current);
    }
}

internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Assert.Equal(TimeSpan.FromMilliseconds(250), dueTime);
        var timer = new ManualTimer(callback, state);
        _timers.Add(timer);
        return timer;
    }
    internal void Advance()
    {
        foreach (var timer in _timers.ToArray())
            timer.Fire();
        _timers.Clear();
    }
    private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
    {
        private bool _disposed;
        public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;
        internal void Fire() { if (!_disposed) callback(state); }
        public void Dispose() => _disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
