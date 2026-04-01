using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if DM_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension; 
#endif

namespace dotmob
{
	public class IAPTestScene : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private IAPTestButton	testButtonPrefab	= null;
		[SerializeField] private Transform		testButtonContainer	= null;

		#endregion

		#region Member Variables

		private ObjectPool testButtonPool = null;

		#endregion

		#region Unity Methods

		private void Start()
		{
			if (IAPSettings.IsIAPEnabled)
			{
				testButtonPool = new ObjectPool(testButtonPrefab.gameObject, 1, testButtonContainer);

				if (IAPManager.Instance.IsInitialized)
				{
					SetupButtons();
				}
				else
				{
					IAPManager.Instance.OnIAPInitialized += OnIAPInitialized;
				}

				IAPManager.Instance.OnProductPurchased += OnProductPurchased;
			}
			else
			{
				Logger.LogError("IAPTestScene", "IAP is not enabled. Please open the IAP Settings window in Unity and enable IAP.");
			}
		}

		#endregion

		#region Private Methods

		private void OnIAPInitialized(bool success)
		{
			SetupButtons();
		}

		private void SetupButtons()
		{
			testButtonPool.ReturnAllObjectsToPool();

			for (int i = 0; i < IAPSettings.Instance.productInfos.Count; i++)
			{
				IAPSettings.ProductInfo	productInfo	= IAPSettings.Instance.productInfos[i];
				IAPTestButton			testButton	= testButtonPool.GetObject<IAPTestButton>();

				SetupTestButton(productInfo.productId, testButton);
			}
		}

		private void SetupTestButton(string productId, IAPTestButton testButton)
		{
			testButton.idText.text = productId;

			if (IAPSettings.IsIAPEnabled)
			{
#if DM_IAP

				Product product = IAPManager.Instance.GetProductInformation(productId);

				if (product == null)
				{
					SetErrorText(testButton, "Product does not exist");
				}
				else
				{
					if (!product.availableToPurchase)
					{
						SetErrorText(testButton, "Product is not available to purchase");
					}
					else
					{
						testButton.errorText.gameObject.SetActive(false);
					}

					testButton.nameText.gameObject.SetActive(true);
					testButton.descText.gameObject.SetActive(true);
					testButton.priceText.gameObject.SetActive(true);
					testButton.consumableText.gameObject.SetActive(true);

					testButton.nameText.text		= "Title: " + product.metadata.localizedTitle;
					testButton.descText.text		= "Description: " + product.metadata.localizedDescription;
					testButton.priceText.text		= "Price: " + product.metadata.localizedPriceString;
					testButton.consumableText.text	= "Type: " + product.definition.type + (product.definition.type != ProductType.Consumable ? " - Purchased: " + IAPManager.Instance.IsProductPurchased(productId) : "");

					testButton.Data					= productId;
					testButton.OnListItemClicked	= OnButtonClicked;
				}

#endif
			}
			else
			{
				SetErrorText(testButton, "IAP is not enabled");
			}
		}

		private void SetErrorText(IAPTestButton testButton, string message)
		{
			testButton.errorText.text = message;

			testButton.errorText.gameObject.SetActive(true);
			testButton.nameText.gameObject.SetActive(false);
			testButton.descText.gameObject.SetActive(false);
			testButton.priceText.gameObject.SetActive(false);
			testButton.consumableText.gameObject.SetActive(false);

			testButton.OnListItemClicked = null;
		}

		private void OnButtonClicked(int index, object data)
		{
#if DM_IAP

			IAPManager.Instance.BuyProduct(data as string);

#endif
		}

		private void OnProductPurchased(string productId)
		{
			// Refresh the buttons
			SetupButtons();
		}

		#endregion
	}
}
