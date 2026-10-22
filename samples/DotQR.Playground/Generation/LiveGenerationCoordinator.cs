namespace DotQR.Playground.Generation;

/// <summary>Owns the latest request for one playground component. Superseded work cannot publish.</summary>
public sealed class LiveGenerationCoordinator : IAsyncDisposable
{
    private readonly Func<GenerationRequest, CancellationToken, Task<GenerationOutcome>> _generate;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationToken _token;
    public LiveGenerationCoordinator(Func<GenerationRequest, CancellationToken, Task<GenerationOutcome>> generate, TimeProvider timeProvider)
    {
        _generate = generate;
        _timeProvider = timeProvider;
        _token = _lifetime.Token;
    }
    private sealed record State(GenerationOutcome? Outcome, bool Busy, bool Disposed = false);
    private State _state = new(null, false);
    private readonly CancellationTokenSource _lifetime = new();
    public GenerationOutcome? Current => Volatile.Read(ref _state).Outcome;
    public bool IsBusy => Volatile.Read(ref _state).Busy;
    public event Action? Changed;

    public void Reject(string error)
    {
        State previous;
        var rejected = new State(new(null, error), false);
        do
        {
            previous = Volatile.Read(ref _state);
            if (previous.Disposed)
                return;
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _state, rejected, previous), previous));
        Changed?.Invoke();
    }

    public async Task SubmitAsync(GenerationRequest request, bool immediate = false)
    {
        if (Volatile.Read(ref _state).Disposed)
            return;
        var snapshot = request.Snapshot();
        var state = new State(null, true);
        State previous;
        do
        {
            previous = Volatile.Read(ref _state);
            if (previous.Disposed)
                return;
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _state, state, previous), previous));
        Changed?.Invoke();
        var token = _token;
        try
        {
            if (!immediate)
                await Task.Delay(TimeSpan.FromMilliseconds(250), _timeProvider, token);
            if (!ReferenceEquals(Volatile.Read(ref _state), state))
                return;
            var outcome = await _generate(snapshot, token);
            var completed = new State(outcome, false);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _state, completed, state), state))
                Changed?.Invoke();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Disposal intentionally abandons the request; never publish an error or old output.
            return;
        }
    }

    public async ValueTask DisposeAsync()
    {
        var previous = Interlocked.Exchange(ref _state, new(null, false, true));
        if (previous.Disposed)
            return;
        Changed = null;
        await _lifetime.CancelAsync();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }
}
