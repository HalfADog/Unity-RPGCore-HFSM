using System;

namespace RPGCore.AI.HFSM
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class StateMachineControllerAttribute : Attribute
	{
		public string ControllerName;
	}
}