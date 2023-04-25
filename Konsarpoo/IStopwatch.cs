using System;
using System.Diagnostics;

namespace Konsarpoo.Collections;

/// <summary>
/// Interface to abstract system stopwatch.
/// </summary>
public interface IStopwatch
{
    bool IsRunning { get; }
    TimeSpan Elapsed { get; }
    long ElapsedTicks { get; }
    void Start();
    void Stop();
    void Restart();
    void Reset();
}

/// <summary>
/// System stopwatch wrapper
/// </summary>
public class SystemStopwatch : IStopwatch
{
    Stopwatch m_stopwatch = new Stopwatch();

    public bool IsRunning => m_stopwatch.IsRunning;
    public TimeSpan Elapsed => m_stopwatch.Elapsed;
    public long ElapsedTicks => m_stopwatch.ElapsedTicks;
    public void Start() => m_stopwatch.Start();
    public void Stop() => m_stopwatch.Stop();
    public void Reset() => m_stopwatch.Reset();
    public void Restart() => m_stopwatch.Restart();
}

/// <summary>
/// Stop watch mock class
/// </summary>
internal class MockStopwatch : IStopwatch
{
    public bool IsRunning { get; private set; }
    
    public TimeSpan Elapsed { get; set; }

    public long ElapsedTicks => Elapsed.Ticks;

    public void Start() => IsRunning = true;
    public void Stop() => IsRunning = false;

    public void Restart()
    {
        Stop();
        Reset();
        Start();
    }

    public void Reset()
    {
        Elapsed = TimeSpan.Zero;
    }
}