using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using RStein.AsyncNf4;

namespace RStein.AsyncNf4
{
  public static class TaskExtensions
  {
    private static readonly MethodInfo _preserveStackMethod;
    private static readonly object[] NO_ARGS;

    static TaskExtensions()
    {
      setUnobserverdHandler();
      _preserveStackMethod = TryGetExceptionPreserveStackMethodInfo();
      NO_ARGS = new object[0];
    }

    public static TaskAwaiter GetAwaiter(this Task task)
    {
      return new TaskAwaiter(task);
    }

    public static TaskAwaiter<T> GetAwaiter<T>(this Task<T> task)
    {
      return new TaskAwaiter<T>(task);
    }

    public static TaskAwaiter ConfigureAwait(this Task task, bool continueOnCapturedContext)
    {
      return new TaskAwaiter(task, continueOnCapturedContext);
    }

    public static TaskAwaiter<TResult> ConfigureAwait<TResult>(this Task<TResult> task, bool continueOnCapturedContext)
    {
      return new TaskAwaiter<TResult>(task, continueOnCapturedContext);
    }

    internal static void PreserveExceptionStack(this Exception ex)
    {
      if (_preserveStackMethod == null)
      {
        return;
      }

      _preserveStackMethod.Invoke(ex, NO_ARGS);
    }


    internal static void TryRethrowException(this Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      Exception taskException = null;
      try
      {
        task.Wait();
      }
      catch (Exception ex)
      {
        taskException = ex;
      }

      if (task.IsCanceled)
      {
        throw new TaskCanceledException(task);
      }

      if (task.Status != TaskStatus.Faulted)
      {
        Debug.Assert(task.Status == TaskStatus.RanToCompletion);
        return;
      }

      var toThrowException = taskException.InnerException;
      Debug.Assert(toThrowException != null);

      toThrowException.PreserveExceptionStack();
      throw toThrowException;
    }

    private static void setUnobserverdHandler()
    {
      //dirty
      TaskScheduler.UnobservedTaskException += (sender, ex) =>
      {
        if (!ex.Observed)
        {
          Debug.WriteLine(ex);
        }

        ex.SetObserved();
      };
    }

    //Hack, Reactive extension do something similar
    private static MethodInfo TryGetExceptionPreserveStackMethodInfo()
    {
      const string PRESERVE_STACK_METHOD = "PrepForRemoting";
      try
      {
        return typeof(Exception).GetMethod(PRESERVE_STACK_METHOD,
          BindingFlags.Instance |
          BindingFlags.NonPublic |
          BindingFlags.InvokeMethod);
      }
      catch (Exception)
      {
        return null;
      }
    }
  }


  internal class ContextsContinuationTriad
  {
    public SynchronizationContext SynchContext
    {
      get;
      set;
    }

    public ExecutionContext ExecutionContext
    {
      get;
      set;
    }

    public Action Continuation
    {
      get;
      set;
    }
  }
}

//TODO: Not mutable struct

public class TaskAwaiter : ICriticalNotifyCompletion
{
  private readonly bool _continueOnCapturedContext;
  private readonly Task _task;
  private ContextsContinuationTriad _continuationTriad;

  internal TaskAwaiter(Task task, bool continueOnCapturedContext = true)
  {
    if (task == null)
    {
      throw new ArgumentNullException("task");
    }

    _task = task;
    _continueOnCapturedContext = continueOnCapturedContext;
  }

  private ContextsContinuationTriad ContinuationTriad
  {
    get
    {
      return _continuationTriad ?? (_continuationTriad = new ContextsContinuationTriad());
    }
    set
    {
      _continuationTriad = value;
    }
  }

  public bool IsCompleted
  {
    get
    {
      return _task.IsCompleted;
    }
  }

  public void OnCompleted(Action continuation)
  {
    ContinuationTriad = CaptureContext(continuation);
    OnCompletedCommon(_task, PreserveOldSyncContextContinuation, _continueOnCapturedContext);
  }

