using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	public class MainScreen : Screen
	{
		#region Inspector Variables

		[SerializeField] private GameObject	removeAdsButton = null;

		#endregion

		#region Unity Methods

		protected override void Start()
		{

//			Debug.Log("USER CONSENT :" + Advertisements.Instance.UserConsentWasSet());
		//	Invoke("CheckForGDPR", 0.1f);

			base.Start();

			
		}


		//GDPR
		//void CheckForGDPR()
		//{
		//	if (Advertisements.Instance.UserConsentWasSet() == false)
		//	{
		//		PopupManager.Instance.ShowNoAd("consent");
		//	}


		//}

		////Popup events
		//public void OnUserClickAccept()
		//{
		//	Advertisements.Instance.SetUserConsent(true);

		//}

		//public void OnUserClickCancel()
		//{
		//	Advertisements.Instance.SetUserConsent(false);

		//}


		#endregion
	}
}
