using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if BBG_UNITYADS
using UnityEngine.Monetization;
#endif

namespace dotmob
{
	public class InvokeOnMainThread : MonoBehaviour
	{
		#region Inspector Variables


		#endregion

		#region Member Variables

		private static InvokeOnMainThread	instance	= null;
		private static object				lockObj		= new object();

		private List<System.Action<object>>	actions;
		private List<object>				args;

		#endregion

		#region Unity Methods

		private void Update()
		{
			lock (lockObj)
			{
				while (actions.Count > 0)
				{
					actions[0](args[0]);

					actions.RemoveAt(0);
					args.RemoveAt(0);
				}
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Creates the InvokeOnMainThread instance, must be called on the main unity thread prior to any Action calls
		/// </summary>
		public static void CreateInstance()
		{
			if (instance == null)
			{
				instance = new GameObject("InvokeOnMainThread").AddComponent<InvokeOnMainThread>();

				DontDestroyOnLoad(instance.gameObject);

				instance.Init();
			}
		}

		public static void Action(System.Action<object> action, object arg = null)
		{
			instance.QueueAction(action, arg);
		}

		#endregion

		#region Private Methods

		private void Init()
		{
			actions	= new List<System.Action<object>>();
			args	= new List<object>();
		}

		private void QueueAction(System.Action<object> action, object arg)
		{
			lock (lockObj)
			{
				actions.Add(action);
				args.Add(arg);
			}
		}

		#endregion
	}
}
