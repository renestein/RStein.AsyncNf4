using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

//Tests for old NUnit
namespace RStein.AsyncNf4.Test
{
  //NUNIT problem? (resharper runner)
  // Task signature  - Ignored: Test method has non-void return type, but no result is expected

  //using dangerous overloads of the methods Task.Factory.Start(), Wait instead of await...
  [/*unsafe*/TestFixture]
  public class AwaitInfrastructureTester
  {
    private async Task When_Awaited_Task_Is_Completed_Then_Continuation_Run_On_Current_Thread_Impl()
    {
      var completedTask = getcompletedTask();
      var beforeAwaitThreadId = getCurrentThreadId();
      await completedTask;
      var afterAwaitThreadId = getCurrentThreadId();
      Assert.AreEqual(beforeAwaitThreadId, afterAwaitThreadId);
    }

    private async Task When_Awaited_Task_Is_Running_Then_Continuation_Run_On_Another_Thread_Impl()
    {
      const int SLEEP_MS = 1000;
      var ctsRunningTaskPair = getCtsRunningTaskPair();
      var beforeAwaitThreadId = getCurrentThreadId();
       Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("aa"), null);
      Task.Factory.StartNew(() =>
      {
        Thread.Sleep(SLEEP_MS);
        ctsRunningTaskPair.Cts.Cancel();
      }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
      await ctsRunningTaskPair.Task;
      var afterAwaitThreadId = getCurrentThreadId();
      Assert.AreNotEqual(beforeAwaitThreadId, afterAwaitThreadId);
    }

