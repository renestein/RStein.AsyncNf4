namespace System.Runtime.CompilerServices
{
  internal interface IAsyncStateMachine
  {
    void MoveNext();
    void SetStateMachine(IAsyncStateMachine stateMachine);
  }
}