using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	public class RewardAdGrantedPopup : Popup
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private GameObject					coinObject				= null;
		[SerializeField] private Text						amountText				= null;
		[SerializeField] private CoinAnimationController	coinAnimationController	= null;

		#endregion

		#region Member Variables
		
		private int amount;
		
		#endregion // Member Variables

		#region Unity Methods

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			amount = (int)inData[0];

			amountText.text = "x " + amount;
		}
		
		public override void OnHiding(bool cancelled)
		{
			coinAnimationController.Play(coinObject, amount);
		}

		#endregion
	}
}
