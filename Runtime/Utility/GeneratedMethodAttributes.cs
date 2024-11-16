using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class StateAttribute : Attribute
	{
		public string stateName;
		public string stateDescription;

		public StateAttribute(string stateName)
		{
			this.stateName = stateName;
			stateDescription = "";
		}

		public StateAttribute(string stateName, string stateDescription)
		{
			this.stateName = stateName;
			this.stateDescription = stateDescription;
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class ServiceAttribute : Attribute
	{
		public string serviceName;
		public string serviceDescription;

		public ServiceAttribute(string serviceName)
		{
			this.serviceName = serviceName;
			serviceDescription = "";
		}

		public ServiceAttribute(string serviceName, string serviceDescription)
		{
			this.serviceName = serviceName;
			this.serviceDescription = serviceDescription;
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class CanExitAttribute : Attribute
	{
		public string stateName;
		public string canExitDescription;

		public CanExitAttribute(string stateName)
		{
			this.stateName = stateName;
			canExitDescription = "";
		}

		public CanExitAttribute(string stateName, string canExitDescription)
		{
			this.stateName = stateName;
			this.canExitDescription = canExitDescription;
		}
	}
}