    private static async Task When_Awaited_Task_Has_Exception_Then_Does_Not_Propagate_Aggregate_Exception_Impl()
    {
      var faultedTask = Task.Factory.StartNew(() =>
      {
        throw new ArgumentNullException();
      }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

      try
      {
        await faultedTask;
      }
      catch (Exception ex)
      {
        Assert.That(ex, Is.Not.InstanceOf(typeof(AggregateException)));
        Assert.That(ex, Is.InstanceOf(typeof(ArgumentNullException)));
        Trace.WriteLine(ex);
        return;
      }
      Assert.Fail();
    }


    private static async Task When_Awaited_Task_Has_Nested_AggregateException_Then_Propagates_Aggregate_Exception_Impl()
    {
      var faultedTask = Task.Factory.StartNew(() =>
      {
        throw new AggregateException();
      }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

      try
      {
        await faultedTask;
      }
      catch (Exception ex)
      {
        Assert.That(ex, Is.InstanceOf(typeof(AggregateException)));
      }
    }

    private static async Task<int> When_Awaiting_Complex_Expression_Then_Result_Is_Correct_Impl()
    {
      const int FIRST_TASK_RESULT = 5;
      const int SECOND_TASK_RESULT = 10;
      var firstTask = Task.Factory.StartNew(() => FIRST_TASK_RESULT, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
      var secondTask = Task.Factory.StartNew(() => SECOND_TASK_RESULT, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

      var result = await (await Task.Factory.StartNew(async () => await firstTask + await secondTask, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default));
      return result;
    }


    private static async Task<int> When_Awaiting_Faulted_Complex_Expression_Then_Propagates_AggregateException_Impl()
    {
      const int FIRST_TASK_RESULT = 5;
      const int SECOND_TASK_RESULT = 10;
      var firstTask = Task.Factory.StartNew(() =>
      {
        throw new InvalidOperationException();
        return FIRST_TASK_RESULT;
      }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
      var secondTask = Task.Factory.StartNew(() => SECOND_TASK_RESULT,
                      CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskScheduler.Default);

      var result = await (await Task.Factory.StartNew(async () => await firstTask + await secondTask,
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default));
      return result;
    }

    private static void gcCollect()
    {
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();
    }

    private CtsTaskPair getCtsRunningTaskPair()
    {
      var cts = new CancellationTokenSource();
      var task = Task.Factory.StartNew(() => cts.Token.WaitHandle.WaitOne(),
                                      CancellationToken.None,
                                      TaskCreationOptions.None,
                                      TaskScheduler.Default);
      return new CtsTaskPair
      {
        Task = task,
        Cts = cts
      };
    }

    private static int getCurrentThreadId()
    {
      return Thread.CurrentThread.ManagedThreadId;
    }

    private Task getcompletedTask()
    {
      var completeTcs = new TaskCompletionSource<Object>();
      completeTcs.SetResult(null);
      return completeTcs.Task;
    }

    private class CtsTaskPair
    {
      public CtsTaskPair()
      {
      }

      public Task Task
      {
        get;
        set;
      }

      public CancellationTokenSource Cts
      {
        get;
        set;

      }
    }

    [TestCase(2)]
    //[TestCase(5)]
    //[TestCase(10)]
    //unsafe, not repeatable
    public void
      When_Awaiting_Many_Running_Tasks_And_Using_Using_CurrentThread_SynchronizationContext_Then_Only_First_Continuation_Run_In_Synchronization_Context(int numberOfAwaitedTasks)
    {
      var mockSynchronizationContext = new MockSynchronizationContext();
      using (var contextScope = new ScopedSynchronizationContext(mockSynchronizationContext))
      {
        When_Awaiting_Many_Running_Tasks_And_Using_CurrentThread_SynchronizationContext_Then_Only_First_Continuation_Run_In_Synchronization_Context(
          numberOfAwaitedTasks,
          mockSynchronizationContext).Wait();
      }
    }

    private static async Task When_Using_Custom_IntAwaiter_Then_Await_Works_Impl()
    {
      const int DELAY_IN_MS = 200;
      await DELAY_IN_MS;
      Trace.WriteLine("After delay!");

    }

    private async
      Task When_Awaiting_Many_Running_Tasks_And_Using_CurrentThread_SynchronizationContext_Then_Only_First_Continuation_Run_In_Synchronization_Context(
        int numberOfTasks,
        MockSynchronizationContext mockSynchronizationContext)
    {
      const int EXPECTED_SYNCHRONIZATION_CONTEXT_CALLS = 1;
      for (int i = 0; i < numberOfTasks; i++)
      {
        await runningTaskCommon();
      }

      var totalCallsInSynchronizationContext = mockSynchronizationContext.TotalCalls;
      Assert.That(totalCallsInSynchronizationContext, Is.EqualTo(EXPECTED_SYNCHRONIZATION_CONTEXT_CALLS));
    }

    private async Task When_Awaiting_Running_Task_And_Using_SynchronizationContext_Then_Continuation_Run_In_Synchronization_Context_impl(MockSynchronizationContext mockSynchronizationContext)
    {
      const int EXPECTED_SYNCHRONIZATION_CONTEXT_CALLS = 1;
      await runningTaskCommon();

      var totalCallsInSynchronizationContext = mockSynchronizationContext.TotalCalls;
      Assert.That(totalCallsInSynchronizationContext, Is.EqualTo(EXPECTED_SYNCHRONIZATION_CONTEXT_CALLS));
    }

    private async Task runningTaskCommon()
    {

      const int SLEEP_MS = 1000;
      var ctsRunningTaskPair = getCtsRunningTaskPair();
      Task.Factory.StartNew(() =>
      {
        Thread.Sleep(SLEEP_MS);
        ctsRunningTaskPair.Cts.Cancel();
      }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

      await ctsRunningTaskPair.Task.ConfigureAwait(false);
    }

    private async Task When_Awaiting_Already_Completed_Task_And_Using_SynchronizationContext_Then_Synchronization_Context_Is_Not_Used_Impl(MockSynchronizationContext mockSynchronizationContext)
    {
      const int SYNCH_CONTEXT_NOT_USED = 0;
      var task = getcompletedTask();
      await task;
      Assert.That(mockSynchronizationContext.TotalCalls, Is.EqualTo(SYNCH_CONTEXT_NOT_USED));
    }

    [Test]
    public /*Task*/ void When_Awaited_Task_Has_Nested_AggregateException_Then_Propagates_AggregateException()
    {
      When_Awaited_Task_Has_Nested_AggregateException_Then_Propagates_Aggregate_Exception_Impl().Wait();
    }

    [Test]
    public void When_Awaited_Task_Is_Completed_Then_Continuation_Run_On_Current_Thread()
    {
      When_Awaited_Task_Is_Completed_Then_Continuation_Run_On_Current_Thread_Impl().Wait();
    }

    [Test]
    //Unsafe, race condition
    public /*Task*/ void When_Awaited_Task_Is_Running_Then_Continuation_Run_On_Another_Thread()
    {
      When_Awaited_Task_Is_Running_Then_Continuation_Run_On_Another_Thread_Impl().Wait();
    }

    [Test]
    public /*Task*/ void When_Awaited_Task_Throws_ArgumentNullException_Then_Does_Not_Propagate_AggregateException()
    {
      When_Awaited_Task_Has_Exception_Then_Does_Not_Propagate_Aggregate_Exception_Impl().Wait();
    }

    [Test]
    public void
      When_Awaiting_Already_Completed_Task_And_Using_SynchronizationContext_Then_Continuation_Does_Not_Run_In_SynchronizationContext()
    {
      var mockSynchronizationContext = new MockSynchronizationContext();
      using (var contextScope = new ScopedSynchronizationContext(mockSynchronizationContext))
      {
        When_Awaiting_Already_Completed_Task_And_Using_SynchronizationContext_Then_Synchronization_Context_Is_Not_Used_Impl(
          mockSynchronizationContext).Wait();
      }
    }

    [Test]
    public void /*Task*/ When_Awaiting_Complex_Expression_Then_Result_Is_Correct()
    {
      const int EXPECTED_RESULT = 15;

      var resultTask = When_Awaiting_Complex_Expression_Then_Result_Is_Correct_Impl();
      resultTask.Wait();
      var result = resultTask.Result;
      Assert.That(result, Is.EqualTo(EXPECTED_RESULT));
    }

    [Test]
    public static void When_Awaiting_Faulted_Complex_Expression_Then_Propagates_AggregateException()
    {
      try
      {
        When_Awaiting_Faulted_Complex_Expression_Then_Propagates_AggregateException_Impl().Wait();
      }
      catch (Exception ex)
      {
        Assert.That(ex, Is.InstanceOf(typeof(AggregateException)));
        return;
      }

      Assert.Fail();
    }


    [Test]
    //unsafe, not repeatable
    public void
      When_Awaiting_Running_Task_And_Using_SynchronizationContext_Then_Continuation_Run_In_Synchronization_Context()
    {
      var mockSynchronizationContext = new MockSynchronizationContext();
      using (var contextScope = new ScopedSynchronizationContext(mockSynchronizationContext))
      {
        When_Awaiting_Running_Task_And_Using_SynchronizationContext_Then_Continuation_Run_In_Synchronization_Context_impl(
          mockSynchronizationContext).Wait();
      }
    }

    //Unsafe, not repeatable, may crash process
    //Problem: event UnobservedTaskException not fired (even in RELEASE confguration)?
    [Test]
    public void When_Task_Has_Exception_Then_Does_Not_Crash_Process()
    {
#if DEBUG
      Assert.Ignore("DEBUG mode");
#endif
      const int SLEEP_MS = 1000;

      var faultedTask = Task.Factory.StartNew(() => new ArgumentNullException());
      ((IAsyncResult)faultedTask).AsyncWaitHandle.WaitOne();
      faultedTask = null;
      gcCollect();
      Thread.Sleep(SLEEP_MS);
    }

    [Test]
    public void When_Using_Custom_IntAwaiter_Then_Await_Run_Continuation()
    {
      When_Using_Custom_IntAwaiter_Then_Await_Works_Impl().Wait();
    }
  }
}