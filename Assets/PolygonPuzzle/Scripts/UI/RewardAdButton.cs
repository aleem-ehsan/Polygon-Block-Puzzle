using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gley.MobileAds;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	[RequireComponent(typeof(Button))]
	public class RewardAdButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string		currencyId				= "";
		[SerializeField] private int		amountToReward			= 0;
		[SerializeField] private GameObject	uiContainer				= null;
		[SerializeField] private bool		testInEditor			= false;

		[Space]

		[SerializeField] private bool	showOnlyWhenCurrencyIsLow	= false;
		[SerializeField] private int	currencyShowTheshold		= 0;

		[Space]

		[SerializeField] private bool	showRewardGrantedPopup		= false;
		[SerializeField] private string	rewardGrantedPopupId		= "";

		#endregion

		#region Unity Methods

		private void Start()
		{
			//uiContainer.SetActive(false);

			//bool areRewardAdsEnabled = MobileAdsManager.Instance.AreRewardAdsEnabled;

			//#if UNITY_EDITOR
			//areRewardAdsEnabled = testInEditor;
			//#endif

			//if (areRewardAdsEnabled)
			//{
			//	UpdateUI();

			//	MobileAdsManager.Instance.OnRewardAdLoaded	+= UpdateUI;
			//	MobileAdsManager.Instance.OnAdsRemoved		+= OnAdsRemoved;
			//	CurrencyManager.Instance.OnCurrencyChanged	+= OnCurrencyChanged;

				gameObject.GetComponent<Button>().onClick.AddListener(OnClicked);
			//}
		}

		#endregion

		#region Private Methods

		private void OnCurrencyChanged(string changedCurrencyId)
		{
			if (currencyId == changedCurrencyId)
			{
				UpdateUI();
			}
		}

		private void UpdateUI()
		{
			//bool rewardAdLoded		= MobileAdsManager.Instance.RewardAdState == AdNetworkHandler.AdState.Loaded;
			bool passShowThreshold	= (!showOnlyWhenCurrencyIsLow || CurrencyManager.Instance.GetAmount(currencyId) <= currencyShowTheshold);

			//uiContainer.SetActive(rewardAdLoded && passShowThreshold);

			#if UNITY_EDITOR
			if (testInEditor)
			{
				uiContainer.SetActive(passShowThreshold);
			}
			#endif
		}

		private void OnAdsRemoved()
		{
			//MobileAdsManager.Instance.OnRewardAdLoaded	-= UpdateUI;
			//MobileAdsManager.Instance.OnAdsRemoved		-= OnAdsRemoved;
			CurrencyManager.Instance.OnCurrencyChanged	-= OnCurrencyChanged;

			uiContainer.SetActive(false);
		}

		private void OnClicked()
		{
			//#if UNITY_EDITOR
			if (testInEditor)
			{
				OnRewardAdGranted("", 0);

				return;
			}
			//#endif

			//uiContainer.SetActive(false);

			API.ShowRewardedVideo(CompleteMethod);

			
		}

		private void CompleteMethod(bool completed)
		{
			if (completed == true)
			{
				//Debug.Log("Chay vao day");
				CurrencyManager.Instance.Give(currencyId, amountToReward);

				if (showRewardGrantedPopup)
				{
					object[] popupData =
					{
					amountToReward
				};

					// Show a reward ad granted popup
					PopupManager.Instance.Show(rewardGrantedPopupId, popupData);
				}
				else
				{
					// If no reward ad granted popup will appear then update the currency text right away
					CurrencyManager.Instance.UpdateCurrencyText(currencyId);
				}
			}
			else
			{
				Debug.Log("NO REWARD");
			}
		}

        private void OnRewardAdGranted(string id, double amount)
		{
			// Increment the currency right now
			CurrencyManager.Instance.Give(currencyId, amountToReward);

			if (showRewardGrantedPopup)
			{
				object[] popupData =
				{
					amountToReward
				};

				// Show a reward ad granted popup
				PopupManager.Instance.Show(rewardGrantedPopupId, popupData);
			}
			else
			{
				// If no reward ad granted popup will appear then update the currency text right away
				CurrencyManager.Instance.UpdateCurrencyText(currencyId);
			}
		}

		#endregion
	}
}
