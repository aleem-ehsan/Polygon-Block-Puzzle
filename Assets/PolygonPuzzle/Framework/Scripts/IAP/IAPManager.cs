using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

#if DM_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension; 
#endif

#pragma warning disable 0414 // Reason: Some inspector variables are only used in specific platforms and their usages are removed using #if blocks

namespace dotmob
{
	public class IAPManager : SingletonComponent<IAPManager>, ISaveable
#if DM_IAP
	, IStoreListener
#endif
	{
		#region Classes

		[System.Serializable]
		public class OnProductPurchasedEvent : UnityEngine.Events.UnityEvent {}

		[System.Serializable]
		public class PurchaseEvent
		{
			public OnProductPurchasedEvent onProductPurchasedEvent = null;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private List<PurchaseEvent> purchaseEvents = null;

		#endregion

		#region Member Variables

		private const string LogTag = "IAPManager";

#if DM_IAP
		private IStoreController	storeController;
		private IExtensionProvider 	extensionProvider;
#endif

		private HashSet<string> purchasedNonConsumables;

		#endregion

		#region Properties

		/// <summary>
		/// Save data id
		/// </summary>
		public string SaveId { get { return "iap_manager"; } }

		/// <summary>
		/// Callback that is invoked when the IAPManager has successfully initialized and has retrieved the list of products/prices
		/// </summary>
		public System.Action<bool> OnIAPInitialized { get; set; }

		/// <summary>
		/// Callback that is invoked when a product is purchased, passes the product id that was purchased
		/// </summary>
		public System.Action<string> OnProductPurchased { get; set; }

		/// <summary>
		/// Returns true if IAP is initialized
		/// </summary>
		public bool IsInitialized
		{
#if DM_IAP
			get { return storeController != null && extensionProvider != null; }
#else
			get { return false; }
			#endif
		}

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			SaveManager.Instance.Register(this);

			purchasedNonConsumables	= new HashSet<string>();

			LoadSave();
		}

		private void Start()
		{
#if DM_IAP

			// Initialize IAP
			ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			// Add all the product ids to teh builder
			for (int i = 0; i < IAPSettings.Instance.productInfos.Count; i++)
			{
				IAPSettings.ProductInfo productInfo = IAPSettings.Instance.productInfos[i];

				Logger.Log(LogTag, "Adding product to builder, id: " + productInfo.productId + ", consumable: " + productInfo.consumable);

				builder.AddProduct(productInfo.productId, productInfo.consumable ? ProductType.Consumable : ProductType.NonConsumable);
			}

			Logger.Log(LogTag, "Initializing IAP now...");

			UnityPurchasing.Initialize(this, builder);

#endif
		}

		#endregion

		#region Public Methods

#if DM_IAP

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			Logger.Log(LogTag, "Initialization successful!");

			storeController		= controller;
			extensionProvider	= extensions;

			if (OnIAPInitialized != null)
			{
				OnIAPInitialized(true);
			}
		}

		public void OnInitializeFailed(InitializationFailureReason error, string? message = null)
		{
			var errorMessage = $"Purchasing failed to initialize. Reason: {error}.";

			if (message != null)
			{
				errorMessage += $" More details: {message}";
			}

			Debug.Log(errorMessage);
		}

		public void OnInitializeFailed(InitializationFailureReason failureReason)
		{
			Logger.LogError(LogTag, "Initializion failed! Reason: " + failureReason);

			if (OnIAPInitialized != null)
			{
				OnIAPInitialized(false);
			}
		}

		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			Logger.LogError(LogTag, "Purchase failed for product id: " + product.definition.id + ", reason: " + failureReason);
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
		{
			Product product = args.purchasedProduct;

			Logger.Log(LogTag, "Purchase successful for product id: " + product.definition.id);

			SetProductPurchased(product);

			return PurchaseProcessingResult.Complete;
		}

		/// <summary>
		/// Starts the buying process for the given product id
		/// </summary>
		public void BuyProduct(string productId)
		{
			Logger.Log(LogTag, "BuyProduct: Purchasing product with id: " + productId);

			if (IsInitialized)
			{
				Product product = storeController.products.WithID(productId);

				// If the look up found a product for this device's store and that product is ready to be sold ... 
				if (product == null)
				{
					Logger.LogError(LogTag, "BuyProduct: product with id \"" + productId + "\" does not exist.");
				}
				else if (!product.availableToPurchase)
				{
					Logger.LogError(LogTag, "BuyProduct: product with id \"" + productId + "\" is not available to purchase.");
				}
				else
				{
					storeController.InitiatePurchase(product);
				}
			}
			else
			{
				Logger.LogWarning(LogTag, "BuyProduct: IAPManager not initialized.");
			}
		}

		/// <summary>
		/// Sets the given Product as purchased in the IAPManager, this also invokes and events registered to the product
		/// </summary>
		public void SetProductPurchased(Product product)
		{
			if (product.definition.type != ProductType.Consumable)
			{
				purchasedNonConsumables.Add(product.definition.id);
			}

			if (OnProductPurchased != null)
			{
				OnProductPurchased(product.definition.id);
			}

			List<IAPSettings.ProductInfo> productInfos = IAPSettings.Instance.productInfos;

			for (int i = 0; i < productInfos.Count; i++)
			{
				IAPSettings.ProductInfo productInfo = productInfos[i];

				if (productInfo.productId == product.definition.id && purchaseEvents[i].onProductPurchasedEvent != null)
				{
					purchaseEvents[i].onProductPurchasedEvent.Invoke();
				}
			}
		}

		/// <summary>
		/// Sets the given product as purchased if it is an available product
		/// </summary>
		public void SetProductPurchased(string productId)
		{
			Product product = GetProductInformation(productId);

			if (product != null)
			{
				SetProductPurchased(product);
			}
		}

		/// <summary>
		/// Gets the products store information
		/// </summary>
		public Product GetProductInformation(string productId)
		{
			if (IsInitialized)
			{
				return storeController.products.WithID(productId);
			}

			return null;
		}

#endif

		/// <summary>
		/// Returns true if the given product id has been purchased, only for non-consumable products, consumable products will always return false.
		/// </summary>
		public bool IsProductPurchased(string productId)
		{
			return purchasedNonConsumables.Contains(productId);
		}

		/// <summary>
		/// Restores the purchases if platform is iOS or OSX
		/// </summary>
		public void RestorePurchases()
		{
			Logger.Log(LogTag, "RestorePurchases: Restoring purchases");

#if DM_IAP
			if (IsInitialized)
			{
				if ((Application.platform == RuntimePlatform.IPhonePlayer ||
				     Application.platform == RuntimePlatform.OSXPlayer))
				{
					extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions((result) => {});
				}
				else
				{
					Logger.LogWarning(LogTag, "RestorePurchases: Device is not iOS, no need to call this method.");
				}
			}
			else
			{
				Logger.LogWarning(LogTag, "RestorePurchases: IAPManager not initialized.");
			}
#endif
		}

		#endregion

		#region Save Methods

		public Dictionary<string, object> Save()
		{
			Dictionary<string, object> json = new Dictionary<string, object>();

			json["purchases"] = new List<string>(purchasedNonConsumables);

			return json;
		}

		public bool LoadSave()
		{
			JSONNode json = SaveManager.Instance.LoadSave(this);

			if (json == null)
			{
				return false;
			}


			JSONArray purchasesJson = json["purchases"].AsArray;

			for (int i = 0; i < purchasesJson.Count; i++)
			{
				purchasedNonConsumables.Add(purchasesJson[i].Value);
			}

			return true;
		}

		#endregion
	}
}