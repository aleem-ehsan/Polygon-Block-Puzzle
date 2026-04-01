using System.Collections;
using System.Collections.Generic;
using Gley.MobileAds;
using UnityEngine;
using UnityEngine.Networking;

namespace dotmob
{
	public class MobileAdsManager : MonoBehaviour
	{

		private void Start()
		{
			//Debug.Log("Chay vao day dau tien");
			API.Initialize(OnInitialized);
			//Advertisements.Instance.ShowInterstitial()

		}

		private void OnInitialized()
		{
			API.ShowBanner(BannerPosition.Bottom, BannerType.Banner);

			if (!API.GDPRConsentWasSet())
			{
				API.ShowBuiltInConsentPopup(PopupCloseds);
			}
		}

		private void PopupCloseds()
		{

		}

		///// <summary>
		///// Removes ads for this user
		///// </summary>
		public void RemoveAds()
		{

			API.RemoveAds(true);

		}


		private void OnApplicationPause(bool pause)
		{
			if (pause == false)
			{
			API.ShowAppOpen();
			}
		}

	}
}
