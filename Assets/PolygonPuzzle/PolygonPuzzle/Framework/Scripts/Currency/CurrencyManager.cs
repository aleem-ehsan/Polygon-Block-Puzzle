using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob
{
	public class CurrencyManager : SingletonComponent<CurrencyManager>, ISaveable
	{
		#region Classes

		[System.Serializable]
		public class Settings
		{
			public bool		autoUpdateCurrencyText		= true;
			public Text		currencyText				= null;
		}

		[System.Serializable]
		private class CurrencyInfo
		{
			public string	id				= "";
			public int		startingAmount	= 0;
			public Settings	settings		= null;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private List<CurrencyInfo>	currencyInfos = null;

		#endregion

		#region Member Variables

		private Dictionary<string, int> currencyAmounts;

		#endregion

		#region Properties

		public string SaveId { get { return "currency_manager"; } }

		public System.Action<string> OnCurrencyChanged { get; set; }

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			currencyAmounts = new Dictionary<string, int>();
			
			SaveManager.Instance.Register(this);

			if (!LoadSave())
			{
				SetStartingValues();
			}

			for (int i = 0; i < currencyInfos.Count; i++)
			{
				string currencyId = currencyInfos[i].id;
				
				UpdateCurrencyText(currencyId, currencyAmounts[currencyId]);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the amount of currency the player has
		/// </summary>
		public int GetAmount(string currencyId)
		{
			if (!CheckCurrencyExists(currencyId))
			{
				return 0;
			}

			return currencyAmounts[currencyId];
		}

		/// <summary>
		/// Tries to spend the curreny
		/// </summary>
		public bool TrySpend(string currencyId, int amount)
		{
			if (!CheckCurrencyExists(currencyId))
			{
				return false;
			}

			// Check if the player has enough of the currency
			if (currencyAmounts[currencyId] >= amount)
			{
				ChangeCurrency(currencyId, -amount, true);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Gives the amount of currency
		/// </summary>
		public void Give(string currencyId, int amount)
		{
			if (!CheckCurrencyExists(currencyId))
			{
				return;
			}

			ChangeCurrency(currencyId, amount);
		}

		/// <summary>
		/// Gives the amount of currency, data is of the following format: "id;amount"
		/// </summary>
		public void Give(string data)
		{
			string[] stringObjs = data.Trim().Split(';');

			if (stringObjs.Length != 2)
			{
				Debug.LogErrorFormat("[CurrencyManager] Give(string data) : Data format incorrect: \"{0}\", should be \"id;amount\"", data);
				return;
			}

			string currencyId	= stringObjs[0];
			string amountStr	= stringObjs[1];

			int amount;

			if (!int.TryParse(amountStr, out amount))
			{
				Debug.LogErrorFormat("[CurrencyManager] Give(string data) : Amount must be an integer, given data: \"{0}\"", data);
				return;
			}

			if (!CheckCurrencyExists(currencyId))
			{
				return;
			}

			ChangeCurrency(currencyId, amount, true);
		}

		public void UpdateCurrencyText(string currencyId, int specificAmount = -1)
		{
			CurrencyInfo currencyInfo = GetCurrencyInfo(currencyId);

			if (currencyInfo != null && currencyInfo.settings.currencyText != null)
			{
				currencyInfo.settings.currencyText.text = (specificAmount >= 0) ? specificAmount.ToString() : currencyAmounts[currencyId].ToString();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the currency
		/// </summary>
		private void ChangeCurrency(string currencyId, int amount, bool forceUpdateCurrencyText = false)
		{
			currencyAmounts[currencyId] += amount;

			CurrencyInfo currencyInfo = GetCurrencyInfo(currencyId);

			if (currencyInfo.settings.autoUpdateCurrencyText || forceUpdateCurrencyText)
			{
				currencyInfo.settings.currencyText.text = currencyAmounts[currencyId].ToString();
			}

			if (OnCurrencyChanged != null)
			{
				OnCurrencyChanged(currencyId);
			}
		}

		/// <summary>
		/// Sets all the starting currency amounts
		/// </summary>
		private void SetStartingValues()
		{
			for (int i = 0; i < currencyInfos.Count; i++)
			{
				CurrencyInfo currencyInfo = currencyInfos[i];

				currencyAmounts[currencyInfo.id] = currencyInfo.startingAmount;
			}
		}

		/// <summary>
		/// Gets the CUrrencyInfo for the given id
		/// </summary>
		private CurrencyInfo GetCurrencyInfo(string currencyId)
		{
			for (int i = 0; i < currencyInfos.Count; i++)
			{
				CurrencyInfo currencyInfo = currencyInfos[i];

				if (currencyId == currencyInfo.id)
				{
					return currencyInfo;
				}
			}

			return null;
		}

		/// <summary>
		/// Checks that the currency exists
		/// </summary>
		private bool CheckCurrencyExists(string currencyId)
		{
			CurrencyInfo currencyInfo = GetCurrencyInfo(currencyId);

			if (currencyInfo == null || !currencyAmounts.ContainsKey(currencyId))
			{
				Debug.LogErrorFormat("[CurrencyManager] CheckCurrencyExists : The given currencyId \"{0}\" does not exist", currencyId);

				return false;
			}

			return true;
		}

		#endregion

		#region Save Methods

		public Dictionary<string, object> Save()
		{
			Dictionary<string, object> saveData = new Dictionary<string, object>();

			saveData["amounts"] = currencyAmounts;

			return saveData;
		}

		public bool LoadSave()
		{
			JSONNode json = SaveManager.Instance.LoadSave(this);

			if (json == null)
			{
				return false;
			}

			foreach (KeyValuePair<string, JSONNode> pair in json["amounts"])
			{
				// Make sure the currency still exists
				if (GetCurrencyInfo(pair.Key) != null)
				{
					currencyAmounts[pair.Key] = pair.Value.AsInt;
				}
			}

			return true;
		}

		#endregion
	}
}