  [SecuritySafeCritical]
  public void UnsafeOnCompleted(Action continuation)
  {
    ContinuationTriad = CaptureContext(continuation);

    using (var asyncFlowControl = ExecutionContext.SuppressFlow())
    {
      OnCompletedCommon(_task, PreserveOldSyncContextContinuation, _continueOnCapturedContext);
    }
  }

  public TaskAwaiter GetAwaiter()
  {
    return this;
  }

  [SecuritySafeCritical]
  public static void preserveOldSynchronizationContextForContinuation(object context)
  {
    var awaiterContext = context as ContextsContinuationTriad;

    var oldSynchContext = awaiterContext.SynchContext;

    var continuation = awaiterContext.Continuation;
    Debug.Assert(continuation != null);

    SynchronizationContext.SetSynchronizationContext(oldSynchContext);
    continuation();
  }


  public void GetResult()
  {
    _task.TryRethrowException();
  }

  internal static ContextsContinuationTriad CaptureContext(Action continuation)
  {
    var continuationTriad = new ContextsContinuationTriad
    {
      Continuation = continuation,
      ExecutionContext = ExecutionContext.Capture().CreateCopy()
    };
    return continuationTriad;
  }

  [SecuritySafeCritical]
  internal void PreserveOldSyncContextContinuation()
  {
    PreserveOldSynchContextExecutor(ContinuationTriad);
  }

  [SecuritySafeCritical]
  internal static void PreserveOldSynchContextExecutor(ContextsContinuationTriad contextTriad)
  {
    var executionContextCopy = contextTriad.ExecutionContext;
    var currentSynchronizationContext = SynchronizationContext.Current;
    contextTriad.SynchContext = currentSynchronizationContext;
    ExecutionContext.Run(executionContextCopy, preserveOldSynchronizationContextForContinuation, contextTriad);
  }

  internal static void OnCompletedCommon(Task task, Action continuation, bool useSynchContext)
  {
    if (task == null)
    {
      throw new ArgumentNullException("task");
    }
    if (continuation == null)
    {
      throw new ArgumentNullException("continuation");
    }


    var currentScheduler = useSynchContext && SynchronizationContext.Current != null
      ? TaskScheduler.FromCurrentSynchronizationContext()
      : TaskScheduler.Default;

    task.ContinueWith(completedTask =>
    {
      continuation();

    }, currentScheduler);
  }
}

public class TaskAwaiter<T> : ICriticalNotifyCompletion
{
  private readonly bool _continueOnCapturedContext;
  private readonly Task<T> _task;
  private ContextsContinuationTriad _continuationTriad;


  internal TaskAwaiter(Task<T> task, bool continueOnCapturedContext = true)
  {
    if (task == null)
    {
      throw new ArgumentNullException("task");
    }
    _task = task;
    _continueOnCapturedContext = continueOnCapturedContext;
  }

  private ContextsContinuationTriad ContinuationTriad
  {
    get
    {
      return _continuationTriad ?? (_continuationTriad = new ContextsContinuationTriad());
    }
    set
    {
      _continuationTriad = value;
    }
  }

  public bool IsCompleted
  {
    get
    {
      return _task.IsCompleted;
    }
  }

  public void OnCompleted(Action continuation)
  {
    ContinuationTriad = TaskAwaiter.CaptureContext(continuation);
    TaskAwaiter.OnCompletedCommon(_task, PreserveOldSyncContextContinuation, _continueOnCapturedContext);
  }

  public void UnsafeOnCompleted(Action continuation)
  {
    ContinuationTriad = TaskAwaiter.CaptureContext(continuation);

    using (var asyncFlowControl = ExecutionContext.SuppressFlow())
    {
      TaskAwaiter.OnCompletedCommon(_task, PreserveOldSyncContextContinuation, _continueOnCapturedContext);
    }
  }

  public TaskAwaiter<T> GetAwaiter()
  {
    return this;
  }

  public T GetResult()
  {

    _task.TryRethrowException();
    return _task.Result;
  }


  [SecuritySafeCritical]
  private void PreserveOldSyncContextContinuation()
  {
    TaskAwaiter.PreserveOldSynchContextExecutor(ContinuationTriad);
  }
}