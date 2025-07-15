using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gley.MobileAds;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	public class GameManager : SingletonComponent<GameManager>, ISaveable
	{
		#region Inspector Variables

		[Header("Data")]
		[SerializeField] private List<BundleInfo>	bundleInfos			= null;
		[SerializeField] private int				hintCoinCost		= 50;

		[Header("Rewards")]
		[SerializeField] private int				numLevelsForReward	= 10;
		[SerializeField] private int				numCoinsRewarded	= 100;

		[Header("Ads")]
		[SerializeField] private int				numLevelsBetweenAds	= 0;
		//[SerializeField] private int				minTimeBetweenAds	= 0;	// Amount of time that must pass since the last interstitial ad before the next interstitial ad is shown

		[Header("Debug")]
		[SerializeField] private bool				unlockAllPacks	= false;	// Sets all packs to be unlocked
		[SerializeField] private bool				unlockAllLevels	= false;	// Sets all levels to be unlocked (does not unlock packs)
		[SerializeField] private bool				freeHints		= false;	// Hints won't deduct coins

		#endregion

		#region Member Variables

		private HashSet<string>						unlockedPacks;
		private Dictionary<string, int>				packLastCompletedLevel;
		private Dictionary<string, LevelSaveData>	levelSaveDatas;

		#endregion

		#region Properties

		public string			SaveId				{ get { return "game"; } }

		public List<BundleInfo>	BundleInfos			{ get { return bundleInfos; } }
		public int				HintCoinCost		{ get { return hintCoinCost; } }

		public PackInfo			ActivePackInfo		{ get; private set; }
		public LevelData		ActiveLevelData		{ get; private set; }
		public LevelSaveData	ActiveLevelSaveData	{ get; private set; }
		public int				NumLevelsCompleted	{ get; private set; }
		public int				NumLevelsTillAd		{ get; private set; }
		//public double			LastAdTimestamp		{ get; private set; }

		public bool IsActiveDataAvailable	{ get { return ActivePackInfo != null && ActiveLevelData != null && ActiveLevelSaveData != null; } }

		public bool DebugUnlockAllPacks		{ get { return Debug.isDebugBuild && unlockAllPacks; } }
		public bool DebugUnlockAllLevels	{ get { return Debug.isDebugBuild && unlockAllLevels; } }
		public bool DebugFreeHints			{ get { return Debug.isDebugBuild && freeHints; } }

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			SaveManager.Instance.Register(this);

			unlockedPacks			= new HashSet<string>();
			packLastCompletedLevel	= new Dictionary<string, int>();
			levelSaveDatas			= new Dictionary<string, LevelSaveData>();

			if (!LoadSave())
			{
				NumLevelsTillAd	= numLevelsBetweenAds;
				//LastAdTimestamp	= Utilities.SystemTimeInMilliseconds;
			}
		}

		private void OnDestroy()
		{
			Save();
		}

		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				Save();
			}
		}

		#endregion

		#region Public Variables

		/// <summary>
		/// Starts the level.
		/// </summary>
		public void StartLevel(PackInfo packInfo, LevelData levelData)
		{
			ActivePackInfo		= packInfo;
			ActiveLevelData		= levelData;
			ActiveLevelSaveData	= GetLevelSaveData(levelData);

			GameEventManager.Instance.SendEvent(GameEventManager.LevelStartedEventId);

			ScreenManager.Instance.Show("game");

			NumLevelsTillAd--;

			// Check if it's time to show an interstitial ad
			//&& Utilities.SystemTimeInMilliseconds - LastAdTimestamp >= minTimeBetweenAds * 1000
			if (NumLevelsTillAd <= 0 )
			{
                if (API.IsInterstitialAvailable())
                {
					API.ShowInterstitial();
					NumLevelsTillAd = numLevelsBetweenAds;
					//LastAdTimestamp = Utilities.SystemTimeInMilliseconds;
                }
            }
		}

		/// <summary>
		/// Plays the next level based on the current active PackInfo and LevelData
		/// </summary>
		public void NextLevel()
		{
			if (!IsActiveDataAvailable)
			{
				Debug.LogError("[GameManager] NextLevel | Data is null");
				return;
			}

			int nextLevelIndex = ActiveLevelData.LevelIndex + 1;

			if (nextLevelIndex < ActivePackInfo.LevelDatas.Count)
			{
				StartLevel(ActivePackInfo, ActivePackInfo.LevelDatas[nextLevelIndex]);
			}
		}

		/// <summary>
		/// Attempts to spend the coins required to use a hint, if the player has enough coins they are deducted and the hint is set in the save data
		/// </summary>
		public bool TryUseHint(out int polygonIndex)
		{
			polygonIndex = -1;

			if (!IsActiveDataAvailable)
			{
				Debug.LogError("[GameManager] TryUseHint | Data is null");
				return false;
			}

			List<int> polygonIndices = new List<int>();

			// First gather all possible polygons that can be shown as a hint
			for (int i = 0; i < ActiveLevelData.PolygonDatas.Count; i++)
			{
				// Check if this polygons hint is already displayed
				if (ActiveLevelSaveData.hintsDisplayed.Contains(i))
				{
					continue;
				}

				PolygonData polygonData = ActiveLevelData.PolygonDatas[i];

				// Check if the polygon has been placed on the board
				if (ActiveLevelSaveData.placedPositions.ContainsKey(i))
				{
					Vector2 placedPosition = ActiveLevelSaveData.placedPositions[i];

					// Check if the polygon is placed in the correct location
					if (placedPosition == polygonData.gridBounds.position)
					{
						// This polygon is placed in it's correct position, continue to the next polygon
						continue;
					}
				}

				// Polygon isn;t on the board or is placed in the wrong position
				polygonIndices.Add(i);
			}

			// There are no possible polygons to show a hint for
			if (polygonIndices.Count == 0)
			{
				return false;
			}

			// Try and spend a hint first, if that fails try and spend the coins
			if (DebugFreeHints || CurrencyManager.Instance.TrySpend("coins", hintCoinCost))
			{
				// Pick a random polygon to show the hint for
				polygonIndex = polygonIndices[Random.Range(0, polygonIndices.Count)];

				// Set the polygon index in the save data
				ActiveLevelSaveData.hintsDisplayed.Add(polygonIndex);

				return true;
			}
			else
			{
				// The player does not have enough hints or coins
				PopupManager.Instance.Show("not_enough_coins");
			}

			return false;
		}

		/// <summary>
		/// Sets the level save data and checks if the level is complete
		/// </summary>
		public void PolygonPlacedOnBoard(int polygonIndex, Vector2 gridPosition)
		{
			if (!IsActiveDataAvailable)
			{
				Debug.LogError("[GameManager] PolygonPlacedOnBoard | Data is null");
				return;
			}

			ActiveLevelSaveData.placedPositions[polygonIndex] = gridPosition;

			// If the number of placed polygons equals the total number of polygons then all the polygons have been placed on the board and the levle is complete
			if (ActiveLevelSaveData.placedPositions.Count == ActiveLevelData.PolygonDatas.Count)
			{
				ActiveLevelCompleted();
			}
		}

		/// <summary>
		/// Removes the placed polygon from the level save data
		/// </summary>
		public void PolygonRemovedFromBoard(int polygonIndex)
		{
			if (!IsActiveDataAvailable)
			{
				Debug.LogError("[GameManager] PolygonPlacedOnBoard | Data is null");
				return;
			}

			ActiveLevelSaveData.placedPositions.Remove(polygonIndex);
		}

		/// <summary>
		/// Clears the level save data values so that there is no placed polygons (Hints will still be saved)
		/// </summary>
		public void ResetActiveLevel()
		{
			if (!IsActiveDataAvailable)
			{
				Debug.LogError("[GameManager] ResetActiveLevel | Data is null");
				return;
			}

			ActiveLevelSaveData.placedPositions.Clear();
		}

		/// <summary>
		/// Returns true if the level has been completed atleast once
		/// </summary>
		public bool IsLevelCompleted(LevelData levelData)
		{
			if (!packLastCompletedLevel.ContainsKey(levelData.PackId))
			{
				return false;
			}

			return levelData.LevelIndex <= packLastCompletedLevel[levelData.PackId];
		}

		/// <summary>
		/// Returns true if the level is locked, false if it can be played
		/// </summary>
		public bool IsLevelLocked(LevelData levelData)
		{
			if (DebugUnlockAllLevels) return false;

			return levelData.LevelIndex > 0 && (!packLastCompletedLevel.ContainsKey(levelData.PackId) || levelData.LevelIndex > packLastCompletedLevel[levelData.PackId] + 1);
		}

		/// <summary>
		/// Returns true if the pack is locked
		/// </summary>
		public bool IsPackLocked(PackInfo packInfo)
		{
			if (DebugUnlockAllPacks) return false;

			switch (packInfo.unlockType)
			{
				case PackUnlockType.Coins:
					// Check if the player has unlocked the pack by spending coins
					return !unlockedPacks.Contains(packInfo.packId);
				case PackUnlockType.Stars:
					// Check if the player as earned enough stars to unlock the level 
					return CurrencyManager.Instance.GetAmount("stars") < packInfo.unlockAmount;
				case PackUnlockType.IAP:
					// Check if the player has purchased the iap product
					return IAPManager.Exists() && !IAPManager.Instance.IsProductPurchased(packInfo.unlockIAPProductId);
			}

			return false;
		}

		/// <summary>
		/// Unlocks the given pack
		/// </summary>
		public bool TryUnlockPackWithCoins(PackInfo packInfo)
		{
			if (CurrencyManager.Instance.TrySpend("coins", packInfo.unlockAmount))
			{
				unlockedPacks.Add(packInfo.packId);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the pack progress percentage
		/// </summary>
		public int GetNumCompletedLevels(PackInfo packInfo)
		{
			if (!packLastCompletedLevel.ContainsKey(packInfo.packId))
			{
				return 0;
			}

			return packLastCompletedLevel[packInfo.packId] + 1;
		}

		/// <summary>
		/// Gets the pack progress percentage
		/// </summary>
		public float GetPackProgress(PackInfo packInfo)
		{
			return (float)(GetNumCompletedLevels(packInfo)) / (float)packInfo.levelFiles.Count;
		}

		#endregion

		#region Private Variables

		/// <summary>
		/// Gets the LevelSaveData reference to use for the given level
		/// </summary>
		private LevelSaveData GetLevelSaveData(LevelData levelData)
		{
			LevelSaveData levelSaveData = null;

			// Check if the level has not been started and if there is loaded save data for it
			if (!levelSaveDatas.ContainsKey(levelData.Id))
			{
				levelSaveData = CreateLevelSaveData(levelData);
			}
			else
			{
				levelSaveData = levelSaveDatas[levelData.Id];

				// Check if the timestamps no longer match, if they don't then the level file has been changed 
				if (levelSaveData.timestamp != levelData.Timestamp)
				{
					// Remove the old LevelSaveData
					levelSaveDatas.Remove(levelData.Id);

					// Create a new one
					levelSaveData = CreateLevelSaveData(levelData);
				}
			}

			return levelSaveData;
		}

		/// <summary>
		/// Creates a new LevelSaveData for the given level
		/// </summary>
		private LevelSaveData CreateLevelSaveData(LevelData levelData)
		{
			LevelSaveData levelSaveData = new LevelSaveData(levelData);

			levelSaveDatas.Add(levelData.Id, levelSaveData);

			return levelSaveData;
		}

		/// <summary>
		/// Invoked by GameGrid when the active level has all the lines placed on the grid
		/// </summary>
		private void ActiveLevelCompleted()
		{
			int		lastLevelCompleted	= (packLastCompletedLevel.ContainsKey(ActiveLevelData.PackId) ? packLastCompletedLevel[ActiveLevelData.PackId] : -1);
			bool	firstTimeCompleting	= (ActiveLevelData.LevelIndex > lastLevelCompleted);
			int		fromRewardProgress	= NumLevelsCompleted;
			int		toRewardProgress	= NumLevelsCompleted;

			// If this is the first time they completed the level then give the player 1 star
			if (firstTimeCompleting)
			{
				NumLevelsCompleted++;
				toRewardProgress++;
				CurrencyManager.Instance.Give("stars", 1);
			}

			// If the player completed enough levels to earn
			if (NumLevelsCompleted >= numLevelsForReward)
			{
				NumLevelsCompleted = 0;
				CurrencyManager.Instance.Give("coins", numCoinsRewarded);
			}

			// Set the active level as completed
			SetLevelComplete(ActiveLevelData);

			// Remove the save data since it's only for levels which have been started but not completed
			levelSaveDatas.Remove(ActiveLevelData.Id);

			bool isLastLevel = (ActiveLevelData.LevelIndex == ActivePackInfo.LevelDatas.Count - 1);

			// Create the data object array to pass to the level complete popup
			object[] popupData = 
			{
				firstTimeCompleting,
				isLastLevel,
				fromRewardProgress,
				toRewardProgress,
				numLevelsForReward,
				numCoinsRewarded
			};

			SoundManager.Instance.Play("level-completed");


			// Show the level completed popup
			PopupManager.Instance.Show("level_complete", popupData, OnLevelCompletePopupClosed);

			// Send an active level completed game event 
			GameEventManager.Instance.SendEvent(GameEventManager.ActiveLevelCompletedEventId);
		}

		private void OnLevelCompletePopupClosed(bool cancelled, object[] data)
		{
			string action = data[0] as string;

			switch (action)
			{
				case "next_level":
					NextLevel();
					break;
				case "back_to_level_list":
					ScreenManager.Instance.Back();
					break;
				case "back_to_bundle_list":
					ScreenManager.Instance.BackTo("bundles");
					break;
			}
		}

		/// <summary>
		/// Sets the level status
		/// </summary>
		private void SetLevelComplete(LevelData levelData)
		{
			// Set the last completed level in the pack
			int curLastCompletedLevel = packLastCompletedLevel.ContainsKey(levelData.PackId) ? packLastCompletedLevel[levelData.PackId] : -1;

			if (levelData.LevelIndex > curLastCompletedLevel)
			{
				packLastCompletedLevel[levelData.PackId] = levelData.LevelIndex;
			}
		}

		public Dictionary<string, object> Save()
		{
			Dictionary<string, object> json = new Dictionary<string, object>();

			json["unlocked_packs"]			= new List<string>(unlockedPacks);
			json["last_completed"]			= SaveLastCompleteLevels();
			json["level_save_datas"]		= SaveLevelDatas();
			json["num_levels_till_ad"]		= NumLevelsTillAd;
			//json["last_ad_timestamp"]		= LastAdTimestamp;
			json["num_levels_completed"]	= NumLevelsCompleted;

			return json;
		}

		private List<object> SaveLastCompleteLevels()
		{
			List<object> json = new List<object>();

			foreach (KeyValuePair<string, int> pair in packLastCompletedLevel)
			{
				Dictionary<string, object> packJson = new Dictionary<string, object>();

				packJson["pack_id"]					= pair.Key;
				packJson["last_completed_level"]	= pair.Value;

				json.Add(packJson);
			}

			return json;
		}

		private List<object> SaveLevelDatas()
		{
			List<object> savedLevelDatas = new List<object>();

			foreach (KeyValuePair<string, LevelSaveData> pair in levelSaveDatas)
			{
				Dictionary<string, object> levelSaveDataJson = new Dictionary<string, object>();

				levelSaveDataJson["id"]		= pair.Key;
				levelSaveDataJson["data"]	= pair.Value.Save();

				savedLevelDatas.Add(levelSaveDataJson);
			}

			return savedLevelDatas;
		}

		private bool LoadSave()
		{
			JSONNode json = SaveManager.Instance.LoadSave(this);

			if (json == null)
			{
				return false;
			}

			LoadUnlockedPacks(json["unlocked_packs"].AsArray);
			LoadLastCompleteLevels(json["last_completed"].AsArray);
			LoadLevelSaveDatas(json["level_save_datas"].AsArray);

			NumLevelsTillAd		= json["num_levels_till_ad"].AsInt;
			//LastAdTimestamp		= json["last_ad_timestamp"].AsInt;
			NumLevelsCompleted	= json["num_levels_completed"].AsInt;

			return true;
		}

		private void LoadUnlockedPacks(JSONArray json)
		{
			for (int i = 0; i < json.Count; i++)
			{
				unlockedPacks.Add(json[i].Value);
			}
		}

		private void LoadLastCompleteLevels(JSONArray json)
		{
			for (int i = 0; i < json.Count; i++)
			{
				JSONNode childJson = json[i];

				string	packId				= childJson["pack_id"].Value;
				int		lastCompletedLevel	= childJson["last_completed_level"].AsInt;

				packLastCompletedLevel.Add(packId, lastCompletedLevel);
			}
		}

		/// <summary>
		/// Loads the game from the saved json file
		/// </summary>
		private void LoadLevelSaveDatas(JSONArray savedLevelDatasJson)
		{
			// Load all the placed line segments for levels that have progress
			for (int i = 0; i < savedLevelDatasJson.Count; i++)
			{
				JSONNode	savedLevelJson	= savedLevelDatasJson[i];
				string		levelId			= savedLevelJson["id"].Value;
				JSONNode	savedLevelData	= savedLevelJson["data"];

				LevelSaveData levelSaveData = new LevelSaveData(savedLevelData);

				levelSaveDatas.Add(levelId, levelSaveData);
			}
		}

		#endregion
	}
}
