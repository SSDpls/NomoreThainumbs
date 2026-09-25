using NoMoreThaiNums.App;
using Xunit;

namespace NoMoreThaiNums.Tests;

public class SingleInstanceManagerTests
{
    [Fact]
    public void SecondAcquire_IsRejected()
    {
        using var first = new SingleInstanceManager();
        Assert.True(first.TryAcquire());

        // Mutex ownership is thread-affine: the second acquire must happen on
        // a different thread, mirroring how a real second process would behave.
        bool secondResult = true;
        var thread = new Thread(() =>
        {
            using var second = new SingleInstanceManager();
            secondResult = second.TryAcquire();
        });
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "second acquire thread did not finish");

        Assert.False(secondResult);

        // After the first instance releases, a new one can acquire again.
        first.Dispose();
        using var third = new SingleInstanceManager();
        Assert.True(third.TryAcquire());
    }

    [Fact]
    public void SignalExistingInstance_DoesNotThrowWithoutInstance()
    {
        // No other instance is running; signaling must be a safe no-op.
        SingleInstanceManager.SignalExistingInstanceToShowWindow();
    }
}
