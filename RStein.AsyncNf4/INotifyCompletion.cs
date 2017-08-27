namespace System.Runtime.CompilerServices
{
  internal interface INotifyCompletion
  {
    void OnCompleted(Action continuation);
  }
}