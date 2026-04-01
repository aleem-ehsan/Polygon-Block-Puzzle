using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	public class LevelCompletePopup : Popup
	{
		#region Inspector Variables

		[Space]
		[SerializeField] private GameObject					coinRewardObject		= null;
		[SerializeField] private GameObject					nextLevelButton			= null;
		[SerializeField] private GameObject					backToMenuButton		= null;
		[Space]
		[SerializeField] private Image						starImage				= null;
		[SerializeField] private AnimationCurve				starEarnedAnimCurve		= null;
		[Space]
		[SerializeField] private ProgressBar				rewardProgressBar		= null;
		[SerializeField] private Text						rewardProgressText		= null;
		[SerializeField] private CanvasGroup				rewardCoinContainer		= null;
		[SerializeField] private Text						rewardCoinAmountText	= null;
		[SerializeField] private CoinAnimationController	coinAnimationController	= null;

		#endregion

		#region Member Variables

		private const float StarEarnedAnimDuration		= 0.75f;
		private const float RewardProgressAnimDuration	= 0.5f;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			int index = 0;

			bool	firstTimeCompleting	= (bool)inData[index++];
			bool	isLastLevel			= (bool)inData[index++];
			int		fromRewardProgress	= (int)inData[index++];
			int		toRewardProgress	= (int)inData[index++];
			int		numLevelsForReward	= (int)inData[index++];
			int		numCoinsRewarded	= (int)inData[index++];

			coinAnimationController.ResetUI();
			coinRewardObject.SetActive(true);
			rewardCoinContainer.alpha = 1f;

			nextLevelButton.SetActive(!isLastLevel);
			backToMenuButton.SetActive(isLastLevel);

			rewardCoinAmountText.text	= "x" + numCoinsRewarded;
			rewardProgressText.text		= string.Format("{0} / {1}", toRewardProgress, numLevelsForReward);

			// First time completing level, animate in the star and reward progress bar
			if (firstTimeCompleting)
			{
				float startDelay = animDuration + 0.25f;

				// Animate in the star
				PlayStarEarnedAnimation(startDelay);

				float fromProgress	= (float)fromRewardProgress / (float)numLevelsForReward;
				float toProgress	= (float)toRewardProgress / (float)numLevelsForReward;

				startDelay += StarEarnedAnimDuration + 0.25f;

				rewardProgressBar.SetProgressAnimated(fromProgress, toProgress, RewardProgressAnimDuration, startDelay);

				if (toRewardProgress == numLevelsForReward)
				{
					// Don't allow the player to exit the popup until the coin reward animation has finished
					SetPopupInteractable(false);

					startDelay += RewardProgressAnimDuration + 0.25f;
					
					// Play the coin animations after the progress bar has finished aniamting
					StartCoroutine(PlayCoinsAwardedAnimation(startDelay, numCoinsRewarded));
				}
			}
			// Level was already completed
			else
			{
				starImage.color					= new Color(starImage.color.r, starImage.color.g, starImage.color.b, 1f);
				starImage.transform.localScale	= Vector3.one;

				rewardProgressBar.SetProgress((float)fromRewardProgress / (float)numLevelsForReward);
			}
		}

		public override void OnHiding(bool cancelled)
		{
			base.OnHiding(cancelled);

			coinAnimationController.StopAllCoroutines();

			StopAllCoroutines();
		}

		#endregion

		#region Private Methods

		private void PlayStarEarnedAnimation(float startDelay)
		{
			UIAnimation anim;

			anim					= UIAnimation.ScaleX(starImage.transform as RectTransform, 2f, 1f, StarEarnedAnimDuration);
			anim.style				= UIAnimation.Style.Custom;
			anim.startOnFirstFrame	= true;
			anim.startDelay			= startDelay;
			anim.animationCurve		= starEarnedAnimCurve;
			anim.Play();

			anim					= UIAnimation.ScaleY(starImage.transform as RectTransform, 2f, 1f, StarEarnedAnimDuration);
			anim.style				= UIAnimation.Style.Custom;
			anim.startOnFirstFrame	= true;
			anim.startDelay			= startDelay;
			anim.animationCurve		= starEarnedAnimCurve;
			anim.Play();

			Color fromColor	= new Color(starImage.color.r, starImage.color.g, starImage.color.b, 0f);
			Color toColor	= new Color(starImage.color.r, starImage.color.g, starImage.color.b, 1f);

			anim					= UIAnimation.Color(starImage, fromColor, toColor, StarEarnedAnimDuration);
			anim.startOnFirstFrame	= true;
			anim.startDelay			= startDelay;
			anim.style				= UIAnimation.Style.EaseIn;
			anim.Play();
		}

		private IEnumerator PlayCoinsAwardedAnimation(float startDelay, int amountOfCoins)
		{
			// Wait before starting the coin animations
			yield return new WaitForSeconds(startDelay);

			UIAnimation.Alpha(rewardCoinContainer, 0f, 1f).Play();

			coinRewardObject.SetActive(false);

			coinAnimationController.Play(coinRewardObject, amountOfCoins, (int coin, int numCoins) => 
			{
				if (coin == numCoins)
				{
					SetPopupInteractable(true);
				}
			});
		}

		private void SetPopupInteractable(bool interactable)
		{
			CG.interactable		= interactable;
			CG.blocksRaycasts	= interactable;
		}

		#endregion
	}
}
