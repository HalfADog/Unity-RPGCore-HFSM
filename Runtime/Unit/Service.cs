using System;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class Service
	{
		public string id => m_id;
		private string m_id;
		public ServiceType serviceType => m_serviceType;
		private ServiceType m_serviceType;
		public bool pauseService => m_pauseService;
		private bool m_pauseService = false;
		public StateMachine serviceOwner => m_serviceOwner;
		private StateMachine m_serviceOwner;

		private Action<Service> m_beginService;
		private Action<Service> m_serviceAction;
		private Action<Service> m_endService;
		private Action<Service, ServiceExecuteType> m_executeService;
		public float customInterval => m_customInterval;
		private float m_customInterval;
		public Timer timer;

		public Service(string serviceId,
			Action<Service> service = null,
			Action<Service> beginService = null,
			Action<Service> endService = null,
			ServiceType type = ServiceType.Update, float customInterval = 0f)
		{
			m_id = serviceId;
			m_serviceType = type;
			m_beginService = beginService;
			m_serviceAction = service;
			m_endService = endService;
			m_customInterval = customInterval;
			timer = new Timer();
		}

		public void OnBeginService()
		{
			timer.Reset();
			m_pauseService = false;
			if (m_beginService != null)
			{
				m_beginService.Invoke(this);
				return;
			}
			OnExecuteService(ServiceExecuteType.BeginService);
		}

		public void SetBeginService(Action<Service> action) => m_beginService = action;

		public void OnSercive()
		{
			if (m_pauseService) return;
			if (m_serviceAction != null)
			{
				m_serviceAction.Invoke(this);
				return;
			}
			OnExecuteService(ServiceExecuteType.Service);
		}

		public void SetSercive(Action<Service> action) => m_serviceAction = action;

		public void OnEndService()
		{
			m_pauseService = false;
			if (m_endService != null)
			{
				m_endService.Invoke(this);
				return;
			}
			OnExecuteService(ServiceExecuteType.EndService);
		}

		public void SetEndService(Action<Service> action) => m_endService = action;

		public void OnExecuteService(ServiceExecuteType type) => m_executeService?.Invoke(this, type);

		public void SetExecuteService(Action<Service, ServiceExecuteType> service) => m_executeService = service;

		public void Pause() => m_pauseService = true;

		public void Continue() => m_pauseService = false;
	}

	[Serializable]
	public class ServiceData
	{
		public string id;

		public ServiceType serviceType = ServiceType.Update;

		public float customInterval = 0;

		[Multiline]
		public string description;
	}
}