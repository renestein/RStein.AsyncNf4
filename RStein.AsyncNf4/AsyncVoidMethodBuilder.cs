using System.Threading;

namespace System.Runtime.CompilerServices
{
  internal struct AsyncVoidMethodBuilder
  {
    public static AsyncVoidMethodBuilder Create()
    {
      var asyncVoidMethodBuilder = new AsyncVoidMethodBuilder();
      asyncVoidMethodBuilder.SynchContext = SynchronizationContext.Current;
      return asyncVoidMethodBuilder;
    }

    public SynchronizationContext SynchContext
    {
      get;
      private set;
    }

    public void SetException(Exception exception)
    {
      if (SynchContext != null)
      {
        SynchContext.Post(_ => throw exception, null);
      }

      throw exception;
    }

    public void SetResult()
    {
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {

    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
      stateMachine.MoveNext();
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
  }
}