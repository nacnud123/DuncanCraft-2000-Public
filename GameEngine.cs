using DuncanCraft.World;
using System.Diagnostics;
using System.Linq;

namespace DuncanCraft;

public class GameEngine : IDisposable
{
    private readonly CancellationTokenSource _mCancellationTokenSource;
    private readonly GameWorld _mWorld;
    private readonly Timer _mTickTimer;
    private long mTickCount;
    private bool mDisposed;
    
    // TPS tracking
    private readonly Queue<long> _mTickTimeHistory = new();
    private const int TICK_HISTORY_SIZE = 60;
    private double mCurrentTPS = 0;
    private long mLastTickTime = 0;
    
    private const int TPS = 20;
    private const int TICK_INTERVAL_MS = 1000 / TPS;
    private const int MAIN_LOOP_DELAY_MS = 16;
    private const int STATUS_UPDATE_INTERVA_LMS = 5000;
    
    public bool IsRunning { get; private set; }
    public long TickCount => mTickCount;
    public GameWorld World => _mWorld;
    public double GetCurrentTPS() => mCurrentTPS;
    
    public GameEngine()
    {
        _mCancellationTokenSource = new CancellationTokenSource();
        _mWorld = new GameWorld();
        _mTickTimer = new Timer(onTick, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public async Task StartAsync()
    {
        if (IsRunning) 
            return;
        
        IsRunning = true;
        Console.WriteLine("Starting game engine...");
        
        await _mWorld.InitializeAsync();
        
        _mTickTimer.Change(0, TICK_INTERVAL_MS);
        
        await runMainLoopAsync(_mCancellationTokenSource.Token);
    }
    
    public void Stop()
    {
        if (!IsRunning)
            return;
        
        Console.WriteLine("Stopping game engine...");
        IsRunning = false;
        _mCancellationTokenSource.Cancel();
        _mTickTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }
    
    private async Task runMainLoopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                await Task.Delay(MAIN_LOOP_DELAY_MS, cancellationToken);
                
                if (stopwatch.ElapsedMilliseconds >= STATUS_UPDATE_INTERVA_LMS)
                {
                    stopwatch.Restart();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    
    private async void onTick(object? state)
    {
        if (mDisposed || !IsRunning) 
            return;
        
        try
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            updateTPS(currentTime);
            
            Interlocked.Increment(ref mTickCount);
            await _mWorld.TickAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in game tick: {ex.Message}");
        }
    }
    
    private void updateTPS(long currentTime)
    {
        if (mLastTickTime > 0)
        {
            long deltaTime = currentTime - mLastTickTime;
            _mTickTimeHistory.Enqueue(deltaTime);
            
            while (_mTickTimeHistory.Count > TICK_HISTORY_SIZE)
            {
                _mTickTimeHistory.Dequeue();
            }
            
            if (_mTickTimeHistory.Count > 0)
            {
                double averageTickTime = _mTickTimeHistory.Average();
                mCurrentTPS = 1000.0 / averageTickTime;
            }
        }
        mLastTickTime = currentTime;
    }
    
    public void Dispose()
    {
        if (mDisposed) 
            return;
        
        Stop();
        _mTickTimer?.Dispose();
        _mCancellationTokenSource?.Dispose();
        _mWorld?.Dispose();
        
        mDisposed = true;
    }
}