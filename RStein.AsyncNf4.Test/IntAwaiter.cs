using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.AsyncNf4.Test
{
  public class IntAwaiter : INotifyCompletion
  {
    private Task _task;

    public IntAwaiter(int value)
    {
      if (value < 0)
      {
        throw new ArgumentOutOfRangeException("value");
      }
      _task = Task.Factory.StartNew(() => Thread.Sleep(value), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
    }

    public void OnCompleted(Action continuation)
    {
      _task.ContinueWith(_ => continuation(), TaskScheduler.Default);
    }

    public void GetResult()
    {
      _task.Wait();
    }

    public bool IsCompleted
    {
      get
      {
        return _task.IsCompleted;
      }
    }

  }

  public static class IntExtensions
  {
    public static IntAwaiter GetAwaiter(this int value)
    {
      return new IntAwaiter(value);
    }

  }
}

