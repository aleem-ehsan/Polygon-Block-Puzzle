using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	public class GameScreen : Screen
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private GameArea	gameArea		= null;
		[SerializeField] private Text		hintCostText	= null;

		#endregion // Inspector Variables

		#region Public Methods
		
		public override void Initialize()
		{
			base.Initialize();

			hintCostText.text = GameManager.Instance.HintCoinCost.ToString();

			gameArea.Initialize();

			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelStartedEventId, OnLevelStarted);
		}

		/// <summary>
		/// Invoked when the Reset button on the GameScreen is clicked
		/// </summary>
		public void OnResetClicked()
		{
			// Reset the LevelSaveData for the active level so no shapes are placed
			GameManager.Instance.ResetActiveLevel();

			// Just re-stup the game area so all the shapes will be placed back in the shapes container
			SetupGameArea();
		}

		/// <summary>
		/// Invoked when the Hint button on the GameScreen is clicked
		/// </summary>
		public void OnHintClicked()
		{
			int polygonIndex;

			// Try and spend a hint/coins for the hint
			if (GameManager.Instance.TryUseHint(out polygonIndex))
			{
				// Currency has been spend and a polygon has been selected to display
				gameArea.DisplayHint(polygonIndex);
				SoundManager.Instance.Play("hint-used");
			}
		}
		
		#endregion // Public Methods

		#region Private Methods
		
		private void OnLevelStarted(string eventId, object[] data)
		{
			SetupGameArea();
		}

		private void SetupGameArea()
		{
			LevelData		activeLevelData		= GameManager.Instance.ActiveLevelData;
			LevelSaveData	activeLevelSaveData	= GameManager.Instance.ActiveLevelSaveData;

			if (activeLevelData != null && activeLevelSaveData != null)
			{
				gameArea.SetupLevel(activeLevelData, activeLevelSaveData);
			}
		}
		
		#endregion // Private Methods
	}
}
