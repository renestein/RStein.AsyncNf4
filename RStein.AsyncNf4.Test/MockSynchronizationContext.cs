using System.Threading;

namespace RStein.AsyncNf4.Test
{
  public class MockSynchronizationContext : SynchronizationContext
  {
    private int _sendCallCount;
    private int _postCallCount;

    public MockSynchronizationContext()
    {
      _sendCallCount = _postCallCount = 0;
    }

    public int SendCallCount
    {
      get
      {
        return _sendCallCount;
      }
    }

    public int PostCallCount
    {
      get
      {
        return _postCallCount;
      }
    }

    public int TotalCalls
    {
      get
      {
        return _sendCallCount + _postCallCount;
      }
    }

    public override void Send(SendOrPostCallback d, object state)
    {
      Interlocked.Increment(ref _sendCallCount);
      d(state);
    }

    public override void Post(SendOrPostCallback d, object state)
    {
      Interlocked.Increment(ref _sendCallCount);
      d(state);
    }


    public override SynchronizationContext CreateCopy()
    {
      return this;
    }
  }
}