using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob
{
	public class Logger : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private Text	logText 		= null;
		[SerializeField] private bool	enableTextColor	= false;
		[SerializeField] private Color	warningColor	= Color.white;
		[SerializeField] private Color	errorColor		= Color.white;

		#endregion

		#region Member Variables

		private static Logger instance;

		#endregion

		#region Properties

		private static Logger Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<Logger>();

					if (instance != null)
					{
						instance.Initialize();
					}
				}

				return instance;
			}
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;

				Initialize();
			}
			else if (instance != this)
			{
				Debug.LogWarning("[Logger] There is more than one Logger in the scene.");
			}
		}

		private void OnDestroy()
		{
			Application.logMessageReceived -= UnityLogCallback;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Logs a message if enableDebugLogging is true
		/// </summary>
		public static void Log(string tag, string message)
		{
			string log = string.Format("[{0}] {1}", tag, message);
			
			Debug.Log(log);

			if (Instance != null)
			{
				Instance.AddLogToText(log);
			}
		}

		/// <summary>
		/// Logs an warning message if enableDebugLogging is true
		/// </summary>
		public static void LogWarning(string tag, string message)
		{
			string log = string.Format("[{0}] {1}", tag, message);
			
			Debug.LogWarning(log);

			if (Instance != null)
			{
				Instance.AddWarningLogToText(log);
			}
		}

		/// <summary>
		/// Logs a error message if enableDebugLogging is true
		/// </summary>
		public static void LogError(string tag, string message)
		{
			string log = string.Format("[{0}] {1}", tag, message);
			
			Debug.LogError(log);

			if (Instance != null)
			{
				Instance.AddErrorLogToText(log);
			}
		}

		#endregion

		#region Private Methods

		private void Initialize()
		{
			Application.logMessageReceived += UnityLogCallback;
		}

		private void AddLogToText(string log)
		{
			if (logText != null)
			{
				logText.text += "\n" + log;
			}
		}

		private void AddWarningLogToText(string log)
		{
			if (logText != null)
			{
				if (enableTextColor)
				{
					log = AddColorTags(log, warningColor);
				}

				AddLogToText(log);
			}
		}

		private void AddErrorLogToText(string log)
		{
			if (enableTextColor)
			{
				log = AddColorTags(log, errorColor);
			}

			AddLogToText(log);
		}

		private string AddColorTags(string text, Color color)
		{
			string colorStr = ColorUtility.ToHtmlStringRGBA(color);

			return string.Format("<color=#{0}>{1}</color>", colorStr, text);
		}

		private void UnityLogCallback(string condition, string stackTrace, LogType logType)
		{
			if (logType == LogType.Exception)
			{
				if (!string.IsNullOrEmpty(stackTrace))
				{
					stackTrace = stackTrace.Remove(stackTrace.Length - 1, 1);
				}

				AddErrorLogToText(condition + "\n" + stackTrace);
			}
		}

		#endregion
	}
}
