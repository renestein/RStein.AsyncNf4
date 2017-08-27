namespace System.Runtime.CompilerServices
{
	[Serializable, AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class StateMachineAttribute : Attribute
	{
		public StateMachineAttribute(Type stateMachineType)
		{
			StateMachineType = stateMachineType;
		}

		public Type StateMachineType
		{
			get;
			private set;
		}
	}
}
