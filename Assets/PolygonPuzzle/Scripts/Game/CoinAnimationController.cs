using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob.PolygonPuzzle
{
	public class CoinAnimationController : MonoBehaviour
	{
		#region Inspector Variables
		
		[SerializeField] private RectTransform	animateToMarker				= null;
		[SerializeField] private RectTransform	coinAnimationContainer		= null;
		[SerializeField] private int			amountPerCoin				= 0;
		[SerializeField] private float			animationDuration			= 0;
		[SerializeField] private float			explodeAnimationDuration	= 0;
		[SerializeField] private float			minExpodeForce				= 0;
		[SerializeField] private float			maxExpodeForce				= 0;
		
		#endregion // Inspector Variables

		#region Member Variables
		
		private List<GameObject>		animatingCoins;
		private System.Action<int, int>	onCoinFinishedAnimating;
		private int						numCoinsFinished;
		private int						amountOfCoinsGiven;
		
		#endregion // Member Variables

		#region Public Methods
		
		public void Play(GameObject coinObject, int amount, System.Action<int, int> onCoinFinishedAnimating = null)
		{
			if (animatingCoins == null)
			{
				animatingCoins	= new List<GameObject>();
				amountPerCoin	= Mathf.Max(1, amountPerCoin);
			}

			this.amountOfCoinsGiven			= amount;
			this.onCoinFinishedAnimating	= onCoinFinishedAnimating;

			SoundManager.Instance.Play("coins_rewarded");

			AnimateCoins(coinObject, amount);
		}

		public void ResetUI()
		{
			StopAllCoroutines();

			if (animatingCoins != null)
			{
				for (int i = 0; i < animatingCoins.Count; i++)
				{
					Destroy(animatingCoins[i]);
				}

				animatingCoins.Clear();
			}
		}
		
		#endregion // Public Methods

		#region Private Methods
		
		/// <summary>
		/// Animates coins to the coin container
		/// </summary>
		private void AnimateCoins(GameObject coinObject, int amount)
		{
			int numCoinToAnimate = Mathf.CeilToInt((float)amount / (float)amountPerCoin);

			numCoinsFinished = 0;

			for (int i = 1; i <= numCoinToAnimate; i++)
			{
				StartCoroutine(AnimateCoin(coinObject, i, numCoinToAnimate));
			}
		}

		private IEnumerator AnimateCoin(GameObject coinObject, int index, int numCoinsAnimating)
		{
			RectTransform coinToAnimate = Instantiate(coinObject, coinAnimationContainer, false).transform as RectTransform;

			coinToAnimate.gameObject.SetActive(true);

			animatingCoins.Add(coinToAnimate.gameObject);

			UIAnimation.DestroyAllAnimations(coinToAnimate.gameObject);

			coinToAnimate.anchoredPosition	= Utilities.SwitchToRectTransform(coinObject.transform as RectTransform, coinAnimationContainer);
			coinToAnimate.sizeDelta			= (coinObject.transform as RectTransform).sizeDelta;

			yield return ExplodeCoinOut(coinToAnimate);
			
			Vector2 toPosition = Utilities.SwitchToRectTransform(animateToMarker, coinAnimationContainer);

			float duration = animationDuration + Random.Range(-0.1f, 0.1f);

			PlayAnimation(UIAnimation.Width(coinToAnimate, animateToMarker.sizeDelta.x, duration));
			PlayAnimation(UIAnimation.Height(coinToAnimate, animateToMarker.sizeDelta.y, duration));

			PlayAnimation(UIAnimation.PositionX(coinToAnimate, toPosition.x, duration));
			UIAnimation anim = PlayAnimation(UIAnimation.PositionY(coinToAnimate, toPosition.y, duration));

			anim.OnAnimationFinished += (GameObject target) =>
			{
				// A coin has finished animating to the marker
				animatingCoins.Remove(target);
				Destroy(target);
				numCoinsFinished++;

				SoundManager.Instance.Play("coin");

				if (onCoinFinishedAnimating != null)
				{
					onCoinFinishedAnimating(numCoinsFinished, numCoinsAnimating);
				}

				if (numCoinsFinished == numCoinsAnimating)
				{
					// If it's the last coin to finish animating then set the coin text to whatever the currency managers amount it
					CurrencyManager.Instance.UpdateCurrencyText("coins");
				}
				else
				{
					int coinAmount		= CurrencyManager.Instance.GetAmount("coins");
					int amountPerCoin	= amountOfCoinsGiven / numCoinsAnimating;

					coinAmount -= (numCoinsAnimating - numCoinsFinished) * amountPerCoin;

					// "Tick up" the coins amount
					CurrencyManager.Instance.UpdateCurrencyText("coins", coinAmount);
				}
			};
		}

		private IEnumerator ExplodeCoinOut(RectTransform coinToAnimate)
		{
			Vector2 randDir		= new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
			Vector2 toPosition	= coinToAnimate.anchoredPosition + randDir * Random.Range(minExpodeForce, maxExpodeForce);

			UIAnimation anim;

			anim		= UIAnimation.PositionX(coinToAnimate, toPosition.x, explodeAnimationDuration + Random.Range(-0.05f, 0.05f));
			anim.style	= UIAnimation.Style.EaseOut;
			anim.Play();

			anim		= UIAnimation.PositionY(coinToAnimate, toPosition.y, explodeAnimationDuration + Random.Range(-0.05f, 0.05f));
			anim.style	= UIAnimation.Style.EaseOut;
			anim.Play();

			while (anim.IsPlaying)
			{
				yield return null;
			}
		}

		/// <summary>
		/// Sets up and plays the UIAnimation for a coin
		/// </summary>
		private UIAnimation PlayAnimation(UIAnimation anim)
		{
			anim.style = UIAnimation.Style.EaseIn;

			anim.Play();

			return anim;
		}

		#endregion // Private Methods
	}
}
