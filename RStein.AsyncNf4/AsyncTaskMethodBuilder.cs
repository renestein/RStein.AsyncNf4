using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
  internal struct AsyncTaskMethodBuilder
  {
    private TaskCompletionSource<object> _tcs;


    public Task Task
    {
      get
      {
        return _tcs.Task;
      }
    }

    public static AsyncTaskMethodBuilder Create()
    {
      AsyncTaskMethodBuilder asyncTaskMethodBuilder;
      asyncTaskMethodBuilder._tcs = new TaskCompletionSource<object>();
      return asyncTaskMethodBuilder;
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
      stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {

    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
    {
      awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
    {
      awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
    }

    public void SetResult()
    {
      _tcs.SetResult(null);
    }

    public void SetException(Exception exception)
    {
      _tcs.SetException(exception);
    }
  }

  internal struct AsyncTaskMethodBuilder<T>
  {
    TaskCompletionSource<T> _tcs;

    public Task<T> Task
    {
      get
      {
        return _tcs.Task;
      }
    }

    public static AsyncTaskMethodBuilder<T> Create()
    {
      AsyncTaskMethodBuilder<T> asyncTaskMethodBuilder;
      asyncTaskMethodBuilder._tcs = new TaskCompletionSource<T>();
      return asyncTaskMethodBuilder;
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
      stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {

    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
    {
      awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
    {
      awaiter.UnsafeOnCompleted(stateMachine.MoveNext);

    }

    public void SetResult(T result)
    {
      _tcs.SetResult(result);
    }

    public void SetException(Exception exception)
    {
      _tcs.SetException(exception);
    }
  }
}