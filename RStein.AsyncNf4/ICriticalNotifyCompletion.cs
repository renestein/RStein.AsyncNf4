using System.Security;

namespace System.Runtime.CompilerServices
{
  internal interface ICriticalNotifyCompletion : INotifyCompletion
  {
    [SecurityCritical]
    void UnsafeOnCompleted(Action continuation);
  }
}