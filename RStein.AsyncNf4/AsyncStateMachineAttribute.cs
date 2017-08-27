
namespace System.Runtime.CompilerServices
{
	[Serializable]
	[AttributeUsageAttribute(AttributeTargets.Method, Inherited = false)]
	public sealed class AsyncStateMachineAttribute : StateMachineAttribute
	{
		public AsyncStateMachineAttribute(Type stateMachineType)
			: base(stateMachineType)
		{

		}
	}
}
