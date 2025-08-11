using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class SideloadAPK : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string developerKey = "";
    [SerializeField] private string appOpenAdId = "";
    
    private const string FIRST_LAUNCH_KEY = "SideloadAPK_FirstLaunch";
    private const string INSTANCE_ID_KEY = "SideloadAPK_InstanceID";
    private const string API_ENDPOINT = "https://sideloadapk.com/api";

    private string deviceFingerprint;
    private GameObject adCanvas;
    private bool adShown = false;

    void Awake()
    {
        if (string.IsNullOrEmpty(developerKey))
        {
            Debug.LogError("SideloadAPK: Developer Key must be set in the Inspector!");
            return;
        }

        // Ensure singleton behavior
        if (FindObjectsByType<SideloadAPK>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        InitializeAnalytics();
        
        if (!string.IsNullOrEmpty(appOpenAdId))
        {
            StartCoroutine(ShowAppOpenAdAfterDelay());
        }
    }

    private void InitializeAnalytics()
    {
        // Generate or retrieve instance ID
        string instanceId = PlayerPrefs.GetString(INSTANCE_ID_KEY, "");
        if (string.IsNullOrEmpty(instanceId))
        {
            instanceId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(INSTANCE_ID_KEY, instanceId);
            PlayerPrefs.Save();
        }

        // Generate device fingerprint
        deviceFingerprint = GenerateDeviceFingerprint(instanceId);

        // Check if first launch and if running on Android (not in Unity Editor)
        bool isFirstLaunch = !PlayerPrefs.HasKey(FIRST_LAUNCH_KEY);
        bool isAndroidDevice = Application.platform == RuntimePlatform.Android;
        
        if (isFirstLaunch)
        {
            PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
            PlayerPrefs.Save();

            // Only send install event on Android devices (not in Unity Editor)
            if (isAndroidDevice)
            {
                StartCoroutine(SendInstallEvent());
            }
            else
            {
                Debug.Log("SideloadAPK: Install event skipped - not running on Android device");
            }
        }
    }

    private string GenerateDeviceFingerprint(string instanceId)
    {
        string rawData = instanceId;
        rawData += SystemInfo.deviceModel;
        rawData += SystemInfo.deviceType.ToString();
        rawData += SystemInfo.operatingSystem;

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    private IEnumerator SendInstallEvent()
    {
        Debug.Log("SideloadAPK: Preparing to send install event");
        
        string url = API_ENDPOINT + "/installs/" + developerKey;
        Debug.Log($"SideloadAPK: Sending to URL: {url}");
        
        var payload = new InstallEventPayload
        {
            developerKey = this.developerKey,
            deviceFingerprint = this.deviceFingerprint,
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log($"SideloadAPK: JSON payload: {json}");
        
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("SideloadAPK: Install event sent successfully");
            }
            else
            {
                Debug.LogError($"SideloadAPK: Failed to send install event: {request.error}");
            }
        }
    }

    private IEnumerator ShowAppOpenAdAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        
        if (!adShown)
        {
            StartCoroutine(LoadAndShowAppOpenAd());
        }
    }

    private IEnumerator LoadAndShowAppOpenAd()
    {
        Debug.Log("SideloadAPK: Loading app open ad...");
        
        string url = API_ENDPOINT + "/ads/app-open";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    AppOpenAdData adData = JsonUtility.FromJson<AppOpenAdData>(request.downloadHandler.text);
                    CreateAndShowAd(adData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"SideloadAPK: Failed to parse ad data: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"SideloadAPK: Failed to load ad: {request.error}");
            }
        }
    }

    private void CreateAndShowAd(AppOpenAdData adData)
    {
        if (adShown) return;
        adShown = true;

        // Create Canvas
        GameObject canvasGO = new GameObject("SideloadAPK_AdCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create semi-transparent background with blur effect
        GameObject background = CreateBackground(canvas.transform);
        
        // Create main ad container with better proportions
        GameObject adContainer = CreateAdContainer(canvas.transform);
        
        // Create close button (repositioned to top-right corner)
        GameObject closeButton = CreateCloseButton(adContainer.transform);
        closeButton.GetComponent<Button>().onClick.AddListener(() => CloseAd());
        
        // Create app info section with better layout
        GameObject appInfo = CreateAppInfoSection(adContainer.transform, adData.name);
        GameObject appIcon = appInfo.transform.Find("AppIcon").gameObject;
        
        // Create screenshot section with proper aspect ratio and rounded corners
        GameObject screenshotContainer = CreateScreenshotContainer(adContainer.transform);
        GameObject screenshot = screenshotContainer.transform.Find("Screenshot").gameObject;
        
        // Create CTA button with better styling
        GameObject ctaButton = CreateCTAButton(adContainer.transform);
        ctaButton.GetComponent<Button>().onClick.AddListener(() => OnCTAClick(adData.id));
        
        adCanvas = canvasGO;
        
        // Load images
        StartCoroutine(LoadIconImage(adData.iconUrl, appIcon.GetComponent<Image>()));
        StartCoroutine(LoadScreenshotImage(adData.screenshotUrl, screenshot.GetComponent<Image>()));
    }

    private GameObject CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);
        
        Image image = bg.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f); // Darker overlay for better contrast
        
        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        return bg;
    }

    private GameObject CreateAdContainer(Transform parent)
    {
        GameObject container = new GameObject("AdContainer");
        container.transform.SetParent(parent, false);
        
        Image image = container.AddComponent<Image>();
        image.color = Color.white;
        
        // Add more subtle shadow effect
        Shadow shadow = container.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.2f);
        shadow.effectDistance = new Vector2(0, -6);
        
        // Better proportions - less padding, more content
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.25f);
        rect.anchorMax = new Vector2(0.95f, 0.75f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        return container;
    }

    private GameObject CreateCloseButton(Transform parent)
    {
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(parent, false);
        
        Image image = closeBtn.AddComponent<Image>();
        image.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        
        Button button = closeBtn.AddComponent<Button>();
        
        // Add subtle hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;
        
        // Add X text with better styling
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(closeBtn.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.text = "✕";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Position at top-right corner
        RectTransform rect = closeBtn.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(40, 40);
        rect.anchoredPosition = new Vector2(-20, -20);
        
        return closeBtn;
    }

    private GameObject CreateAppInfoSection(Transform parent, string appName)
    {
        GameObject infoSection = new GameObject("AppInfoSection");
        infoSection.transform.SetParent(parent, false);
        
        RectTransform rect = infoSection.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.75f);
        rect.anchorMax = new Vector2(1, 0.95f);
        rect.offsetMin = new Vector2(20, 0); // Add some margin from edges
        rect.offsetMax = new Vector2(-20, -10);
        
        // Create app icon with proper aspect ratio
        GameObject icon = new GameObject("AppIcon");
        icon.transform.SetParent(infoSection.transform, false);
        
        Image iconImage = icon.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.preserveAspect = true; // Maintain aspect ratio
        
        // Add subtle border to icon
        Shadow iconShadow = icon.AddComponent<Shadow>();
        iconShadow.effectColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        iconShadow.effectDistance = new Vector2(1, -1);
        
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.sizeDelta = new Vector2(80, 80);
        iconRect.anchoredPosition = new Vector2(40, 0);
        
        // Create app name with better typography
        GameObject nameGO = new GameObject("AppName");
        nameGO.transform.SetParent(infoSection.transform, false);
        
        Text text = nameGO.AddComponent<Text>();
        text.text = appName;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32; // Larger font size
        text.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        
        // Add text shadow for better readability
        Shadow textShadow = nameGO.AddComponent<Shadow>();
        textShadow.effectColor = new Color(1f, 1f, 1f, 0.5f);
        textShadow.effectDistance = new Vector2(1, -1);
        
        RectTransform nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(100, 0); // Space for icon
        nameRect.offsetMax = new Vector2(-20, 0);
        
        return infoSection;
    }

    private GameObject CreateScreenshotContainer(Transform parent)
    {
        GameObject container = new GameObject("ScreenshotContainer");
        container.transform.SetParent(parent, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0.2f);
        containerRect.anchorMax = new Vector2(1, 0.75f);
        containerRect.offsetMin = new Vector2(20, 0);
        containerRect.offsetMax = new Vector2(-20, -10);
        
        // Add background for screenshot area
        Image containerBg = container.AddComponent<Image>();
        containerBg.color = new Color(0.98f, 0.98f, 0.98f, 1f);
        
        // Create screenshot image
        GameObject screenshot = new GameObject("Screenshot");
        screenshot.transform.SetParent(container.transform, false);
        
        Image screenshotImage = screenshot.AddComponent<Image>();
        screenshotImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        screenshotImage.preserveAspect = true;
        
        RectTransform screenshotRect = screenshot.GetComponent<RectTransform>();
        screenshotRect.anchorMin = new Vector2(0.1f, 0.1f);
        screenshotRect.anchorMax = new Vector2(0.9f, 0.9f);
        screenshotRect.offsetMin = Vector2.zero;
        screenshotRect.offsetMax = Vector2.zero;
        
        return container;
    }

    private GameObject CreateCTAButton(Transform parent)
    {
        GameObject ctaBtn = new GameObject("CTAButton");
        ctaBtn.transform.SetParent(parent, false);
        
        Image image = ctaBtn.AddComponent<Image>();
        image.color = new Color(0.13f, 0.75f, 0.4f, 1f); // Modern green
        
        Button button = ctaBtn.AddComponent<Button>();
        
        // Better button color states
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.13f, 0.75f, 0.4f, 1f);
        colors.highlightedColor = new Color(0.1f, 0.65f, 0.35f, 1f);
        colors.pressedColor = new Color(0.08f, 0.55f, 0.3f, 1f);
        button.colors = colors;
        
        // Add subtle shadow to button
        Shadow buttonShadow = ctaBtn.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0, 0, 0, 0.3f);
        buttonShadow.effectDistance = new Vector2(0, -3);
        
        // Add button text with better styling
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(ctaBtn.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.text = "INSTALL NOW";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.color = Color.white;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        
        // Add text shadow for better contrast
        Shadow textOutline = textGO.AddComponent<Shadow>();
        textOutline.effectColor = new Color(0, 0, 0, 0.3f);
        textOutline.effectDistance = new Vector2(1, -1);
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Better button positioning and sizing
        RectTransform rect = ctaBtn.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.05f);
        rect.anchorMax = new Vector2(0.85f, 0.18f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        return ctaBtn;
    }

    private IEnumerator LoadIconImage(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                targetImage.sprite = sprite;
            }
            else
            {
                Debug.LogError($"SideloadAPK: Failed to load icon from {url}: {request.error}");
            }
        }
    }

    private IEnumerator LoadScreenshotImage(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                targetImage.sprite = sprite;
            }
            else
            {
                Debug.LogError($"SideloadAPK: Failed to load screenshot from {url}: {request.error}");
            }
        }
    }

    private void OnCTAClick(string appId)
    {
        string clickUrl = $"{API_ENDPOINT}/ads/click?app={appId}&adid={appOpenAdId}";
        Debug.Log($"SideloadAPK: Opening click URL: {clickUrl}");
        Application.OpenURL(clickUrl);
        CloseAd();
    }

    private void CloseAd()
    {
        if (adCanvas != null)
        {
            Destroy(adCanvas);
            adCanvas = null;
        }
    }

    [System.Serializable]
    private class InstallEventPayload
    {
        public string developerKey;
        public string deviceFingerprint;
    }

    [System.Serializable]
    private class AppOpenAdData
    {
        public string id;
        public string name;
        public string iconUrl;
        public string screenshotUrl;
    }
}