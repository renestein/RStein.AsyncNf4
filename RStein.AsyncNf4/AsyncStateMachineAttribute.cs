using System;

using System.Runtime.CompilerServices;

namespace Common.Test.Tasks
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
