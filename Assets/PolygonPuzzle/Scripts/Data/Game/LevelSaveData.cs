using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob.PolygonPuzzle
{
	public class LevelSaveData
	{
		#region Member Variables

		public string					timestamp;
		public Dictionary<int, Vector2>	placedPositions;
		public HashSet<int>				hintsDisplayed;

		#endregion

		#region Public Methods

		public LevelSaveData(LevelData levelData)
		{
			placedPositions	= new Dictionary<int, Vector2>();
			hintsDisplayed	= new HashSet<int>();
			timestamp		= levelData.Timestamp;
		}

		public LevelSaveData(JSONNode saveData)
		{
			placedPositions	= new Dictionary<int, Vector2>();
			hintsDisplayed	= new HashSet<int>();

			LoadSave(saveData);
		}

		public Dictionary<string, object> Save()
		{
			List<object> savedPlacedPositions = new List<object>();

			foreach (KeyValuePair<int, Vector2> pair in placedPositions)
			{
				Dictionary<string, object> savedPlacedPosition = new Dictionary<string, object>();

				savedPlacedPosition["index"]	= pair.Key;
				savedPlacedPosition["x"]		= pair.Value.x;
				savedPlacedPosition["y"]		= pair.Value.y;

				savedPlacedPositions.Add(savedPlacedPosition);
			}

			Dictionary<string, object>	saveData = new Dictionary<string, object>();

			saveData["timestamp"]			= timestamp;
			saveData["placed_positions"]	= savedPlacedPositions;
			saveData["hints_displayed"]		= new List<int>(hintsDisplayed);

			return saveData;
		}

		public void LoadSave(JSONNode saveData)
		{
			timestamp = saveData["timestamp"].Value;

			// Load the saved placed positions
			JSONArray savedPlacedPositions = saveData["placed_positions"].AsArray;

			foreach (JSONNode savedPlacedPosition in savedPlacedPositions)
			{
				int		index	= savedPlacedPosition["index"].AsInt;
				float	x		= savedPlacedPosition["x"].AsFloat;
				float	y		= savedPlacedPosition["y"].AsFloat;

				placedPositions.Add(index, new Vector2(x, y));
			}

			// Load the hints that are displayed
			JSONArray savedHintsDisplayed = saveData["hints_displayed"].AsArray;

			foreach (JSONNode savedHintDisplayed in savedHintsDisplayed)
			{
				hintsDisplayed.Add(savedHintDisplayed.AsInt);
			}
		}

		#endregion
	}
}
