using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if BBG_UNITYADS
using UnityEngine.Monetization;
#endif

namespace dotmob
{
	public class UnityAdsEventListener : MonoBehaviour
	{
		#if BBG_UNITYADS

		#region Member Variables

		private static object interstitialLock	= new object();
		private static object rewardLock		= new object();

		private bool		awaitingInterstitialEvent;
		private bool		awaitingRewardEvent;

		private bool		hasInterstitialResult;
		private bool		hasRewardResult;

		private ShowResult	interstitialShowResult;
		private ShowResult	rewardShowResult;

		#endregion

		#region Properties
		
		public System.Action<ShowResult> OnInterstitialAdFinished	{ get; set; }
		public System.Action<ShowResult> OnRewardAdFinished		{ get; set; }

		#endregion

		#region Unity Methods

		private void Update()
		{
			// Check if we are waiting for the interstitial ad to finish
			if (awaitingInterstitialEvent)
			{
				lock (interstitialLock)
				{
					// Check if we have the interstitial ad finish result
					if (hasInterstitialResult)
					{
						// Call the event on the main thread
						OnInterstitialAdFinished(interstitialShowResult);

						awaitingInterstitialEvent	= false;
						hasInterstitialResult		= false;
					}
				}
			}

			if (awaitingRewardEvent)
			{
				// Check if we are waiting for the reward ad to finish
				lock (rewardLock)
				{
					// Check if we have the reward ad finish result
					if (hasRewardResult)
					{
						// Call the event on the main thread
						OnRewardAdFinished(rewardShowResult);

						awaitingRewardEvent	= false;
						hasRewardResult		= false;
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void InterstitialAdShown()
		{
			awaitingInterstitialEvent	= true;
			hasInterstitialResult		= false;
		}

		public void RewardAdShown()
		{
			awaitingRewardEvent		= true;
			hasInterstitialResult	= false;
		}

		public void InterstitialAdFinished(ShowResult showResult)
		{
			lock (interstitialLock)
			{
				hasInterstitialResult	= true;
				interstitialShowResult	= showResult;
			}
		}

		public void RewardAdFinished(ShowResult showResult)
		{
			lock (rewardLock)
			{
				hasRewardResult		= true;
				rewardShowResult	= showResult;
			}
		}

		#endregion

		#endif
	}
}
