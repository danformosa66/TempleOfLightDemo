using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
// Add Menu Option:
//  using UnityEditor;
//  [MenuItem("Menu/Action")]
//  static void DoStuff() { Debug.Log ("Hi..!"); }

// EnumCount: 
//  public static int NUM_ENUM_TYPE { get { return System.Enum.GetNames(typeof(ENUM_TYPE)).Length; } }
// EnumParse:
//  (ENUM_TYPE) System.Enum.Parse(typeof(ENUM_TYPE), "NAME");
// EnumEnumeration:
//  foreach (ENUM_TYPE e in System.Enum.GetValues(typeof(ENUM_TYPE))) 

// LIST Find ALL
// list.FindAll(s => s.Equals("match"));

// NonInterractable UI:
// Add Canvas Group, UNCHECK everything

// Singleton:
/*
public static MY_CLASS _instance = null;
public void Awake()
{
    _instance = this;
}  
 */

// Scaling GUI (Scale with Screen Size, Expand)
/*

    float scaleFactor = Mathf.Max((float)1920 / Screen.width, (float)1024 / Screen.height);
    differenceVector.x *= scaleFactor;
    differenceVector.y *= scaleFactor;
*/

// Load / Save:
/*
public static class SaveManager {

    public static SaveInfo saveInfo = new SaveInfo();

    public static void Reset()
    {
        The_Helper.Reset<SaveInfo>(new SaveInfo());
    }

    public static void Load()
    {
        saveInfo = The_Helper.Load<SaveInfo>(new SaveInfo());
    }

    public static void Save()
    {
        saveInfo.Save();
    } 
}
*/

// Broken Lightmap: Generate LIGHTMAP UVs (Model Import Settings)

public static class The_Helper
{
    public static void ScaleAndCrop(this RectTransform rectTransform, float originalWidth, float originalHeight)
    {
        if (rectTransform == null) return;
        if (rectTransform.parent == null) return;
        RectTransform container = rectTransform.parent.GetComponent<RectTransform>();
        if (container == null) return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.one * 0.5f;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalHeight);

        // First match the player with the mask - player should extend beyond it
        float widthPercentile = rectTransform.rect.width / container.rect.width;
        float heightPercentile = rectTransform.rect.height / container.rect.height;
        float scaleMultiplier = 1 / Mathf.Min(widthPercentile, heightPercentile);

        // Look at the ratio between player and mask (container)
        float r_player = rectTransform.rect.width / rectTransform.rect.height;
        float r_mask = container.rect.width / container.rect.height;

        rectTransform.localScale = Vector3.one * scaleMultiplier;
    }

    public static Texture2D Crop_FromCenter_X(this Texture2D original, float xPercentile)
    {
        return original.Crop_FromCenter(xPercentile, -1);
    }

    public static Texture2D Crop_FromCenter_Y(this Texture2D original, float yPercentile)
    {
        return original.Crop_FromCenter(-1, yPercentile);
    }

    public static Texture2D Crop_FromCenter(this Texture2D original, float xPercentile, float yPercentile)
    {
        if (xPercentile < 0 && yPercentile < 0) return original;

        int windowSize_X = (int)(original.width * xPercentile);
        int windowSize_Y = (int)(original.height * yPercentile);

        if (xPercentile < 0)
            windowSize_X = windowSize_Y;
        if (yPercentile < 0)
            windowSize_Y = windowSize_X;

        int x_window = (original.width - windowSize_X) / 2;
        int y_window = (original.height - windowSize_Y) / 2;

        Color[] pixels_In = original.GetPixels(x_window, y_window, windowSize_X, windowSize_Y);
        Color[] pixels_Out = new Color[pixels_In.Length];

        Texture2D t = new Texture2D(windowSize_X, windowSize_Y);
        for (int i = 0; i < windowSize_X; i++)
            for (int j = 0; j < windowSize_Y; j++)
            {
                int idx_In = j * windowSize_X + i;
                int idx_Out = (windowSize_Y - 1 - j) * windowSize_X + (windowSize_X - 1 - i);
                Color c = pixels_In[idx_In];
                //if (c.grayscale > 0.2f)
                //    c = c.AdjustBrightness(c.grayscale.Retargeted(0.2f, 1, 0.2f, 0.35f));
                pixels_Out[idx_Out] = c;
            }

        t.SetPixels(pixels_Out);
        t.Apply();

        return t;
    }

    public static float Fold (this float f, float max)
    {
        f = f % max;
        if (f <= max / 2)
            return f;
        else
            return max - f;
    }

    public static string ToOnOff(this bool v)
    {
        return "<b>" + (v ? "on" : "off") + "</b>";
    }

    public static bool TryGetBounds<T>(this T thing, out Bounds bounds)
    {
        bounds = new Bounds();

        // Is that thing attached to a gameObject? If so it must be a component
        Transform t = thing.GetComponent<Transform>();
        if (!t)
        {
            GameObject gO = thing as GameObject;
            if (!gO) return false;
            t = gO.transform;
        }

        if (!t) return false;

        // Attempt collider bounds
        Collider c = t.GetComponent<Collider>();
        if (c)
        {
            bounds = c.bounds;
            return true;
        }

        // Attempt Renderer bounds
        Renderer r = t.GetComponent<Renderer>();
        if (r)
        {
            bounds = r.bounds;
            return true;
        }

        // Return the scale
        bounds = new Bounds(t.position, t.lossyScale);
        return true;
    }

    public static I GetInterface<I>(this Component c)
    {
        return c.GetComponent<I>();
    }
    public static I GetInterface<I>(this GameObject g)
    {
        return g.GetComponent<I>();
    }
    public static I GetInterface<I>(this object o)
    {
        if (o.IsNull()) return default(I);
        MonoBehaviour mB = o.TryGetAs<object, MonoBehaviour>();
        if (mB == null) return default(I);
        return mB.GetInterface<I>();
    }
    public static MonoBehaviour GetMonoBehaviour<I>(this I i) where I : class
    {
        return i.TryGetAs<I, MonoBehaviour>();
    }
    public static C GetComponent<C>(this object o) where C : Component
    {
        MonoBehaviour mB = o.GetMonoBehaviour();
        if (mB == null) return null;
        return mB.GetComponent<C>();
    }

    private static G TryGetAs<T, G>(this T t, bool debug = false) where T : class where G : class
    {
        G g = t as G;
        if (g == null)
        {
            // Try Component
            if (debug)
                t.Log("Could not cast from " + typeof(T).Name + " to " + typeof(G).Name + ".", LogType.Warning);
            return null;
        }

        return g;
    }

    public static float Lerp(this Vector2 v2, float interpolator, bool clamp = true)
    {
        if (clamp) interpolator = interpolator.Clamped01();
        return Mathf.Lerp(v2.x, v2.y, interpolator);
    }
    public static Quaternion MuteAxes(this Quaternion q, bool muteX, bool muteY, bool muteZ)
    {
        Vector3 euler = q.eulerAngles;

        Quaternion corrector = Quaternion.identity;

        if (muteX) corrector *= Quaternion.Euler(euler.x * Vector3.right);
        if (muteY) corrector *= Quaternion.Euler(euler.y * Vector3.up);
        if (muteZ) corrector *= Quaternion.Euler(euler.z * Vector3.forward);

        Quaternion r = q * Quaternion.Inverse(corrector);

        // Make sure we got the right one
        Debug.Log(q + "\n" + r);
        return r;
    }

    public static Quaternion Equivalent(this Quaternion q)
    {
        return new Quaternion(-q.x, -q.y, -q.z, -q.w);
    }

    /// <summary>
    /// Feed it the stick (2D input) - it will break it down to speed and rot. Parameters refer to angles from the input's "UP" or "FWD".
    /// </summary>
    /// <param name="fwdSpeed">To affect the character's relative forward movement.</param>
    /// <param name="yRotation">To affect the character's Y rotation.</param>
    /// <param name="fullSpeedZone">Angles less than this will not have a speed reduction.</param>
    /// <param name="justRotationAngle">Approaching this angle, speed will reach 0.</param>
    /// <param name="fullBackMovementZone">Angles more than this will not suffer a speed reduction (negative speeds).</param>
    public static void BreakMovementInput_TopDown_FwdSpeedAndYRotation(this Vector2 input, out float fwdSpeed, out float yRotation,
        float fullSpeedZone = 70, float justRotationAngle = 125, float fullBackMovementZone = 160)
    {
        fwdSpeed = 0;
        yRotation = 0;

        // Break the input into cartessian (angle from "UP" and magnitude)
        float theta = The_Helper.AngleSigned(Vector2.up, input);
        float r = input.magnitude;

        // Figure out the rotation
        if (Mathf.Abs(theta) < justRotationAngle)
            yRotation = theta / justRotationAngle;
        else
            yRotation = Mathf.Sign(theta) * (180 - Mathf.Abs(theta)) / (180 - justRotationAngle);

        // Figure out the speed based on the zone we're in
        if (Mathf.Abs(theta) < fullSpeedZone)
            fwdSpeed = r;
        else if (Mathf.Abs(theta) < justRotationAngle)
            fwdSpeed = r * (justRotationAngle - Mathf.Abs(theta)) / (justRotationAngle - fullSpeedZone);
        else if (Mathf.Abs(theta) < fullBackMovementZone)
            fwdSpeed = Mathf.Sign(input.y) * r * (Mathf.Abs(theta) - justRotationAngle) / (180 - justRotationAngle);
        else
            fwdSpeed = -r;
    }

    public static bool TryAdd<T>(this List<T> list, T element)
    {
        if (list == null) return false;
        if (list.Contains(element)) return false;
        list.Add(element);
        return true;
    }

    public static bool TryAdd<T, G>(this Dictionary<T, G> dictionary, T key, G value)
    {
        if (dictionary == null) return false;
        if (dictionary.ContainsKey(key)) return false;
        dictionary.Add(key, value);
        return true;
    }

    public static bool TryRemove<T>(this List<T> list, T element)
    {
        if (list == null) return false;
        if (!list.Contains(element)) return false;
        list.Remove(element);
        return true;
    }

    public static bool TryRemove<T, G>(this Dictionary<T, G> dictionary, T key)
    {
        if (dictionary == null) return false;
        if (!dictionary.ContainsKey(key)) return false;
        dictionary.Remove(key);
        return true;
    }

    public static Vector3 Lerp(this Vector3 from, Vector3 to, Vector3 lerper)
    {
        return new Vector3(
            Mathf.Lerp(from.x, to.x, lerper.x),
            Mathf.Lerp(from.y, to.y, lerper.y),
            Mathf.Lerp(from.z, to.z, lerper.z));
    }

    public static Vector3 Abs(this Vector3 v3)
    {
        return new Vector3(Mathf.Abs(v3.x), Mathf.Abs(v3.y), Mathf.Abs(v3.z));
    }
    public static float TwoPiT { get { return 2 * Mathf.PI * Time.time; } }
    public static Vector3 Cos(this Vector3 v3)
    {
        return new Vector3(Mathf.Cos(v3.x), Mathf.Cos(v3.y), Mathf.Cos(v3.z));
    }
    public static Vector3 Sin(this Vector3 v3)
    {
        return new Vector3(Mathf.Sin(v3.x), Mathf.Sin(v3.y), Mathf.Sin(v3.z));
    }
    public static Vector2 Cos(this Vector2 v2)
    {
        return new Vector2(Mathf.Cos(v2.x), Mathf.Cos(v2.y));
    }
    public static Vector2 Sin(this Vector2 v2)
    {
        return new Vector2(Mathf.Sin(v2.x), Mathf.Sin(v2.y));
    }

    public static int Iput_GetNumericalChoice()
    {
        if (Input.GetKeyUp(KeyCode.Alpha0)) return 0;
        else if (Input.GetKeyUp(KeyCode.Alpha1)) return 1;
        else if (Input.GetKeyUp(KeyCode.Alpha2)) return 2;
        else if (Input.GetKeyUp(KeyCode.Alpha3)) return 3;
        else if (Input.GetKeyUp(KeyCode.Alpha4)) return 4;
        else if (Input.GetKeyUp(KeyCode.Alpha5)) return 5;
        else if (Input.GetKeyUp(KeyCode.Alpha6)) return 6;
        else if (Input.GetKeyUp(KeyCode.Alpha7)) return 7;
        else if (Input.GetKeyUp(KeyCode.Alpha8)) return 8;
        else if (Input.GetKeyUp(KeyCode.Alpha9)) return 9;
        return int.MinValue;
    }

    public static void PauseRigidbodies<T>(this T obj) where T : Component
    {
        foreach (Rigidbody childRb in obj.GetComponentsInChildren<Rigidbody>())
            if (childRb != obj)
                childRb.PauseRigidbodies();
    }

    public static void Pause(this Rigidbody rB)
    {
        rB.velocity = Vector3.zero;
        rB.angularVelocity = Vector3.zero;
    }

    public static Vector3 SignedPow(this Vector3 v3, float pow)
    {
        return new Vector3(v3.x.SignedPow(pow), v3.y.SignedPow(pow), v3.z.SignedPow(pow));
    }

    public static Vector2 SignedPow(this Vector2 v2, float pow)
    {
        return new Vector2(v2.x.SignedPow(pow), v2.y.SignedPow(pow));
    }

    public static float SignedPow(this float f, float pow)
    {
        return Mathf.Sign(f) * Mathf.Pow(Mathf.Abs(f), pow);
    }

    public static Texture2D LoadFromPath(this Texture2D Texture, string Path)
    {
        if (File.Exists(Path))
        {
            byte[] rawData = File.ReadAllBytes(Path);

            //Put loaded bytes to Texture2D 
            Texture.LoadRawTextureData(rawData);
            Texture.Apply();
        }
        return Texture;
    }

    public static int GetMaxIndex<T>(this IList<T> list) where T : IComparable
    {
        int indexMax
           = !list.Any() ? -1 :
           list
           .Select((value, index) => new { Value = value, Index = index })
           .Aggregate((a, b) => (a.Value.CompareTo(b.Value) > 0) ? a : b)
           .Index;

        return indexMax;
    }

    internal static void Vibrate(int v)
    {
#if UNITY_ANDROID
        VibrationManager.Vibrate(v);
#elif UNITY_IOS
        Handheld.Vibrate();
#elif UNITY_WINRT_8_1
        TGP_UnityPlugins.VibrationManager.Vibrate(v);
#endif
    }

    public static void Genocide<T>(this T t) where T : Component
    {
        if (!t) return;
        for (int i = 0; i < t.transform.childCount; i++)
            UnityEngine.Object.Destroy(t.transform.GetChild(i).gameObject);
    }

    public static int ANDROID_SDK_VERSION
    {
        get
        {
            int version = -1;
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                version = buildVersion.GetStatic<int>("SDK_INT");
            }
#elif UNITY_ANDROID && UNITY_EDITOR
            version = 9000;
#endif
            return version;
        }
    }

    private static AndroidJavaObject Android_getPackageManager()
    {
        AndroidJavaObject pM = null;

#if UNITY_ANDROID
        if (!Application.isEditor)
        {
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
            // int flag = new AndroidJavaClass("android.content.pm.PackageManager").GetStatic<int>("GET_META_DATA");
            pM = currentActivity.Call<AndroidJavaObject>("getPackageManager");
        }
#endif
        return pM;
    }

    /// <summary>
    /// Where did this app come from? (Market)
    /// </summary>
    public static AndroidMarketPlace Android_getMarketPlace()
    {
        // Setting installer name when developing
        /*
        http://pixplicity.com/setting-install-vendor-debug-app/
        adb shell pm uninstall com.tallguyproductions.agameofcoins
        adb push C:/app.apk /sdcard/app.apk
        adb shell pm install -i "com.android" -r /sdcard/app.apk
        adb shell rm /sdcard/app.apk

        */

        AndroidMarketPlace source = AndroidMarketPlace.Unknown;
        string installerPackageName = "";

#if UNITY_ANDROID
        if (!Application.isEditor)
        {
            string packageName = Application.bundleIdentifier;

            // Application.bundleIdentifier
            AndroidJavaObject pm = Android_getPackageManager();
            installerPackageName = pm.Call<string>("getInstallerPackageName", packageName);
            if (installerPackageName == null)
                installerPackageName = "";
        }
#endif

        if (installerPackageName.CompareTo("") != 0)
        {
            Debug_OnGUI_AddMessage(installerPackageName, 10);

            string processedPackageName = installerPackageName.RemoveSpaces();

            Debug_OnGUI_AddMessage(processedPackageName, 10);

            // Differentiate based on name
            // http://stackoverflow.com/questions/17629787/how-android-app-can-detect-what-store-installed-it

            // Samsung contains android in the name
            if (processedPackageName.ContainsInvariant("samsung"))
                source = AndroidMarketPlace.SamsungStore;
            else if (processedPackageName.ContainsInvariant("amazon"))
                source = AndroidMarketPlace.Amazon;
            else if (processedPackageName.ContainsInvariant("android"))
                source = AndroidMarketPlace.GooglePlay;

            Debug_OnGUI_AddMessage(source.ToString());
        }
        else
            Debug_OnGUI_AddMessage("Couldn't get installer package name", 10);

        return source;
    }

    /// <summary>
    /// Opens the corresponding store, based on the device we're running on
    /// </summary>
    public static void OpenAppRateURL()
    {
        // If by the end of the function this doesn't have a value, we won't open an empty URL.
        string url = "";

        // Set to a default value for all systems which do not support rating
        string defaultURL = "http://tall-guy-productions.com";

        // Don't want any nagging for platform-dependent IDs
#pragma warning disable 0219

        // If your app isn't live yet and this is a debug build, 
        // dummy app details can be used (to test functionality)
        // This will link directly to existing apps in the stores
        bool useDummyDetails_DuringDebug_GooglePlay = true;
        bool useDummyDetails_DuringDebug_Amazon = true;
        bool useDummyDetails_DuringDebug_SamsungStore = true;
        bool useDummyDetails_DuringDebug_WindowsStore = true;
        bool useDummyDetails_DuringDebug_AppleAppStore = true;

        // -------------------- [SOS] --------------------
        // If these strings are not empty, a rating will be attempted!
        // If you are not sure what values to put there before releasing the first version
        // of your app, it's better to leave them empty!
        // -------------------- [SOS] --------------------

        // This is the package name 
        string android_AppID = Application.bundleIdentifier;

        // App management -> App identity -> URL for Windows Phone 8.1 and earlier
        string windows_GUID = "86bc0cb0-bc5a-4c47-bbaa-38654f6950bd";

        // App management -> App identity -> URL for Windows 10
        string windows_AppID = "9nblggh5pckw";

        // App Identity -> Bottom
        string apple_AppID = "1090931634";

#pragma warning restore 0219

        // [NOTES]
        // windowsGUID only appears after submitting a package and selecting for availability:
        // Only available to specified people on Windows Phone 8.x devices, 
        // or people with a promotional code on Windows 10 devices. 

        // Google Play Dev Portal
        // https://play.google.com/apps/publish

        // Amazon Dev Portal
        // https://developer.amazon.com/home.html

        // Samsung Dev Portal
        // http://seller.samsungapps.com/content/common/summaryContentList.as

        // Windows Store Dev Portal:
        // https://dev.windows.com/en-us/dashboard/apps/ 

        // Apple Dev Portal
        // https://itunesconnect.apple.com/WebObjects/iTunesConnect.woa/ra/ng/app

#if UNITY_ANDROID
        AndroidMarketPlace market = Android_getMarketPlace();

        // Use this to simulate behavior
        if (Debug.isDebugBuild)
            market = AndroidMarketPlace.GooglePlay;        
        
        if (Debug.isDebugBuild)
        {
            // [GOOGLE PLAY] 
            // Use maps as dummy 
            // https://play.google.com/store/apps/details?id=com.google.android.apps.maps
            if (market == AndroidMarketPlace.GooglePlay && useDummyDetails_DuringDebug_GooglePlay)
                android_AppID = "com.google.android.apps.maps";

            // [AMAZON]
            // Use angry birds as dummy (will someday people use our packages as dummies? :D)
            // http://www.amazon.com/gp/mas/dl/android?p=com.rovio.angrybirds
            else if (market == AndroidMarketPlace.Amazon && useDummyDetails_DuringDebug_Amazon)
                android_AppID = "com.rovio.angrybirds";

            // [SAMSUNG STORE]
            // Use Samsung app itself as reference
            // samsungapps://ProductDetail/com.sec.android.app.samsungapps
            else if (market == AndroidMarketPlace.SamsungStore && useDummyDetails_DuringDebug_SamsungStore)
                android_AppID = "com.sec.android.app.samsungapps";
        }

        if (market == AndroidMarketPlace.GooglePlay)
            url = "market://details?id=" + android_AppID;
        else if (market == AndroidMarketPlace.Amazon)
            url = "amzn://apps/android?p=" + android_AppID;
        else if (market == AndroidMarketPlace.SamsungStore)
            url = "samsungapps://ProductDetail/" + android_AppID;

        if (android_AppID.CompareTo("") == 0)
            url = "";        
        
#elif UNITY_WINRT_8_0 || UNITY_WINRT_8_1 || UNITY_WINRT_10
        // Windows Phone 7.x / 8.x & Windows 8.x
        if (SystemInfo.operatingSystem.Contains("Windows Phone 7") ||
            SystemInfo.operatingSystem.Contains("Windows Phone 8") ||
            SystemInfo.operatingSystem.Contains("Windows 8"))
        {

            // Use OneNote as dummy
            // https://msdn.microsoft.com/en-us/windows/uwp/launch-resume/launch-store-app
            if (Debug.isDebugBuild && useDummyDetails_DuringDebug_WindowsStore)
                windows_GUID = "ca05b3ab-f157-450c-8c49-a1f127f5e71d";

            if (SystemInfo.operatingSystem.Contains("Phone"))
                url = "ms-windows-store:reviewapp?appid=" + windows_GUID;
            else
                url = "ms-windows-store:review?appid=" + windows_GUID;

            if (windows_GUID.CompareTo("") == 0)
                url = "";
        }
        // Windows 10 / Windows Phone 10
        else if (SystemInfo.operatingSystem.Contains("Windows Phone 10") ||
                 SystemInfo.operatingSystem.Contains("Windows 10"))
        {
            // Use OneNote as dummy
            // https://msdn.microsoft.com/en-us/windows/uwp/launch-resume/launch-store-app
            if (Debug.isDebugBuild && useDummyDetails_DuringDebug_WindowsStore)
                windows_AppID = "9WZDNCRFHVJL";

            url = "ms-windows-store://review/?ProductId=" + windows_AppID;

            if (windows_AppID.CompareTo("") == 0)
                url = "";
        }

#elif UNITY_IOS

        // Use Skype as dummy
        // https://itunes.apple.com/us/app/skype-for-iphone/id304878510?mt=8
		if (Debug.isDebugBuild && useDummyDetails_DuringDebug_AppleAppStore)
            apple_AppID = "304878510";

        // Pre iOS 7.x
        if (SystemInfo.operatingSystem.Contains("6."))
            url = "itms-apps://itunes.apple.com/WebObjects/MZStore.woa/wa/"
                + "viewContentsUserReviews?type=Purple+Software&id=" + apple_AppID;
		// Post iOS 7.x (deep link support dropped, using http to reach review directly)
        else     
			url = "http://itunes.apple.com/WebObjects/MZStore.woa/wa/viewContentsUserReviews?id=" + apple_AppID 
				+ "&pageNumber=0&sortOrdering=2&type=Purple+Software&mt=8";
            //url = "itms-apps://itunes.apple.com/app/id" + apple_AppID;   
        
        if (apple_AppID.CompareTo("") == 0)
            url = "";
#endif

        // Nothing yet, go for default
        if (url.CompareTo("") == 0)
            url = defaultURL;

        // Don't open empty URLs
        if (url.CompareTo("") != 0)
            Application.OpenURL(url);
    }

    static float timeLast_Call_Facebook = -Mathf.Infinity;
    static float timeLast_Call_Twitter = -Mathf.Infinity;
    static float timeLast_Call_GooglePlus = -Mathf.Infinity;
    // We attempt to start the app -> If nothing happens the user will probably click on it again
    // This time we want to try the URL instead. This also works well if the app does launch
    // but with problems (directs to store / doesn't find your product). While the user
    // is not in your app (given it doesn't run on background) time doesn't pass, so he'll also be
    // redirected to the url.
    static readonly float minDurationForAppAttempts = 3f;

    public static void GoToFacebook()
    {
        string facebookAddress = "http://www.facebook.com/TallGuyProds";
        string facebookApp = "fb://facewebmodal/f?href=" + facebookAddress;

        if (Time.time - timeLast_Call_Facebook > minDurationForAppAttempts)
            Application.OpenURL(facebookApp);
        else
            Application.OpenURL(facebookAddress);

        timeLast_Call_Facebook = Time.time;
    }

    public static void GoToTwitter()
    {
        string profileID = "706546285302644737";
        string profileName = "TallGuyProds";


        string twitterApp = "twitter://user?user_id=" + profileID;
        string twitterAddress = "http://twitter.com/" + profileName;


        if (Time.time - timeLast_Call_Twitter > minDurationForAppAttempts)
            Application.OpenURL(twitterApp);
        else
            Application.OpenURL(twitterAddress);

        timeLast_Call_Twitter = Time.time;
    }

    public static void GoToGooglePlus()
    {
        string profileID = "u/0/communities/111734634818768957747";

        // User ID
        //  "117004778634926368759";
        // App link - though it's handled by the browser too
        // "gplus://plus.google.com/"
        string googlePlusApp = "http://plus.google.com/" + profileID;
        string googlePlusAddress = "http://plus.google.com/" + profileID;

        if (Time.time - timeLast_Call_GooglePlus > minDurationForAppAttempts)
            Application.OpenURL(googlePlusApp);
        else
            Application.OpenURL(googlePlusAddress);

        timeLast_Call_GooglePlus = Time.time;
    }

    public static void GoToTallGuyProductions()
    {
        string webpage = "http://tall-guy-productions.com/";
        Application.OpenURL(webpage);
    }


    static float[] listenerOutputData;
    public static float GetCurrentVolume(Direction_1D leftRight)
    {
        if (listenerOutputData == null)
            listenerOutputData = new float[4096];

        float result = 0;

        AudioListener.GetOutputData(listenerOutputData, (int)leftRight);
        result = listenerOutputData.GetSqrtOfSumOfSquares();
        return result;
    }

    public static float GetSqrtOfSumOfSquares(this IList<float> iList)
    {
        float result = 0;
        for (int i = 0; i < iList.Count; i++)
            result += Mathf.Pow(iList[i], 2);
        return Mathf.Sqrt(result);
    }

    /// <summary>
    /// Make sure to pass in mean (μ) and variance (σ^2); not standard deviation (σ);
    /// </summary>
    /// <param name="mean"></param>
    /// <param name="variance"></param>
    /// <returns></returns>
    public static float RandomGaussianValue(float mean, float variance)
    {
        double stdDev = Mathf.Sqrt(variance);

        System.Random rand = new System.Random(); //reuse this if you are generating many
        double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
        double u2 = rand.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                     Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        double randNormal =
                     mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
#if UNITY_EDITOR
        // Debug.Log(mean.ToString("#.00") + " -> " + randNormal.ToString("#.00"));
#endif
        return (float)randNormal;
    }

    /// <summary>
    /// Assumes a default words per minute reading of 100 (suitable for children)
    /// </summary>
    public static float GetAverageReadingTime(this string msg)
    {
        float averageWordsPerMinute = 100;
        float averageSecondsPerWord = 1 / (averageWordsPerMinute / 60);
        float averageWordLength = 6;

        float totalTime = 0;
        foreach (string word in msg.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries))
        {
            // Add another word
            totalTime += averageSecondsPerWord * 0.7f + 0.3f * word.Length / averageWordLength;
        }
        return totalTime;
    }
    /// <summary>
    /// Call with both type parameters specified
    /// ie foreach (Renderer r in myRigidbody.GetComponentsInChildren_ExcludeSelf<Rigidbody, Renderer>())
    /// </summary>
    public static T GetComponentInChildren_ExcludeSelf<G, T>(this G g, bool includeInactive = true) where G : Component where T : Component
    {
        foreach (T t in g.GetComponentsInChildren<T>(includeInactive))
        {
            if (t.gameObject == g.gameObject) continue;
            return t;
        }
        return default(T);
    }
    /// <summary>
    /// Call with both type parameters specified
    /// ie foreach (Renderer r in myRigidbody.GetComponentsInChildren_ExcludeSelf<Rigidbody, Renderer>())
    /// </summary>
    public static T[] GetComponentsInChildren_ExcludeSelf<G, T>(this G g, bool includeInactive = true) where G : Component where T : Component
    {
        List<T> objects = new List<T>();
        foreach (T t in g.GetComponentsInChildren<T>(includeInactive))
        {
            if (t.gameObject == g.gameObject) continue;
            objects.Add(t);
        }
        return objects.ToArray();
    }

    public static string AddColor(this string s, Color c)
    {
        return "<color=" + c.ToHex() + ">" + s + "</color>";
    }
    public static void SetLayer(this GameObject gO, int layer, bool includeChildren)
    {
        gO.layer = layer;
        if (!includeChildren) return;
        foreach (Transform t in gO.GetComponentsInChildren<Transform>())
            t.gameObject.layer = layer;
    }

    public static Vector2 GetPositionAsScreenPercentile(this RectTransform rect)
    {
        Vector2 absPos = rect.transform.position;
        absPos.x /= Screen.width;
        absPos.y /= Screen.height;
        return absPos;
    }
    public static Vector2 GetRandomVector2(float min = Mathf.NegativeInfinity, float max = Mathf.Infinity)
    {
        return new Vector2(
            UnityEngine.Random.Range(min, max),
            UnityEngine.Random.Range(min, max));
    }
    public static Vector3 GetRandomVector3(float min = Mathf.NegativeInfinity, float max = Mathf.Infinity)
    {
        return new Vector3(
            UnityEngine.Random.Range(min, max),
            UnityEngine.Random.Range(min, max),
            UnityEngine.Random.Range(min, max));
    }
    public static Vector3 GetRandomVector3BothSigns(float min = Mathf.NegativeInfinity, float max = Mathf.Infinity)
    {
        return new Vector3(
            RandomRangeBothSigns(min, max),
            RandomRangeBothSigns(min, max),
            RandomRangeBothSigns(min, max));
    }
    public static Vector2 GetRandomVector2BothSigns(float min = Mathf.NegativeInfinity, float max = Mathf.Infinity)
    {
        return new Vector2(
            RandomRangeBothSigns(min, max),
            RandomRangeBothSigns(min, max));
    }
    public static T EnumGetRandom<T>()
    {
        var v = Enum.GetValues(typeof(T));
        return (T)v.GetValue(new System.Random().Next(v.Length));
    }
    public static List<T> EnumGetValues<T>()
    {
        List<T> values = new List<T>();
        var v = Enum.GetValues(typeof(T));
        for (int i = 0; i < v.Length; i++)
            values.Add((T)v.GetValue(i));
        return values;
    }
    public static int EnumCount<T>()
    {
        var v = Enum.GetValues(typeof(T));
        return v.Length;
    }

    public static T ToEnum<T>(this string name)
    {
        try
        {
            return (T)Enum.Parse(typeof(T), name);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return default(T);
        }
    }
    public static int ToInt(this string name, int defaultValue = -1)
    {
        int val = defaultValue;
        if (int.TryParse(name, out val))
            return val;
        else
            return defaultValue;
    }
    // 

    /// <summary>
    /// Loads all resources under the base name named ie. Dust0, Dust1, .. until it doesn't find one to load. Returns the loaded objects
    /// </summary> 
    public static List<T> ResourceLoadNumbered<T>(string baseName, int initNumber = 0, int maxNumber = -1) where T : UnityEngine.Object
    {
        List<T> resources = new List<T>();

        int i = 0;

        while (true && (maxNumber < 0 || i <= maxNumber))
        {
            T nextResource = ResourceLoad<T>(baseName + i.ToString());
            if (nextResource == null)
                break;
            resources.Add(nextResource);
            i++;
        }

        return resources;
    }

    public static float Min(this Vector3 v3)
    {
        return Mathf.Min(v3.x, v3.y, v3.z);
    }
    public static float Max(this Vector3 v3)
    {
        return Mathf.Max(v3.x, v3.y, v3.z);
    }
    public static float Min(this Vector2 v2)
    {
        return Mathf.Min(v2.x, v2.y);
    }
    public static float Max(this Vector2 v2)
    {
        return Mathf.Max(v2.x, v2.y);
    }
    /// <summary>
    /// Returns the last element of the list
    /// </summary> 
    public static T GetLast<T>(this IList<T> iList)
    {
        if (iList.Count == 0)
            return default(T);
        return iList[iList.Count - 1];
    }

    /// <summary>
    /// Sets the list's capacity, resizing if necessary
    /// </summary> 
    public static void SetCapacity<T>(this List<T> list, int newCapacity)
    {
        // Check for resize
        if (newCapacity < list.Capacity && newCapacity < list.Count)
            list.RemoveRange(list.Capacity - 1, newCapacity - list.Capacity);

        list.Capacity = newCapacity;
    }

    /// <summary>
    /// Uses the List's capacity to simulate a queue. Useful if you want all the goodies of a list.
    /// </summary> 
    public static void Enqueue<T>(this List<T> list, T value)
    {
        if (list.Count == list.Capacity)
            list.Dequeue();
        list.Add(value);
    }

    /// <summary>
    /// Uses the List's capacity to simulate a queue. Useful if you want all the goodies of a list.
    /// </summary> 
    public static T Dequeue<T>(this List<T> list)
    {
        if (list.Count == 0)
            return default(T);

        T objToReturn = list[0];
        list.RemoveAt(0);
        return objToReturn;
    }

    /// <summary>
    /// Logarithmically smooths outliers - start with default parameters and tune if necessary
    /// </summary>
    /// <param name="outlierTolerance">High Limit -> Mean * (1 + tolerance) || Low Limit -> Mean / (1 + tolerance) </param>
    /// <param name="outlierReductor">Log base for reduction</param>
    public static Vector3 SmoothOutlier(this Vector3 currentMean, Vector3 possibleOutlier, float outlierTolerance = 1, float outlierReductor = 10)
    {
        possibleOutlier.x = currentMean.x.SmoothOutlier(possibleOutlier.x, outlierTolerance, outlierReductor);
        possibleOutlier.y = currentMean.y.SmoothOutlier(possibleOutlier.y, outlierTolerance, outlierReductor);
        possibleOutlier.z = currentMean.z.SmoothOutlier(possibleOutlier.z, outlierTolerance, outlierReductor);

        return possibleOutlier;
    }

    /// <summary>
    /// Logarithmically smooths outliers - start with default parameters and tune if necessary
    /// </summary>
    /// <param name="outlierTolerance">High Limit -> Mean * (1 + tolerance) || Low Limit -> Mean / (1 + tolerance) </param>
    /// <param name="outlierReductor">Log base for reduction</param>
    public static Vector2 SmoothOutlier(this Vector2 currentMean, Vector2 possibleOutlier, float outlierTolerance = 1, float outlierReductor = 10)
    {
        possibleOutlier.x = currentMean.x.SmoothOutlier(possibleOutlier.x, outlierTolerance, outlierReductor);
        possibleOutlier.y = currentMean.y.SmoothOutlier(possibleOutlier.y, outlierTolerance, outlierReductor);

        return possibleOutlier;
    }

    /// <summary>
    /// Logarithmically smooths outliers - start with default parameters and tune if necessary
    /// </summary>
    /// <param name="outlierTolerance">High Limit -> Mean * (1 + tolerance) || Low Limit -> Mean / (1 + tolerance) </param>
    /// <param name="outlierReductor">Log base for reduction</param>
    public static float SmoothOutlier(this float currentMean, float possibleOutlier, float outlierTolerance = 1, float outlierReductor = 10)
    {
        if (currentMean == 0)
        {
            // Debug.LogWarning("CurrentMean was 0, returned the original value");
            return possibleOutlier;
        }

        float highOutlierTolerance = Mathf.Max(
            currentMean * (1 + outlierTolerance), currentMean / (1 + outlierTolerance));
        float lowOutlierTolerance = Mathf.Min(
            currentMean * (1 + outlierTolerance), currentMean / (1 + outlierTolerance));
        outlierReductor = Mathf.Max(2, outlierReductor);

        // Much larger
        if (possibleOutlier > highOutlierTolerance)
            possibleOutlier = highOutlierTolerance +
                Mathf.Log(1 + Mathf.Abs(possibleOutlier - lowOutlierTolerance), outlierReductor);

        // Much smaller
        else if (possibleOutlier < lowOutlierTolerance)
            possibleOutlier = lowOutlierTolerance -
                Mathf.Log(1 + Mathf.Abs(possibleOutlier - lowOutlierTolerance), outlierReductor);

        return possibleOutlier;
    }

    public static float GetRelativeDistance(this Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b) / a.magnitude;
    }

    public static void CopyTo<T>(this IList<T> from, IList<T> to)
    {
        if (to == null)
        {
            if (Debug.isDebugBuild)
                Debug.LogException(new Exception("Null reference exception"));
            return;
        }

        to.Clear();

        foreach (T t in from)
            to.Add(t);
    }

    public static IList<T> RemoveNull<T>(this IList<T> container)
    {
        List<int> nullIndices = new List<int>();

        for (int i = 0; i < container.Count; i++)
            if (container[i].IsNull())
                nullIndices.Add(i);

        if (nullIndices.Count > 0)
            Debug.Log(nullIndices.ToReadableString());

        return container.RemoveIDs(nullIndices);
    }

    public static List<T> CustomToList<T>(this IList<T> iList)
    {
        return iList.ToList();
    }

    public static bool IsNull<T>(this T obj)
    {
        return EqualityComparer<T>.Default.Equals(obj, default(T));
    }
    /*

    static bool IsNull<T>(this T obj)
    {
        bool isNull = obj == null || (obj is UnityEngine.Object && ((obj as UnityEngine.Object) == null));
        return isNull;
    }
    */
    public static IList<T> RemoveDuplicates<T>(this IList<T> container)
    {
        container = container.Distinct().ToList();
        return container;
    }

    public static IList<T> RemoveRange<T>(this IList<T> container, IList<T> rangeToRemove)
    {
        if (container.IsReadOnly)
        {
            List<T> newContainer = new List<T>();
            foreach (T t in container)
                newContainer.Add(t);

            return newContainer.RemoveRange(rangeToRemove);
        }

        foreach (T t in rangeToRemove)
            container.Remove(t);

        return container;
    }

    public static IList<T> CustomOrderBy<T, TKey>(this IList<T> container, Func<T, TKey> keySelector)
    {
        return container.OrderBy(keySelector).ToList();
    }

    public static IList<T> RemoveIDs<T>(this IList<T> container, IList<int> idsToRemove)
    {
        if (container.IsReadOnly)
        {
            List<T> newContainer = new List<T>();
            foreach (T t in container)
                newContainer.Add(t);

            return newContainer.RemoveIDs(idsToRemove);
        }

        for (int i = idsToRemove.Count - 1; i >= 0; i--)
            container.RemoveAt(i);

        return container;
    }

    public static IList<T> FilterByTag<T>(this IList<T> container, KeepOrRemove keepOrDestroyThoseInTag, string tag) where T : Component
    {
        return container.FilterByTags(keepOrDestroyThoseInTag, tag);
    }
    public static IList<T> FilterByTags<T>(this IList<T> container, KeepOrRemove keepOrDestroyThoseInTags, params string[] tags) where T : Component
    {
        if (container.IsReadOnly)
            container = container.FromReadOnly();

        List<T> originalContainer = new List<T>(container);

        if (tags.Length > 0)
            foreach (T obj in originalContainer)
                if ((!tags.Contains(obj.tag) && keepOrDestroyThoseInTags == KeepOrRemove.Keep)
                    || (tags.Contains(obj.tag) && keepOrDestroyThoseInTags == KeepOrRemove.Remove))
                    container.Remove(obj);

        return container;
    }

    public static IList<T> FromReadOnly<T>(this IList<T> readOnlyContainer)
    {
        if (!readOnlyContainer.IsReadOnly)
            return readOnlyContainer;

        IList<T> newContainer = new List<T>();
        foreach (T t in readOnlyContainer)
            newContainer.Add(t);

        return newContainer;
    }

    public static Collider[] GetAllColliders(this IList<RaycastHit> hitInfos, bool includeTriggers)
    {
        List<Collider> colliders = new List<Collider>();

        foreach (RaycastHit rH in hitInfos)
            if (!rH.IsNull() && (includeTriggers || !rH.collider.isTrigger))
                colliders.Add(rH.collider);

        return colliders.ToArray();
    }

    // Contained In Hull <-> Contained in any of the hull's triangles
    public static bool ConvexHullContains(this IList<Vector2> hull, Vector2 point)
    {
        // We need at least 3 points to form a hull
        if (hull == null || hull.Count < 3) return false;

        // Grab 3 points, check if we're in them
        for (int i = 0; i < hull.Count - 2; i++)
            for (int j = i + 1; j < hull.Count - 1; j++)
                for (int k = j + 1; k < hull.Count; k++)
                    if (point.IsContainedInTriangle(hull[i], hull[j], hull[k]))
                        return true;

        // Guess we're not
        return false;
    }

    public static bool IsContainedInTriangle(this Vector2 s, Vector2 a, Vector2 b, Vector2 c)
    {
        // http://stackoverflow.com/questions/2049582/how-to-determine-a-point-in-a-2d-triangle
        float as_x = s.x - a.x;
        float as_y = s.y - a.y;

        bool s_ab = (b.x - a.x) * as_y - (b.y - a.y) * as_x > 0;

        if ((c.x - a.x) * as_y - (c.y - a.y) * as_x > 0 == s_ab) return false;

        if ((c.x - b.x) * (s.y - b.y) - (c.y - b.y) * (s.x - b.x) > 0 != s_ab) return false;

        return true;
    }

    /// <summary>Computes the convex hull of a polygon, in clockwise order in a Y-up 
    /// coordinate system (counterclockwise in a Y-down coordinate system).</summary>
    /// <remarks>Uses the Monotone Chain algorithm, a.k.a. Andrew's Algorithm.</remarks>
    public static List<Vector2> ComputeConvexHull(this IList<Vector2> points)
    {
        var list = new List<Vector2>(points);
        return ComputeConvexHull(list, true);
    }

    public static List<Vector2> ComputeConvexHull(this List<Vector2> points, bool sortInPlace)
    {
        if (!sortInPlace)
            points = new List<Vector2>(points);
        points.Sort((a, b) =>
          a.x == b.x ? a.y.CompareTo(b.y) : (a.x > b.x ? 1 : -1));

        // Importantly, DList provides O(1) insertion at beginning and end
        List<Vector2> hull = new List<Vector2>();
        int L = 0, U = 0; // size of lower and upper hulls

        // Builds a hull such that the output polygon starts at the leftmost point.
        for (int i = points.Count - 1; i >= 0; i--)
        {
            Vector2 p = points[i], p1;

            // build lower hull (at end of output list)
            while (L >= 2 && (p1 = hull.GetLast()).Sub(hull[hull.Count - 2]).Cross(p.Sub(p1)) >= 0)
            {
                hull.RemoveAt(hull.Count - 1);
                L--;
            }
            hull.Add(p);
            L++;

            // build upper hull (at beginning of output list)
            while (U >= 2 && (p1 = hull[0]).Sub(hull[1]).Cross(p.Sub(p1)) <= 0)
            {
                hull.RemoveAt(0);
                U--;
            }
            if (U != 0) // when U=0, share the point added above
                hull.Insert(0, p);
            U++;
            Debug.Assert(U + L == hull.Count + 1);
        }
        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    public static Vector2 Sub(this Vector2 a, Vector2 b)
    {
        return a - b;
    }
    public static float Cross(this Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }


    public static Vector3 Sub(this Vector3 a, Vector3 b)
    {
        return a - b;
    }

    /// <summary>
    /// Returns a valid position within the mesh, where you can place the collider, 
    /// so it doesn't collide with anything in the layermask, 
    /// ignoring those with tags in ignoreTags.
    /// </summary>  
    public static Vector3 GetValidPositionInsideConvex(this MeshFilter container, Collider colliderToPlace, LayerMask layerMask, params string[] ignoreTags)
    {
        // Get all colliders that might cause trouble
        List<Collider> opposingColliders = The_Helper.GetComponentsInScene<Collider>().ToList();

        // Filter out by tag
        opposingColliders.FilterByTags(KeepOrRemove.Remove, ignoreTags);

        // If our container has a collider attached to it, also ignore
        Collider self = container.GetComponent<Collider>();
        if (self)
            opposingColliders.Remove(self);

        // Remove the collider we're trying to place
        opposingColliders.Remove(colliderToPlace);

        // Filter out those who aren't in the area or in the layer mask or which are triggers
        for (int i = 0; i < opposingColliders.Count; i++)
        {
            if (!opposingColliders[i].gameObject.layer.IsInLayerMask(layerMask) || opposingColliders[i].isTrigger)
            {
                opposingColliders.RemoveAt(i);
                i--;
            }
        }

        // Pick a random position in the container 
        Vector3 currentGuess = Vector3.zero;

        // Create a dummy collider at the size of our collider
        GameObject dummyGO = new GameObject();
        colliderToPlace.CopyTo(dummyGO);
        dummyGO.transform.localScale = colliderToPlace.transform.lossyScale;
        Collider dummy = dummyGO.GetComponent<Collider>();

        bool done = false;
        int count = 0;
        while (!done && count++ < 50)
        {
            // Pick a new spot
            currentGuess = container.transform.TransformPoint(container.mesh.GetRandomPointInsideConvex());

            // Check if placing the collider at that point would collide with anything
            dummy.transform.position = currentGuess;
            bool debug = false;
#if UNITY_EDITOR
            debug = true;
#endif
            if (!dummy.IntersectsAny(opposingColliders, debug))
                done = true;
        }
        UnityEngine.Object.DestroyImmediate(dummyGO);
        return currentGuess;
    }

    /// <summary>
    /// Picks a random point inside a CONVEX mesh.
    /// Taking advantage of Convexity, we can produce more evenly distributed points
    /// </summary> 
    public static Vector3 GetRandomPointInsideConvex(this Mesh m)
    {
        // Grab two points on the surface
        Vector3 randomPointOnSurfaceA = m.GetRandomPointOnSurface();
        Vector3 randomPointOnSurfaceB = m.GetRandomPointOnSurface();

        // Interpolate between them
        return Vector3.Lerp(randomPointOnSurfaceA, randomPointOnSurfaceB, UnityEngine.Random.Range(0f, 1f));
    }

    /// <summary>
    /// Picks a random point inside a NON-CONVEX mesh.
    /// The only way to get good approximations is by providing a point (if there is one)
    /// that has line of sight to most other points in the non-convex shape.
    /// </summary> 
    public static Vector3 GetRandomPointInsideNonConvex(this Mesh m, Vector3 pointWhichSeesAll)
    {
        // Grab one point (and the center which we assume has line of sight with this point)
        Vector3 randomPointOnSurface = m.GetRandomPointOnSurface();

        // Interpolate between them
        return Vector3.Lerp(pointWhichSeesAll, randomPointOnSurface, UnityEngine.Random.Range(0f, 1f));
    }


    /// <summary>
    /// Picks a random point on the mesh's surface.
    /// </summary> 
    public static Vector3 GetRandomPointOnSurface(this Mesh m, Transform debugTransform = null)
    {
        // Pick a random triangle (each triangle is 3 integers in a row in m.triangles)
        // So Pick a random origin (0, 3, 6, .. m.triangles.Length - 3)
        // -> Random (0.. m.triangles.Length / 3) * 3
        float randSeed = UnityEngine.Random.Range(0f, Mathf.Max(m.triangles.Length, 100000000));
        int triangleOrigin = Mathf.FloorToInt(randSeed % m.triangles.Length / 3f) * 3;

        // Grab the 3 points that consist of the triangle
        Vector3 vertexA = m.vertices[m.triangles[triangleOrigin]];
        Vector3 vertexB = m.vertices[m.triangles[triangleOrigin + 1]];
        Vector3 vertexC = m.vertices[m.triangles[triangleOrigin + 2]];

        // Pick a random point on the triangle
        // For a uniform distribution, we pick randomly according to this:
        // http://mathworld.wolfram.com/TrianglePointPicking.html
        // From the point of origin (vertexA) move a random distance towards vertexB and from there a random distance in the direction of (vertexC - vertexB)
        // The only (temporary) downside is that we might end up with points outside our triangle as well, which have to be mapped back
        // The good thing is that these points can only end up in the triangle's "reflection" across the AC side (forming a quad AB, BC, CD, DA)

        Vector3 dAB = vertexB - vertexA;
        Vector3 dBC = vertexC - vertexB;

        float rAB = UnityEngine.Random.Range(0f, 1f);
        float rBC = UnityEngine.Random.Range(0f, 1f);

        Vector3 randPoint = vertexA + rAB * dAB + rBC * dBC;

        // We have produces random points on a quad (the extension of our triangle)
        // To map back to the triangle, first we check if we are on the extension of the triangle
        // Since we can be on one of two triangles this is equivalent with checking if we are on the correct side of the AC line
        // If we are on the correct side (towards B) we are on the triangle - else we are not.

        // To check that we can compare the direction of our point towards any point on that line (say, C)
        // with the direction of the height of side AC (Cross (triangleNormal, dirBC)))
        Vector3 dirPC = (vertexC - randPoint).normalized;

        Vector3 dirAB = (vertexB - vertexA).normalized;
        Vector3 dirAC = (vertexC - vertexA).normalized;

        Vector3 triangleNormal = Vector3.Cross(dirAC, dirAB).normalized;

        Vector3 dirH_AC = Vector3.Cross(triangleNormal, dirAC).normalized;

        // If the two are alligned, we're in the wrong side
        float dot = Vector3.Dot(dirPC, dirH_AC);

        // We are on the right side, we're done
        if (dot >= 0)
        {
            // Otherwise, we need to find the symmetric to the center of the "quad" which is on the intersection of side AC with the bisecting line of angle (BA, BC)
            // Given by
            Vector3 centralPoint = (vertexA + vertexC) / 2;

            // And the symmetric point is given by the equation c - p = p_Sym - c => p_Sym = 2c - p
            Vector3 symmetricRandPoint = 2 * centralPoint - randPoint;

            if (debugTransform)
                Debug.DrawLine(debugTransform.TransformPoint(randPoint), debugTransform.TransformPoint(symmetricRandPoint), Color.red, 10);
            randPoint = symmetricRandPoint;
        }

        // For debugging purposes
        if (debugTransform)
        {
            Debug.DrawLine(debugTransform.TransformPoint(randPoint), debugTransform.TransformPoint(vertexA), Color.cyan, 10);
            Debug.DrawLine(debugTransform.TransformPoint(randPoint), debugTransform.TransformPoint(vertexB), Color.green, 10);
            Debug.DrawLine(debugTransform.TransformPoint(randPoint), debugTransform.TransformPoint(vertexC), Color.blue, 10);
            // Debug.DrawRay(debugTransform.TransformPoint(randPoint), triangleNormal, Color.cyan, 10); 
        }

        return randPoint;
    }

    /// <summary>
    /// Returns the mesh's center.
    /// </summary> 
    public static Vector3 GetCenterPoint(this Mesh m)
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 v in m.vertices)
            center += v;
        return center / m.vertexCount;
    }
    public static bool IsConvex(this Collider c)
    {
        return !(c as MeshCollider) || (c as MeshCollider).convex;
    }

    public static void SetConvex(this Collider c, bool on)
    {
        MeshCollider mC = c as MeshCollider;
        if (!mC) return;
        mC.convex = on;
    }

    // TODO: NEEDS REWORK - DOESNT WORK WITH ROTATED OBJECTS

    /// <summary>
    /// Returns false if our collider's bounding box intersects any of the colliders
    /// </summary> 
    public static bool IntersectsAny(this Collider c, IList<Collider> opposingColliders, bool debug = false)
    {
        foreach (Collider oC in opposingColliders)
            if (c.bounds.Intersects(oC.bounds))
            {
                if (Debug.isDebugBuild && debug)
                    Debug.Log(c.name + " intersects " + oC.name);
                return true;
            }
        return false;
    }
    /// <summary>
    /// Returns false if our collider's bounding box intersects any of the colliders
    /// </summary> 
    public static bool Intersects(this Collider c, Collider opposingCollider)
    {
        return c.bounds.Intersects(opposingCollider.bounds);
    }

    public static void CopyTo<T>(this T other, GameObject gO) where T : Component
    {
        try
        {
            if (typeof(T) == typeof(Collider))
            {
                Type colliderType = other.GetType();
                gO.AddComponent(colliderType);
                other.CopyTo(gO.GetComponent(colliderType));
            }
            else
            {
#if !UNITY_METRO
                if (!typeof(T).IsAbstract)
#endif
                    other.CopyTo(gO.AddComponentIfNotExists<T>());
            }
        }
        catch (Exception e)
        {
            if (Debug.isDebugBuild)
                Debug.LogException(e, gO);
            return;
        }
    }
    static T CopyTo<T>(this T other, T comp) where T : Component
    {
        Type type = comp.GetType();
        if (type != other.GetType()) return null; // type mis-match

#pragma warning disable 0219
        BindingFlags flags = BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
#pragma warning restore 0219

#if !UNITY_METRO
        flags |= BindingFlags.Default;
#endif
        PropertyInfo[] pinfos;
#if !UNITY_METRO || UNITY_EDITOR
        pinfos = type.GetProperties(flags);
#else
        pinfos = type.GetRuntimeProperties().
                         Where(p=> p.GetMethod != null && p.SetMethod != null &&
                                     !p.SetMethod.IsStatic && !p.GetMethod.IsStatic).ToArray();
#endif
        foreach (var pinfo in pinfos)
        {
            if (pinfo.CanWrite)
            {
                try
                {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos;
#if !UNITY_METRO || UNITY_EDITOR
        finfos = type.GetFields(flags);
#else
        finfos = type.GetRuntimeFields().ToArray();
#endif
        foreach (var finfo in finfos)
        {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }

    public static float RandomRangeBothSigns(float from, float to)
    {
        float sgn = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
        if (sgn == 0)
            sgn = 1;

        return UnityEngine.Random.Range(from, to) * sgn;
    }


    public static bool IsActive(this CanvasGroup cG)
    {
        return (cG.alpha > 0 && cG.interactable);
    }


    public static float Lerp_Fill(this Image img, float desiredPercentile, float speed = 1, float maxFill = 1)
    {
        float diff = desiredPercentile * maxFill - img.fillAmount;
        if (Mathf.Abs(diff) > maxFill * Time.deltaTime)
            img.fillAmount += Mathf.Sign(diff) * maxFill * Time.deltaTime * speed;
        else
            img.fillAmount = desiredPercentile * maxFill;
        return img.fillAmount / maxFill;
    }


    public static string ToRoman(this int number)
    {
        if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
        if (number < 1) return string.Empty;
        if (number >= 1000) return "M" + ToRoman(number - 1000);
        if (number >= 900) return "CM" + ToRoman(number - 900); //EDIT: i've typed 400 instead 900
        if (number >= 500) return "D" + ToRoman(number - 500);
        if (number >= 400) return "CD" + ToRoman(number - 400);
        if (number >= 100) return "C" + ToRoman(number - 100);
        if (number >= 90) return "XC" + ToRoman(number - 90);
        if (number >= 50) return "L" + ToRoman(number - 50);
        if (number >= 40) return "XL" + ToRoman(number - 40);
        if (number >= 10) return "X" + ToRoman(number - 10);
        if (number >= 9) return "IX" + ToRoman(number - 9);
        if (number >= 5) return "V" + ToRoman(number - 5);
        if (number >= 4) return "IV" + ToRoman(number - 4);
        if (number >= 1) return "I" + ToRoman(number - 1);
        throw new ArgumentOutOfRangeException("something bad happened");
    }
    public static void Toggle(this IList<CanvasGroup> canvasGroupList, bool on)
    {
        if (canvasGroupList == null) return;
        foreach (CanvasGroup cG in canvasGroupList)
            cG.Toggle(on);
    }
    public static void Toggle(this CanvasGroup canvasGroup, bool on)
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = on ? 1 : 0;
        canvasGroup.blocksRaycasts = on;
        LayoutElement lE = canvasGroup.GetComponent<LayoutElement>();
        if (lE)
        {
            lE.enabled = !on;
            lE.ignoreLayout = !on;
        }
    }

    public static void Reset<T>(T defaultValue)
    {

        string serializedInfo = defaultValue.XmlSerializeToString();
        PlayerPrefs.SetString("SaveInfo", serializedInfo);

    }
    public static T Load<T>(T defaultValue)
    {
        // First time
        if (!PlayerPrefs.HasKey("SaveInfo"))
        {
            PlayerPrefs.SetString("SaveInfo", defaultValue.XmlSerializeToString());
            return defaultValue;
        }

        string serializedInfo = PlayerPrefs.GetString("SaveInfo");

        return serializedInfo.XmlDeserializeFromString<T>();

    }

    public static void Save<T>(this T saveInfo)
    {
        string serializedInfo = saveInfo.XmlSerializeToString();
        PlayerPrefs.SetString("SaveInfo", serializedInfo);
    }

    public static string XmlSerializeToString(this object objectInstance)
    {
        var serializer = new XmlSerializer(objectInstance.GetType());
        var sb = new StringBuilder();

        using (TextWriter writer = new StringWriter(sb))
        {
            serializer.Serialize(writer, objectInstance);
        }

        return sb.ToString();
    }

    public static Texture2D ReadPNGFromFile(string fullPath)
    {
        if (!File.Exists(fullPath)) return null;

        byte[] rawData = File.ReadAllBytes(fullPath);
        if (rawData == null) return null;

        // Tex size does NOT matter - will be replaced in the next line
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(rawData);
        return tex;
    }

    public static T XmlDeserializeFromString<T>(this string objectData)
    {
        return (T)XmlDeserializeFromString(objectData, typeof(T));
    }

    public static object XmlDeserializeFromString(this string objectData, Type type)
    {
        var serializer = new XmlSerializer(type);
        object result;

        using (TextReader reader = new StringReader(objectData))
        {
            result = serializer.Deserialize(reader);
        }

        return result;
    }

    public static void XmlSaveToAssets<T>(this T item, string folder, string fileName) where T : class
    {
        if (item == null) return;

        if (!fileName.ContainsInvariant(".xml"))
            fileName += ".xml";
        if (folder[folder.Length - 1] != '/')
            folder += "/";
        string filePath = Application.dataPath + "/" + folder + fileName;

        item.XmlSaveToFile(filePath);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    public static void XmlSaveToFile<T>(this T item, string fullPath) where T : class
    {
        if (item == null) return;

        string data = item.XmlSerializeToString();

        data.SaveData(fullPath);
    }
    public static T XmlLoadFromFile<T>(string filePath) where T : class
    {
        if (!filePath.ContainsInvariant(".xml"))
            filePath += ".xml";

        // Check if file exists
        if (!File.Exists(filePath))
            return null;

        // Read raw file text
        string _xml = File.ReadAllText(filePath);

        // XML Deserialize text to FeedbackAnalysis
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        StringReader reader = new StringReader(_xml.ToString());

        T loadedObject = serializer.Deserialize(reader) as T;
        reader.Close();

        return loadedObject;
    }

    public static void SaveData(this string data, string fullFilePath)
    {
        // Write the string array to a new file named "WriteLines.txt".
        using (StreamWriter outputFile = new StreamWriter(fullFilePath))
        {
            outputFile.WriteLine(data);
        }
    }

    public static void SaveDataToAssets(this string data, string assetsFilePath)
    {
        data.SaveData(Application.dataPath + "/" + assetsFilePath);
    }

    public static T XmlLoadFromAssets<T>(string folder, string fileName) where T : class
    {
        string filePath = Application.dataPath + "/" + folder + "/" + fileName;

        return XmlLoadFromFile<T>(filePath);
    }

    public static GameObject[] GetChildrenGameObjects(this GameObject obj)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (Transform t in obj.GetComponentsInChildren<Transform>())
            if (t != obj.transform)
                list.Add(t.gameObject);
        return list.ToArray();
    }

    public static T[] GetCompNoRoot<T>(this GameObject obj) where T : Component
    {
        List<T> tList = new List<T>();
        foreach (Transform child in obj.transform.root)
        {
            T[] scripts = child.GetComponentsInChildren<T>();
            if (scripts != null)
            {
                foreach (T sc in scripts)
                    tList.Add(sc);
            }
        }
        return tList.ToArray();
    }

    public static T[] ResourceLoadAll_Under<T>(string folderPath) where T : UnityEngine.Object
    {
        // Grab the exercise's audio clip
        List<T> resources = new List<T>();

        // We want the same cues for left and right exercises
        foreach (object o in Resources.LoadAll(folderPath))
        {
            T nextResource = o as T;
            if (nextResource == null) continue;
            resources.Add(nextResource);
        }

        return resources.ToArray();
    }

    public static T[] ResourceLoadAll<T>(string mainName, int initIndex = 0, int maxIndex = 100) where T : UnityEngine.Object
    {
        List<T> resources = new List<T>();
        int i = initIndex;
        while (true && i < maxIndex)
        {
            T resource = ResourceLoad<T>(mainName + i);
            i++;
            if (!resource)
                break;
            resources.Add(resource);
        }
        return resources.ToArray();
    }
    public static T AddComponentIfNotExists<T>(this GameObject gO) where T : Component
    {
        T existingT = gO.GetComponent<T>();
        if (existingT)
            return existingT;
        return gO.AddComponent<T>();
    }

    public static T GetComponentByNetID<T>(this NetworkInstanceId ID) where T : Component
    {
        if (ID == NetworkInstanceId.Invalid) return null;

        GameObject gO = ClientScene.FindLocalObject(ID);

        if (!gO)
            return null;

        return gO.GetComponent<T>();
    }
    public static string AddLeadingSymbols(this int number, int maxCount, char c = '_')
    {
        // Add as many zeros as needed to have everything aligned
        string zeros = "";
        int leadingZeros = Mathf.FloorToInt(
            Mathf.FloorToInt(Mathf.Log10(maxCount)) -
            Mathf.FloorToInt(Mathf.Log10(Mathf.Max(number, 1))));

        for (int i = 0; i < leadingZeros; i++)
            zeros += "_";

        zeros += number;

        return zeros;
    }

    public static string ToReadableString<T, G>(this Dictionary<T, G> dictionary, string title = "")
    {
        if (title == "")
            title = "Dictionary contents";
        string s = title + ":\n";
        for (int i = 0; i < title.Length + 2; i++)
            s += "-";
        foreach (KeyValuePair<T, G> kVP in dictionary)
            s += "\n\n[" + kVP.Key.ToString() + "]\n" + kVP.Value.ToString();

        return s;
    }

    public static string ToReadableString<T>(this IList<T> list)
    {
        string s = "IList contents:\n-----------------";
        for (int idx = 0; idx < list.Count; idx++)
        {
            // Add as many zeros as needed to have everything aligned
            string zeros = idx.AddLeadingSymbols(list.Count);

            s += "\n[" + zeros + "]: " + list[idx].ToString();
        }
        return s;
    }
    public static List<T> ToList<T>(this T[] array)
    {
        return new List<T>(array);
    }
    public static void ClearAndDestroy<T>(this IList<T> list) where T : Component
    {
        foreach (T t in list)
            if (t)
                GameObject.Destroy(t.gameObject);
        list.Clear();
    }
    public static T TryGetComponent<T>(this GameObject gO) where T : Component
    {
        if (!gO) return null;
        return gO.GetComponent<T>();
    }
    public static bool LogicalAnd(this IList<Toggle> list)
    {
        bool and = true;
        foreach (Toggle t in list)
        {
            if (!t.isOn)
            {
                and = false;
                break;
            }
        }
        return and;
    }

    public static bool LogicalOr(this IList<Toggle> list)
    {
        bool or = false;
        foreach (Toggle t in list)
        {
            if (t.isOn)
            {
                or = true;
                break;
            }
        }
        return or;
    }
    public static void Toggle(this GameObject gO)
    {
        gO.SetActive(!gO.activeSelf);
    }


    public static void Toggle_On_Play_Toggle_Off(this ParticleSystem pS)
    {
        if (pS.isPlaying) pS.Stop();
        pS.Simulate(0.0f, true, true);
        pS.Toggle(true);
        pS.Play();
        StartTimer(pS.duration, success => { if (pS) { pS.Toggle(false); pS.Stop(); } });

    }

    public static void Toggle(this ParticleSystem pS, bool on)
    {
        foreach (ParticleSystem child in pS.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.EmissionModule pEM = child.emission;
            pEM.enabled = on;
            child.GetComponent<ParticleSystemRenderer>().enabled = on;
            ParticleSystem.SubEmittersModule sE = child.subEmitters;
            if (sE.birth0)
                sE.birth0.Toggle(on);
            if (sE.birth1)
                sE.birth1.Toggle(on);
            if (sE.collision0)
                sE.collision0.Toggle(on);
            if (sE.collision1)
                sE.collision1.Toggle(on);
            if (sE.death0)
                sE.death0.Toggle(on);
            if (sE.death1)
                sE.death1.Toggle(on);
        }

    }

    public static string CapitalizeFirst(this string s)
    {
        return s[0].ToString().ToUpper() + s.Substring(1, s.Length - 1).ToLower();

    }
    public static float RandomBetween(this Vector2 v2)
    {
        return UnityEngine.Random.Range(v2.x, v2.y);
    }
    public static bool CopyPoseTo(this Transform from, Transform to)
    {
        Transform[] fromChildren = from.GetComponentsInChildren<Transform>();
        Transform[] toChildren = to.GetComponentsInChildren<Transform>();

        if (fromChildren.Length != toChildren.Length)
            return false;

        for (int i = 0; i < fromChildren.Length; i++)
        {
            toChildren[i].localPosition = fromChildren[i].localPosition;
            toChildren[i].localRotation = fromChildren[i].localRotation;
            toChildren[i].localScale = fromChildren[i].localScale;
        }

        return true;
    }

    public static float NegMod(this float f, float m)
    {
        while (f < 0)
            f += m;
        return f % m;
    }
    public static int NegMod(this int f, int m)
    {
        return (int)((float)f).NegMod(m);
    }
    public static float CircularDistance(this float a, float b, float m, MinMax minMax)
    {
        // Bring both a and b in the [0, m] 
        a = a.NegMod(m);
        b = b.NegMod(m);

        float dist1 = (b - a).NegMod(m);
        float dist2 = (a - b).NegMod(m);

        // Keep either the shortest or the longest of the two distances
        if (minMax == MinMax.Min)
            return Mathf.Min(dist1, dist2);
        else
            return Mathf.Max(dist1, dist2);
    }
    public static int CircularDistance(this int a, int b, int m, MinMax minMax)
    {
        return (int)((float)a).CircularDistance(b, m, minMax);
    }

    public static void SetEmissionColor(this Material m, Color c, float emissionLevel = 1)
    {
        m.SetColor("_EmissionColor", c * Mathf.LinearToGammaSpace(emissionLevel));
    }

    public static void TurnInvisible(this Renderer r, float alpha)
    {
        r.material.TurnInvisible(alpha);
    }
    public static void TurnVisible(this Renderer r)
    {
        r.material.TurnVisible();
    }
    public static void TurnInvisible(this Material m, float alpha)
    {
        m.ChangeColorProperty(ColorProperty.a, alpha);
        m.DisableKeyword("_EMISSION");
        m.ChangeMode(BlendMode.Fade);
    }

    public static void TurnVisible(this Material m)
    {
        m.ChangeColorProperty(ColorProperty.a, 1);

        if (m.HasProperty("_EmissionMap") && m.GetTexture("_EmissionMap") && !m.shaderKeywords.Contains("_EMISSION"))
            m.EnableKeyword("_EMISSION");

        m.ChangeMode(BlendMode.Opaque);
    }

    public static void ChangeMode(this Material material, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Opaque:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case BlendMode.Cutout:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                break;
            case BlendMode.Fade:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
            case BlendMode.Transparent:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }

    public static GameObject PrefabInstantiateAsChild(this GameObject gO, GameObject prefab, bool isUI = false)
    {
        if (prefab == null)
        {
            if (Debug.isDebugBuild)
                Debug.LogError("NULL");
        }
        GameObject child = UnityEngine.Object.Instantiate(prefab,
            gO.transform.position, gO.transform.rotation) as GameObject;
        child.transform.SetParent(gO.transform, !isUI);
        if (isUI)
        {
            child.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            child.GetComponent<RectTransform>().localPosition = Vector3.zero;
        }
        return child;
    }

    public static GameObject ResourceInstantiateAsChild_GameObject(this GameObject gO, string filePath, bool isUI = false)
    {
        GameObject child = ResourceInstantiate_GameObject(filePath, gO.transform.position, gO.transform.rotation);
        child.transform.SetParent(gO.transform, !isUI);
        if (isUI)
            child.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        return child;
    }
    public static GameObject ResourceInstantiate_GameObject(string filePath)
    {
        return ResourceInstantiate_GameObject(filePath, Vector3.zero, Quaternion.identity);
    }
    public static GameObject ResourceInstantiate_GameObject(string filePath, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = Resources.Load<GameObject>(filePath);
        if (!prefab)
        {
            if (Debug.isDebugBuild)
                Debug.LogError("Could not instantiate " + filePath);
            return null;
        }
        GameObject gO = GameObject.Instantiate(prefab, position, rotation) as GameObject;
        gO.name = filePath;
        return gO;
    }

    public static T ResourceLoad<T>(string filePath) where T : UnityEngine.Object
    {
        return Resources.Load<T>(filePath) as T;
    }


    public static T ResourceInstantiate<T>(string filePath) where T : UnityEngine.Object
    {
        return UnityEngine.Object.Instantiate(ResourceLoad<T>(filePath)) as T;
    }
    public static bool Or(this IList<bool> values)
    {
        bool or = false;
        foreach (bool v in values)
        {
            if (v)
            {
                or = true;
                break;
            }
        }
        return or;
    }
    public static bool And(this IList<bool> values)
    {
        bool and = true;
        foreach (bool v in values)
        {
            if (!v)
            {
                and = false;
                break;
            }
        }
        return and;
    }
    /// <summary>
    /// Sets the local scale of the transform so that its lossy scale is as requested.
    /// </summary> 
    public static void SetLossyScale(this Transform t, Vector3 lossyScale)
    {
        Vector3 scaleToSet = lossyScale;
        if (t.parent != null)
            scaleToSet = lossyScale.DivideBy(t.parent.lossyScale);
        if (!scaleToSet.isNaN())
            t.localScale = scaleToSet;
    }

    public static Vector3 DivideBy(this Vector3 vA, Vector3 vB)
    {
        return new Vector3(
            vB.x == 0 ? float.NaN : vA.x / vB.x,
            vB.y == 0 ? float.NaN : vA.y / vB.y,
            vB.z == 0 ? float.NaN : vA.z / vB.z);
    }
    public static Vector2 DivideBy(this Vector2 vA, Vector2 vB)
    {
        return new Vector2(
            vB.x == 0 ? float.NaN : vA.x / vB.x,
            vB.y == 0 ? float.NaN : vA.y / vB.y);
    }

    public static Vector3 MultiplyBy(this Vector3 vA, Vector3 vB)
    {
        return new Vector3(vA.x * vB.x, vA.y * vB.y, vA.z * vB.z);
    }
    public static Vector2 MultiplyBy(this Vector2 vA, Vector2 vB)
    {
        return new Vector2(vA.x * vB.x, vA.y * vB.y);
    }

    public static float ToKMH(this float speedMS)
    {
        return speedMS * 3600 / 1000;
    }

    public static Vector3 localRight(this Transform t)
    {
        return t.localRotation * Vector3.right;
    }
    public static Vector3 localUp(this Transform t)
    {
        return t.localRotation * Vector3.up;
    }
    public static Vector3 localForward(this Transform t)
    {
        return t.localRotation * Vector3.forward;
    }
    /// <summary>
    /// Signed angle from one V3 to another, assuming up direction
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="up"></param>
    /// <returns></returns>
    public static float AngleSigned(Vector3 from, Vector3 to, Vector3 up)
    {
        return Mathf.Atan2(
            Vector3.Dot(up, Vector3.Cross(from, to)),
            Vector3.Dot(from, to)) * Mathf.Rad2Deg;
    }

    internal static float AngleSigned(Vector2 from, Vector2 to, bool clockwise = true)
    {
        return AngleSigned(from, to, clockwise ? Vector3.back : Vector3.forward);
    }

    /// <summary>
    /// Finds a single Component of type T1 in the scene - else returns null & Error
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <param name="debug"></param>
    /// <returns></returns>
    public static T GetComponentInScene<T>(bool debug = false) where T : Component
    {
        T myObject = UnityEngine.Object.FindObjectOfType<T>();

        if (Debug.isDebugBuild && debug)
        {
            if (myObject == null)
                Debug.LogWarning("Make sure there is a " + typeof(T).FullName + " in the scene..!");
            else
                Debug.Log("Found a " + typeof(T).FullName + " attached to " + myObject.name);
        }

        return myObject;
    }

    /*
	public static bool IsSameOrSubclassOf(this Type potentialDescendant, Type potentialBase)
	{
		return potentialDescendant.IsSubclassOf(potentialBase)
			|| potentialDescendant == potentialBase;
	}
    */

    public static void Log<T>(this T caller, string msg, LogType logType = LogType.Log, float showOnGUI_ForSeconds = 0) where T : class
    {
        if (!Debug.isDebugBuild)
            return;

        string output = "[" + Time.time.ToString("#.00") + " (" + caller + ")]: " + msg;
        if (logType == LogType.Log)
            Debug.Log(output, caller as UnityEngine.Object);
        else if (logType == LogType.Warning)
            Debug.LogWarning(output, caller as UnityEngine.Object);
        else if (logType == LogType.Error)
            Debug.LogError(output, caller as UnityEngine.Object);
        else if (logType == LogType.Exception)
            Debug.LogException(new Exception(output), caller as UnityEngine.Object);
        else if (logType == LogType.Assert)
            Debug.LogAssertion(output, caller as UnityEngine.Object);

        Debug_OnGUI_AddMessage(output, showOnGUI_ForSeconds);
    }
    /// <summary>
    /// Finds a single Component of type T1 in the scene - else returns null & Error
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <param name="debug"></param>
    /// <returns></returns>
    public static T[] GetComponentsInScene<T>(bool debug = false) where T : Component
    {
        T[] myObjects = UnityEngine.Object.FindObjectsOfType<T>();

        if (Debug.isDebugBuild && debug)
        {
            if (myObjects.Length == 0)
                Debug.LogWarning("Couldn't find any " + typeof(T).FullName + " components in the scene..!");
            else
                Debug.Log("Found " + myObjects.Length + " " + typeof(T).FullName + (myObjects.Length != 1 ? " component" : " components") + " attached to various gameObjects");
        }

        return myObjects;
    }

    public static T GetRandom<T>(this IList<T> array)
    {
        if (array == null) return default(T);
        if (array.Count == 0) return default(T);
        return array[UnityEngine.Random.Range(0, 1000000) % array.Count];
    }

    public static float ArcLengthToDegrees(this float length, float radius)
    {
        // All of it is 2πr and it correspnds to 360 degrees
        // So l/2πr <-> degrees/360
        // degrees = l / 2πr * 360
        return length / (2 * Mathf.PI * radius) * 360;
    }
    public static void RemoveAt<T>(this T[] array, int value)
    {
        List<T> newArray = new List<T>();
        for (int i = 0; i < array.Length; i++)
            if (i != value)
                newArray.Add(array[i]);
        array = newArray.ToArray();
    }
    public static void Add<T>(this T[] array, T value)
    {
        List<T> newArray = new List<T>();
        for (int i = 0; i < array.Length; i++)
            newArray.Add(array[i]);
        newArray.Add(value);
        array = newArray.ToArray();
    }
    public static bool Contains(this IList<string> array, string value)
    {
        foreach (string s in array)
            if (s == value)
                return true;

        return false;
    }
    public static bool ContainsInvariant(this IList<string> array, string value)
    {
        foreach (string s in array)
            if (s.ContainsInvariant(value))
                return true;

        return false;
    }
    public static bool IsInLayerMask(this string layerName, LayerMask layerMask)
    {
        return (layerMask == (layerMask | 1 << LayerMask.NameToLayer(layerName)));
    }

    public static bool IsInLayerMask(this int layer, LayerMask layerMask)
    {
        return (layerMask == (layerMask | 1 << layer));
    }

    public static Vector3 XZ(this Vector3 v3)
    {
        return v3 - v3.y * Vector3.up;
    }

    /// <summary>
    /// Returns the sigmoid value of x.
    /// </summary>
    /// <param name="x">Input</param>
    /// <param name="a">Steepness</param>
    /// <param name="b">Center</param>
    /// <returns></returns>
    public static float Sigmoid(this float x, float a = 1, float b = 0)
    {
        return 1 / (1 + Mathf.Exp(-a * (x - b)));
    }

    /// <summary>
    /// Sigmoid that produces the full spectrum of 0...1 values when given input in the range [0... 2*b]
    /// </summary>
    /// <param name="x"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float Sigmoid01(this float x, float a = 1, float b = 1)
    {
        return Mathf.Clamp01((x.Sigmoid(a, b) - 0.5f) / (-((0f).Sigmoid(a, b) - 0.5f)) * 0.5f + 0.5f);
    }


    public static float RetargetedFrom0_1To05_2(this float x01)
    {
        x01 = Mathf.Clamp01(x01);
        if (x01 <= 0.5f)
            return x01 + 0.5f;
        else
            return x01 * 2;
    }
    public static float RetargetedFrom05_2To0_1(this float x05_2)
    {
        x05_2 = Mathf.Clamp(x05_2, 0.5f, 2f);
        if (x05_2 <= 1)
            return x05_2 - 0.5f;
        else
            return x05_2 / 2;
    }

    /// <summary>
    /// Smooths an unbounded value
    /// </summary>
    /// <param name="t"></param>
    /// <param name="sType"></param>
    /// <param name="duration"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="init">In π</param>
    /// <returns></returns> 
    public static float SmoothMin2Max(this float t, SmoothType sType, float duration, float min, float max, float init = 0)
    {

        if (sType == SmoothType.EaseIn)
            return min + (max - min) * (1 + Mathf.Cos(t * Mathf.PI / duration - Mathf.PI - init)) / 2;
        else if (sType == SmoothType.EaseOut)
            return min + (max - min) * (1 + Mathf.Sin(t * Mathf.PI / duration - Mathf.PI / 2 - init)) / 2;
        else
            return t;
    }

    public static void PlayRandom(this AudioSource source, AudioClip[] clips, bool interrupt, float volumeScale = 1)
    {
        if (clips == null)
            return;

        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (interrupt)
            source.Stop();

        source.PlayOneShot(clip);
    }

    public static bool ContainsInvariant(this string s, string subString)
    {
        return s.ToLower().Contains(subString.ToLower());
    }

    public static bool EqualsInvariant(this string s, string otherString)
    {
        return s.ToLower() == otherString.ToLower();
    }

    public static Transform FindChildRecursively(this Transform t, string subString)
    {
        Transform[] temp = t.GetChildrenContaining(subString, true, true);

        if (temp == null)
            return null;
        if (temp.Length == 0)
            return null;
        return temp[0];
    }

    public static List<Transform> GetOrderedChildTree(this Transform T, bool downwards = true)
    {
        List<Transform> tree = new List<Transform>();

        for (int i = 0; i < T.childCount; i++)
        {
            tree.Add(T.GetChild(i));
            tree.AddRange(T.GetChild(i).GetOrderedChildTree(true));
        }

        if (!downwards)
            tree.Reverse();

        return tree;
    }

    public static Transform[] GetChildrenContaining(this Transform T, string subString, bool matchCase, bool wholeWordsOnly)
    {
        List<Transform> list = new List<Transform>();

        foreach (Transform t in T.GetComponentsInChildren<Transform>())
        {
            if (matchCase && !t.name.ContainsInvariant(subString))
                continue;
            else if (!matchCase && !t.name.Contains(subString))
                continue;

            if (wholeWordsOnly && (t.name.Length != subString.Length))
                continue;

            list.Add(t);
        }

        return list.ToArray();
    }

    public static string PercentileToPercent(this float f, string format = "#")
    {
        string preText = "";
        if (f < 0.01f)
            preText = "0";

        return preText + (f * 100).ToString(format) + "%";
    }

    public static Vector3 GetRelativeLinearVelocity(this Rigidbody r)
    {
        return r.GetRelativeLinearVelocity(Vector3.zero);
    }

    public static Vector3 GetRelativeLinearVelocity(this Rigidbody r, Vector3 groundVelocity)
    {
        if (!r)
            return Vector3.zero;
        return r.transform.InverseTransformDirection(r.velocity - groundVelocity);
    }

    public static Vector3 GetRelativeAngularVelocity(this Rigidbody r)
    {
        return r.GetRelativeAngularVelocity(Vector3.zero);
    }

    public static Vector3 GetRelativeAngularVelocity(this Rigidbody r, Vector3 groundAngularVelocity)
    {
        if (!r)
            return Vector3.zero;
        return r.transform.InverseTransformDirection(r.angularVelocity - groundAngularVelocity);
    }

    // TODO: Add friction Invariance (for now disabling friction works)
    public static Vector3 CalculateInvariantDV(this Vector3 wantedVelocity, Vector3 currentVelocity, float control, float maxSpeedNegationPerSec, float drag, Vector3 constantForces)
    {
        Vector3 dV = Vector3.zero;
        Vector3 velGain = constantForces * Time.fixedDeltaTime;

        Vector3 goodVelocity, badVelocity;

        // Our starting Point
        dV = wantedVelocity;

        // Cancel out the drag we're going to experience
        dV /= Mathf.Clamp01(1 - drag * Time.fixedDeltaTime);

        // Cancel out our current speed
        dV -= currentVelocity;

        // Cancel out gravity
        dV -= velGain;

        // Apply drag to the extra part of our velocity
        currentVelocity.SplitVector3GoodBad(wantedVelocity, out goodVelocity, out badVelocity);
        Vector3 extraVelocity = wantedVelocity.normalized * Mathf.Max(goodVelocity.magnitude - wantedVelocity.magnitude, 0);
        dV += extraVelocity;
        dV += badVelocity * (1 - control);


        // And of our gravity
        velGain.SplitVector3GoodBad(wantedVelocity, out goodVelocity, out badVelocity);
        Vector3 extraVelGain = wantedVelocity.normalized * Mathf.Max(goodVelocity.magnitude - wantedVelocity.magnitude - extraVelocity.magnitude, 0);
        dV += extraVelGain;
        dV += badVelocity * (1 - control);



        return dV;
    }

    public static void SplitVector3GoodBad(this Vector3 v3, Vector3 relativeTo, out Vector3 good, out Vector3 bad)
    {

        float dot = Vector3.Dot(v3, relativeTo);
        good = (dot > 0) ? Vector3.Project(v3, relativeTo) : Vector3.zero;
        bad = v3 - good;

    }

    public static Vector3 TryGetVelocity(this Transform t)
    {
        Rigidbody r = t.GetComponent<Rigidbody>();
        if (!r)
            return Vector3.zero;

        return r.velocity;
    }

    public static bool isNaN(this Vector3 v3)
    {
        return float.IsNaN(v3.x) || float.IsNaN(v3.y) || float.IsNaN(v3.z);

    }

    public static bool IsInsideCircularRect(this Vector2 point, RectTransform rect)
    {
        return (point.NormalizeInRect(rect).magnitude <= 1);
    }

    public static Vector2 NormalizeInRect(this Vector2 point, RectTransform rect, float innerRadius, float outterRadius)
    {
        Vector2 _pos = point.NormalizeInRect(rect);

        Vector2 pos = _pos.normalized * Mathf.Clamp(_pos.magnitude - innerRadius, 0, outterRadius - innerRadius);

        pos /= (outterRadius - innerRadius);

        return pos;
    }

    public static Vector2 NormalizeInRect(this Vector2 point, RectTransform rect)
    {


        Vector2 scale = new Vector2(
            rect.rect.width * rect.transform.lossyScale.x,
            rect.rect.height * rect.transform.lossyScale.y);

        Vector2 center = rect.transform.position;

        Vector2 localPoint = point - center;

        Vector2 normalizedLocalPoint = new Vector2(
            localPoint.x / (scale.x / 2), localPoint.y / (scale.y / 2));

        return normalizedLocalPoint;
    }

    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }
    public static void SmoothLookAt(this Transform T, Vector3 target, float damping)
    {
        Vector3 diff = (target - T.position);
        if (diff.magnitude == 0)
            return;
        Quaternion rotation = Quaternion.LookRotation(target - T.position);
        T.rotation = Quaternion.Slerp(T.rotation, rotation, Time.deltaTime * damping);
    }


    public static void ChangeAnimClipOfState(this Animator m_animator, string animatorState, AnimationClip animationClip)
    {
        AnimatorOverrideController myOverrideController = new AnimatorOverrideController();
        myOverrideController.runtimeAnimatorController = m_animator.runtimeAnimatorController;

        myOverrideController[animatorState] = animationClip;
        // Put this line at the end because when you assign a controller on an Animator, unity rebind all the animated properties

        m_animator.runtimeAnimatorController = myOverrideController;


    }

    public static Vector2 Retargeted(this Vector2 v2, Vector2 currentMinMax, Vector2 targetMinMax, bool clamp = true)
    {

        return new Vector2(v2.x.Retargeted(currentMinMax, targetMinMax, clamp), v2.y.Retargeted(currentMinMax, targetMinMax, clamp));
    }

    public static float Retargeted(this float x, float currentMin, float currentMax, float targetMin, float targetMax, bool clamp = true)
    {
        float temp = x;

        // Normalize
        temp -= currentMin;
        temp /= (currentMax - currentMin);

        if (clamp)
            temp = Mathf.Clamp(temp, 0, 1);

        // Retarget
        temp *= (targetMax - targetMin);
        temp += targetMin;

        temp = Mathf.Clamp(temp, targetMin, targetMax);

        return temp;
    }
    public static float Retargeted(this float x, Vector2 currentMinMax, Vector2 targetMinMax, bool clamp = true)
    {
        return x.Retargeted(currentMinMax.x, currentMinMax.y, targetMinMax.x, targetMinMax.y, clamp);

    }
    public static float Clamped01(this float x)
    {
        return x.Clamped(0, 1);
    }
    public static float Clamped(this float x, float min, float max)
    {
        return Mathf.Clamp(x, min, max);
    }
    public static float Clamped(this float x, Vector2 minMax)
    {
        return Mathf.Clamp(x, minMax.x, minMax.y);
    }
    public static float RetargetedTo_01(this float x, float currentMin, float currentMax, bool clamp = true)
    {
        return x.Retargeted(currentMin, currentMax, 0, 1, clamp);
    }
    public static float RetargetedTo_01(this float x, Vector2 currentMinMax, bool clamp = true)
    {
        return x.RetargetedTo_01(currentMinMax.x, currentMinMax.y, clamp);
    }
    public static float RetargetedFrom_01(this float x, float targetMin, float targetMax, bool clamp = true)
    {
        return x.Retargeted(0, 1, targetMin, targetMax, clamp);
    }
    public static float RetargetedFrom_01(this float x, Vector2 targetMinMax, bool clamp = true)
    {
        return x.RetargetedFrom_01(targetMinMax.x, targetMinMax.y, clamp);
    }
    public static Vector3 GetShiftedAndScaled(this Vector3 _point, Vector3 center, Vector3 scale)
    {
        Vector3 point = _point;
        point -= center;

        point.x /= (scale.x);
        point.y /= (scale.y);

        return point;
    }

    public static Polar GetPolar(this Vector3 _point, Vector3 center, Vector3 scale)
    {

        Vector3 point = _point.GetShiftedAndScaled(center, scale);

        Polar polarCoords = Polar.zero;

        polarCoords.r = Mathf.Sqrt(Mathf.Pow(point.x, 2) + Mathf.Pow(point.y, 2));
        polarCoords.θ = Mathf.PI / 2 - Mathf.Atan2(point.y, point.x);

        // We are outside our system
        if (polarCoords.r > 1)
            return Polar.zero;

        return polarCoords;
    }

    public static bool IsOverUIElement()
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    public static bool IsNull(this RaycastHit hitInfo)
    {
        // Debug.Log(hitInfo.collider);
        return hitInfo.collider == null;
    }

    public static RaycastHit ScreenRaycast(Vector3 screenPos, float dist, Camera camera = null)
    {
        RaycastHit hitInfo = new RaycastHit();

        if (!camera)
            camera = Camera.main;

        Ray ray = camera.ScreenPointToRay(screenPos);
        // Debug.DrawRay(ray.origin, ray.origin + ray.direction * dist, Color.blue, 5);		

        if (Physics.Raycast(ray, out hitInfo, dist))
            return hitInfo;
        else
            return new RaycastHit();
    }

    public static RaycastHit ScreenRaycast(Vector3 screenPos, float dist, LayerMask layerMask, bool ignoreTriggers, float radius = 0, bool debug = false, Camera camera = null)
    {
        RaycastHit[] results = ScreenRaycastAll(screenPos, dist, layerMask, ignoreTriggers, radius, debug);
        if (results == null)
            return new RaycastHit();
        if (results.Length == 0)
            return new RaycastHit();
        return results[0];
    }

    public static RaycastHit[] ScreenRaycastAll(Vector3 screenPos, float dist, LayerMask layerMask, bool ignoreTriggers, float radius = 0, bool debug = false, Camera camera = null)
    {
        RaycastHit[] hitInfo = null;

        if (!camera)
            camera = Camera.main;

        if (!camera)
            camera = GetComponentInScene<Camera>();
        if (!camera)
        {
            if (Debug.isDebugBuild)
                Debug.LogError("Add a damn Camera to the scene..!!");
            return hitInfo;
        }

        Ray ray = camera.ScreenPointToRay(screenPos);
        // Debug.DrawRay(ray.origin, ray.origin + ray.direction * dist, Color.blue, 5);		

        return camera.transform.RaycastTowardsAll(ray.direction, dist, layerMask, null, ignoreTriggers, radius, debug);
    }

    public static string ToStringExtended(this float f, string format, string one, string plural)
    {
        if (f == 1)
            return f.ToString(format) + " " + one;
        else
            return f.ToString(format) + " " + plural;
    }

    /// <summary>
    /// Round to closest 
    /// </summary>
    /// <param name="f"></param>
    /// <param name="snapValues"></param>
    /// <returns></returns>
    public static float Round(this float f, float snapValues)
    {
        return Mathf.Round(f / snapValues) * snapValues;
    }
    /// <summary>
    /// foreach (Player enemy in The_Helper.GetAllObjectsNear<Player>(aC.target.position, destabilizeRange, player))
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    /// <param name="ignoreMe"></param>
    /// <returns></returns>
    public static T[] GetAllObjectsNear<T>(Vector3 position, float radius, LayerMask layerMask, T ignoreMe = null) where T : UnityEngine.Object
    {
        List<T> objects = new List<T>();
        // Find enemies in our viscinity
        foreach (Collider c in Physics.OverlapSphere(position, radius, layerMask))
        {
            T enemy = c.GetComponent<T>();

            // If it's not a player, ignore
            if (!enemy || enemy == ignoreMe) continue;

            objects.Add(enemy);
        }
        return objects.ToArray();

    }

    /// <summary>
    /// Returns null if no targets
    /// </summary>
    public static RaycastHit[] RaycastTowardsAll(this Transform T, Vector3 dir, float dist, LayerMask layerMask, string[] ignoreTags, bool ignoreTriggers, float radius = 0, bool debug = false)
    {
        List<RaycastHit> hitInfos = null;

        dir.Normalize();
        // These could be useful in the raycast towards (not All) // T.lossyScale.z / 2
        // As of v5.3.1f1 there is a where SphereCastAll ignore colliders which are close to you.
        // So we need to start the cast behind us (it's not an issue since we will ignore ourselves anyway)
        Vector3 minDisplacement = -dir * (radius + 0.01f);
        Vector3 verticalDisplacement = Vector3.zero; // T1.up * T1.lossyScale.y / 2;

        Ray ray = new Ray(T.position + minDisplacement + verticalDisplacement, dir + dir.normalized * minDisplacement.magnitude);
        dist -= minDisplacement.magnitude;

        if (debug)
            Debug.DrawRay(ray.origin, ray.direction * dist, Color.blue, 3);

        if (radius == 0)
            hitInfos = Physics.RaycastAll(ray, dist, layerMask).ToList();
        else
            hitInfos = Physics.SphereCastAll(ray, radius, dist, layerMask).ToList();

        // Remove everything which is behind us and order the remaining by distance
        // Required to fix the aforementioned bug & for RaycastTowards to just pick up [0]
        hitInfos = hitInfos
                    .Where(h => Vector3.Dot(h.point - T.position, dir) > 0)
                    .OrderBy(h => h.distance).ToList();

        // Ignore everything in the ignoreTags
        if (ignoreTags != null)
        {
            List<RaycastHit> ignoreHits = new List<RaycastHit>();
            Collider[] collidersToIgnore = hitInfos.GetAllColliders(true).FilterByTags(KeepOrRemove.Keep, ignoreTags).ToArray();
            foreach (RaycastHit rH in hitInfos)
                if (collidersToIgnore.Contains(rH.collider) || (rH.collider.isTrigger && ignoreTriggers))
                    ignoreHits.Add(rH);

            hitInfos = hitInfos.RemoveRange(ignoreHits).ToList();
        }


        // Make sure we don't check against ourselves
        List<RaycastHit> selfHits = new List<RaycastHit>();
        foreach (RaycastHit rH in hitInfos)
            if (T.GetComponentsInChildren<Collider>().Contains(rH.collider))
                selfHits.Add(rH);

        hitInfos.RemoveRange(selfHits);

#if DEBUG_EXTENSIONS
        if (debug)
            foreach (RaycastHit rH in hitInfos)
                DebugExtension.DebugPoint(rH.point, Color.red, 0.1f, 3);
#endif

        return hitInfos.ToArray();
    }

    /// <summary>
    /// Check with hitInfo.IsNull()
    /// </summary>
    /// <param name="T"></param>
    /// <param name="dir"></param>
    /// <param name="dist"></param>
    /// <param name="layerMask"></param>
    /// <param name="radius"></param>
    /// <param name="debug"></param>
    /// <returns></returns>
    public static RaycastHit RaycastTowards(this Transform T, Vector3 dir, float dist, LayerMask layerMask, string[] ignoreTags, bool ignoreTriggers, float radius = 0, bool debug = false)
    {
        RaycastHit[] results = T.RaycastTowardsAll(dir, dist, layerMask, ignoreTags, ignoreTriggers, radius, debug);
        if (results == null)
            return new RaycastHit();
        if (results.Length == 0)
            return new RaycastHit();
        return results[0];
    }

    public static bool HasLineOfSight(this Transform a, Transform b, LayerMask layerMask)
    {
        Vector3 diff = b.position - a.position;
        RaycastHit info = a.RaycastTowards(diff.normalized, diff.magnitude, layerMask, null, true, a.lossyScale.x / 2, true);
        // We didn't hit anything on the way
        if (info.IsNull())
            return true;
        if (info.transform != b.transform)
            return false;
        return true;
    }

    public static bool HasLineOfSight(this Transform a, Transform b, string[] ignoreTags, bool debug = false)
    {
        Vector3 diff = b.position - a.position;
        RaycastHit[] info = a.RaycastTowardsAll(diff.normalized, diff.magnitude, -1, ignoreTags, true, a.lossyScale.x / 2, debug);

        List<Collider> intersectingColliders = info.GetAllColliders(false).ToList();
        //            .FilterByTags(KeepOrRemove.Remove, ignoreTags).ToList();

        // We didn't hit anything on the way
        if (intersectingColliders.Count == 0)
            return true;

        // We hit something, is it only our target?
        Collider targetCollider = b.GetComponent<Collider>();
        if (intersectingColliders.Count == 1 && targetCollider != null && intersectingColliders.Contains(targetCollider))
            return true;

        // There's definitely something else in the way
        return false;
    }

    /// <summary>
    /// Returns null if no targets
    /// </summary>
    /// <param name="T"></param>
    /// <param name="dist"></param>
    /// <param name="layerMask"></param>
    /// <param name="radius"></param>
    /// <param name="debug"></param>
    /// <returns></returns>
    public static RaycastHit[] RaycastForwardAll(this Transform T, float dist, LayerMask layerMask, bool ignoreTriggers, string[] ignoreTags, float radius = 0, bool debug = false)
    {
        return T.RaycastTowardsAll(T.forward, dist, layerMask, ignoreTags, ignoreTriggers, radius, debug);
    }

    /// <summary>
    /// Check with hitInfo.IsNull()
    /// </summary>
    /// <param name="T"></param>
    /// <param name="dist"></param>
    /// <param name="layerMask"></param>
    /// <param name="radius"></param>
    /// <param name="debug"></param>
    /// <returns></returns>
    public static RaycastHit RaycastForward(this Transform T, float dist, LayerMask layerMask, string[] ignoreTags, bool ignoreTriggers, float radius = 0, bool debug = false)
    {
        return T.RaycastTowards(T.forward, dist, layerMask, ignoreTags, ignoreTriggers, radius, debug);
    }

    /// <summary>
    /// Smooths the first, the latter or both halfs of a 01 clamped value.
    /// </summary>
    /// <param name="percentile"></param>
    /// <param name="smoothType"></param>
    /// <returns></returns>
    public static float Smooth01(this float percentile, SmoothType smoothType)
    {

        float value = 0;

        float sign = Mathf.Sign(percentile);

        percentile *= sign;

        percentile = Mathf.Clamp01(percentile);

        if (smoothType == SmoothType.None)
            value = percentile;
        else if (percentile <= 0.5f
            && (smoothType == SmoothType.EaseIn || smoothType == SmoothType.EaseInOut))
            value = Mathf.Sin(percentile * Mathf.PI - Mathf.PI / 2) / 2 + 0.5f;
        else if (percentile >= 0.5f
            && (smoothType == SmoothType.EaseOut || smoothType == SmoothType.EaseInOut))
            value = Mathf.Sin(percentile * Mathf.PI / 2) / 2 + 0.5f;
        else if (smoothType == SmoothType.Exp)
            value = (Mathf.Exp(percentile) - Mathf.Exp(0)) / Mathf.Exp(1);
        else if (smoothType == SmoothType.Log)
            value = Mathf.Log(percentile + 1) / Mathf.Log(2);
        else
            value = percentile;

        return sign * value;
    }

    public static Color AdjustBrightness(this Color color, float newValue)
    {
        float currentBrightness = color.grayscale;
        if (currentBrightness == 0)
            return new Color(newValue, newValue, newValue, color.a);
        // Debug.Log(string.Format("Brightness: {0} to {1}", currentBrightness, newValue));
        return new Color(
            color.r / currentBrightness * newValue,
            color.g / currentBrightness * newValue,
            color.b / currentBrightness * newValue, color.a);
    }

    public static void ChangeColorProperty(this Material material, ColorProperty property, float newValue)
    {
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", material.GetColor("_Color").Change(property, newValue));

        if (material.HasProperty("_TintColor"))
            material.SetColor("_TintColor", material.GetColor("_TintColor").Change(property, newValue));

    }

    public static Color Change(this Color color, ColorProperty property, float newValue)
    {
        newValue = Mathf.Clamp(newValue, 0, 1);

        switch (property)
        {
            case ColorProperty.r:
                return new Color(newValue, color.g, color.b, color.a);
            case ColorProperty.g:
                return new Color(color.r, newValue, color.b, color.a);
            case ColorProperty.b:
                return new Color(color.r, color.g, newValue, color.a);
            case ColorProperty.a:
                return new Color(color.r, color.g, color.b, newValue);

        }
        return color;
    }

    public static string ToHex(this Color color)
    {
        Color32 _color = (Color32)color;
        string hex = _color.r.ToString("X2") + _color.g.ToString("X2") + _color.b.ToString("X2") + _color.a.ToString("X2");
        return "#" + hex.ToLower();
    }

    public static Color ToColor(this string hex)
    {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        byte a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, a);
    }

    public static string AddSpacesBeforeCapitals(this string input)
    {
        return input.AddSubstringBeforeCapitals(" ");
    }

    public static string AddSubstringBeforeCapitals(this string input, string substring)
    {
        return Regex.Replace(input, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", substring + "$0");
    }

    public static string AddApostropheBeforeS(this string input)
    {
        return Regex.Replace(input, @"s ", "'s ");
    }

    // From:
    // http://net-informations.com/q/faq/remove.html
    public static string RemoveSpaces(this string input)
    {
        return Regex.Replace(input, @"\s", "");
    }

    public static void Reset(this Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
    }

    public static void ReparentAndReset(this Transform t, Transform newParent, bool isUI = false)
    {
        t.SetParent(newParent, !isUI);
        t.Reset();
    }
    public static void Reparent(this GameObject t, Transform newParent, bool isUI = false)
    {
        if (t != null)
            t.transform.ReparentAndReset(newParent, isUI);
    }
    public static bool IsBetween(this float v, float from, float to)
    {
        return (v >= Mathf.Min(from, to) && v <= Mathf.Max(from, to));
    }
    public static bool IsBetween(this int v, float from, float to)
    {
        return (v >= Mathf.Min(from, to) && v <= Mathf.Max(from, to));
    }
    public static Color InterpolateColor(Color colorA, Color colorB, float t)
    {
        t = Mathf.Clamp01(t);

        Color c = new Color();

        c.r = colorA.r * (1 - t) + colorB.r * t;
        c.g = colorA.g * (1 - t) + colorB.g * t;
        c.b = colorA.b * (1 - t) + colorB.b * t;
        c.a = colorA.a * (1 - t) + colorB.a * t;

        return c;
    }

#if UNITY_EDITOR
    [MenuItem("Editor Tools/Export Package with Project Settings")]
#endif
    public static void ExportPackage()
    {
#if UNITY_EDITOR
        string[] projectContent = new string[] { "Assets/Gameplay", "ProjectSettings/InputManager.asset" };
        AssetDatabase.ExportPackage(projectContent, "_Gameplay.unitypackage", ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
        Debug.Log("Project Exported");
#endif
    }

    public static Coroutine CheckInternetConnection(Action<bool> callback)
    {
        if (The_Helper_MB.instance)
            return The_Helper_MB.instance.StartCoroutine(The_Helper_MB.instance.
                CheckInternetConnection(callback));
        return null;
    }

    public static Coroutine SmoothLookAt(this Transform t, Vector3 pos, Vector3 up, float timeTo, Action<bool> callback = null)
    {
        if (The_Helper_MB.instance)
            return The_Helper_MB.instance.StartCoroutine(The_Helper_MB.instance.
                SmoothLookAt(t, pos, up, timeTo, ConflictResolutionStrategy.Interrupt, callback));
        return null;
    }
    public static Coroutine InterpolateScale<T>(this T component, Vector3 scaleTo, float timeTo, bool andBack, Action<bool> callback = null) where T : Component
    {
        if (component == null) return null;
        if (The_Helper_MB.instance)
            return The_Helper_MB.instance.StartCoroutine(The_Helper_MB.instance.
                InterpolateScale(component.gameObject, scaleTo, timeTo, andBack, ConflictResolutionStrategy.Interrupt, callback));
        return null;
    }
    public static Coroutine InterpolateVolume(this AudioSource aS, float volumeTo, float timeTo, bool andBack, Action<bool> callback = null)
    {
        if (The_Helper_MB.instance)
            return The_Helper_MB.instance.StartCoroutine(The_Helper_MB.instance.
                InterpolateVolume(aS, volumeTo, timeTo, andBack, ConflictResolutionStrategy.Interrupt, callback));
        return null;
    }
    /// <summary>
    /// Pass in timesToCall negative to call endlessly
    /// success => { if(!this) return; Debug.Log(success? "Success" : "Fail"); 
    /// </summary>
    public static Coroutine StartTimer(float dT, Action<bool> callback, int timesToCall = 1)
    {
        if (The_Helper_MB.instance)
            return The_Helper_MB.instance.StartCoroutine(The_Helper_MB.instance.
                StartTimer(dT, callback, timesToCall));
        return null;
    }

    public static void StopCoroutine(this Coroutine c)
    {
        if (!The_Helper_MB.instance || c == null) return;
        The_Helper_MB.instance.StopCoroutine(c);
    }

    public static void Debug_OnGUI_AddMessage(string message, float time = Mathf.Infinity, bool showInReleaseMode = false)
    {
        if (!showInReleaseMode && !Debug.isDebugBuild) return;

        if (The_Helper_MB.instance)
            The_Helper_MB.instance.OnGUI_AddMessage(message, time);
    }
    public static void Debug_OnGUI_Clear()
    {
        if (The_Helper_MB.instance)
            The_Helper_MB.instance.OnGUI_Clear();
    }
}

/// <summary>
/// Helper for Coroutines -> Expose them neatly above in The_Helper:
/// 
/// Coroutine (in The_Helper_MB): 
/*
        public IEnumerator InterpolateScale(GameObject gO, float scaleTo, float timeTo, bool andBack, Action<bool> callback = null)
        {
            bool result = false;

            // .. Do Stuff..
            
            if(callback != null)
                callback(result);
        }
*/
/// 
/// Exposed (in The_Helper): 
/*
        // callback is called when the function ends
        public static void InterpolateScale(this GameObject gO, float scaleTo, float timeTo, bool andBack, Action<bool> callback = null)
        { 
            The_Helper_MB.instance.StartCoroutine(The_Helper_MB.instance.InterpolateScale(gO, scaleTo, timeTo, andBack, callback));
        } 
*/
/// Call (from other scripts):
/*
        // Simple Call (no Callback)
        gO.InterpolateScale(1f, 1, false);
        
        // With defined Callback
        gO.InterpolateScale(1f, 1, false, OnDone);

        OnDone (bool success)
        {
            Debug.Log(success? "Success" : "Fail");
        }

        // Alternative (Shorthand):
        gO.InterpolateScale(1f, 1, false,
            success => { Debug.Log(success? "Success" : "Fail"); }
        );
*/
/// </summary>
public class The_Helper_MB : Singleton<The_Helper_MB>
{
    private static List<UnityEngine.Object> LOCKED_OBJECTS;

    public static EventHandler<EventArgs> onUpdate { get { return instance._onUpdate; } set { instance._onUpdate = value; } }
    private EventHandler<EventArgs> _onUpdate;

    protected override void Initialize()
    {
        LOCKED_OBJECTS = new List<UnityEngine.Object>();
        messages = new List<TimedMessage>();

        DontDestroyOnLoad(instance.gameObject);
    }
    protected override void DeInitialize()
    {
        LOCKED_OBJECTS.Clear();
        messages.Clear();
    }

    void Update()
    {
        if (onUpdate != null)
            onUpdate(this, new EventArgs());
    }

    public void OnGUI_AddMessage(string message, float time = Mathf.Infinity)
    {
        if (message.CompareTo("") == 0 || time == 0) return;

        TimedMessage tM = new TimedMessage();
        tM = new TimedMessage(instance, message, a => { messages.Remove(tM); tM = null; }, time);
        messages.Add(tM);
    }

    public void OnGUI_Clear()
    {
        messages.Clear();
    }

    List<TimedMessage> messages = new List<TimedMessage>();

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(30, 30, Screen.width, Screen.height));
        foreach (TimedMessage tM in messages)
            if (tM != null && tM.isValid)
                GUILayout.Label(tM.message);

        GUILayout.EndArea();
    }

    // Improvements: Enum for return value, list of callbacks so that when we do check, we notify all of them
    static bool CheckingConnection = false;
    public IEnumerator CheckInternetConnection(Action<bool> callback)
    {
        if (CheckingConnection)
        {
            if (callback != null)
                callback(false);
            this.Log("CheckInternetConnection -> Already Checking");
            yield break;
        }
        CheckingConnection = true;

        WWW www = new WWW("http://www.google.com");

        float t = 0;
        while (!www.isDone)
        {
            t += Time.deltaTime;
            if (t > 15)
            {
                if (callback != null)
                    callback(false);
                this.Log("CheckInternetConnection -> Timed Out");
                CheckingConnection = false;
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }
        if (www.error != null)
        {
            if (callback != null)
                callback(false);
            this.Log("CheckInternetConnection -> Error\n" + www.error);
        }
        else {
            if (callback != null)
                callback(true);
            this.Log("CheckInternetConnection -> Success");
        }
        CheckingConnection = false;
    }

    public IEnumerator StartTimer(float dT, Action<bool> callback, int timesToCall = 1)
    {
        // To
        bool loopEndlessly = (timesToCall <= 0);
        while (loopEndlessly || timesToCall > 0)
        {
            yield return new WaitForSeconds(dT);
            if (callback != null)
                callback(true);
            timesToCall--;
        }
    }

    public IEnumerator InterpolateVolume(AudioSource aS, float volumeTo, float timeTo, bool andBack, ConflictResolutionStrategy conflictResolutionStrategy, Action<bool> callback = null)
    {
        if (conflictResolutionStrategy == ConflictResolutionStrategy.Wait)
            while (!OBJECT_TRY_LOCK(aS)) yield return new WaitForEndOfFrame();
        else if (conflictResolutionStrategy == ConflictResolutionStrategy.Abort)
        {
            if (!OBJECT_TRY_LOCK(aS)) yield break;
        }
        else if (conflictResolutionStrategy == ConflictResolutionStrategy.Interrupt)
        {
            OBJECT_UNLOCK(aS);
            yield return new WaitForEndOfFrame();
            while (!OBJECT_TRY_LOCK(aS)) yield return new WaitForEndOfFrame();
        }
        if (aS)
        {

            float baseVolume = aS.volume;

            // To
            yield return StartCoroutine(InterpolateVolume(aS, volumeTo, timeTo));

            // And Back
            if (andBack && OBJECT_IS_LOCKED(aS))
                yield return StartCoroutine(InterpolateVolume(aS, baseVolume, timeTo));
        }

        OBJECT_UNLOCK(aS);

        if (callback != null)
            callback(OBJECT_IS_LOCKED(aS));
    }


    public IEnumerator SmoothLookAt(Transform T, Vector3 pos, Vector3 up, float timeTo, ConflictResolutionStrategy conflictResolutionStrategy, Action<bool> callback = null)
    {
        if (conflictResolutionStrategy == ConflictResolutionStrategy.Wait)
            while (!OBJECT_TRY_LOCK(T)) yield return new WaitForEndOfFrame();
        else if (conflictResolutionStrategy == ConflictResolutionStrategy.Abort)
        {
            if (!OBJECT_TRY_LOCK(T)) yield break;
        }
        else if (conflictResolutionStrategy == ConflictResolutionStrategy.Interrupt)
        {
            OBJECT_UNLOCK(T);
            yield return new WaitForEndOfFrame();
            while (!OBJECT_TRY_LOCK(T)) yield return new WaitForEndOfFrame();
        }

        if (T)
        {
            if ((pos - T.position).magnitude < 0.005f)
                yield break;

            Quaternion wantedRotation = Quaternion.LookRotation(pos - T.position, up);

            for (float t = 0; t < timeTo; t += Time.deltaTime)
            {
                if (!OBJECT_IS_LOCKED(T)) break;
                if (T)
                {
                    Quaternion rot = Quaternion.Slerp(T.rotation, wantedRotation, (t / timeTo).Clamped01());
                    // Debug.Log(val);
                    T.rotation = rot;
                }
                yield return new WaitForEndOfFrame();
            }

            if (T) T.rotation = wantedRotation;
        }

        OBJECT_UNLOCK(T);

        if (callback != null)
            callback(OBJECT_IS_LOCKED(T));
    }
    public IEnumerator InterpolateScale(GameObject gO, Vector3 scaleTo, float timeTo, bool andBack, ConflictResolutionStrategy conflictResolutionStrategy, Action<bool> callback = null)
    {
        if (conflictResolutionStrategy == ConflictResolutionStrategy.Wait)
            while (!OBJECT_TRY_LOCK(gO)) yield return new WaitForEndOfFrame();
        else if (conflictResolutionStrategy == ConflictResolutionStrategy.Abort)
        {
            if (!OBJECT_TRY_LOCK(gO)) yield break;
        }
        else if (conflictResolutionStrategy == ConflictResolutionStrategy.Interrupt)
        {
            OBJECT_UNLOCK(gO);
            yield return new WaitForEndOfFrame();
            while (!OBJECT_TRY_LOCK(gO)) yield return new WaitForEndOfFrame();
        }

        if (gO)
        {
            Vector3 baseScale = gO.transform.localScale;

            // To
            yield return StartCoroutine(InterpolateScale(gO, scaleTo, timeTo));

            // And Back
            if (andBack && OBJECT_IS_LOCKED(gO))
                yield return StartCoroutine(InterpolateScale(gO, baseScale, timeTo));
        }

        OBJECT_UNLOCK(gO);

        if (callback != null)
            callback(OBJECT_IS_LOCKED(gO));

    }

    private IEnumerator InterpolateScale(GameObject gO, Vector3 scaleTo, float time)
    {
        if (!gO)
            yield break;

        Vector3 scaleFrom = gO.transform.localScale;
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            // We lost the Lock
            if (!OBJECT_IS_LOCKED(gO)) yield break;

            Vector3 val = Vector3.Lerp(scaleFrom, scaleTo, (t / time).Clamped01());
            // Debug.Log(val);
            if (gO)
                gO.transform.localScale = val;


            yield return new WaitForEndOfFrame();
        }
        if (gO)
            gO.transform.localScale = scaleTo;
    }

    private IEnumerator InterpolateVolume(AudioSource aS, float volumeTo, float time)
    {
        float volumeFrom = aS.volume;

        for (float t = 0; t < time; t += Time.deltaTime)
        {
            // We lost the Lock
            if (!OBJECT_IS_LOCKED(aS)) yield break;

            float val = volumeFrom + (volumeTo - volumeFrom) * (t / time).Clamped01();
            // Debug.Log(val);
            if (aS)
                aS.volume = val;

            yield return new WaitForEndOfFrame();
        }
        if (aS)
            aS.volume = volumeTo;
    }

    private static bool OBJECT_IS_LOCKED(UnityEngine.Object obj)
    {
        return LOCKED_OBJECTS.Contains(obj);
    }

    private static bool OBJECT_TRY_LOCK(UnityEngine.Object obj)
    {
        if (OBJECT_IS_LOCKED(obj))
        {
            // instance.Log(obj + " was locked.", LogType.Warning);
            return false;
        }

        LOCKED_OBJECTS.Add(obj);
        return true;
    }

    private static void OBJECT_UNLOCK(UnityEngine.Object obj)
    {
        if (OBJECT_IS_LOCKED(obj))
            LOCKED_OBJECTS.Remove(obj);
    }
}

/*
 public class Job : ThreadedJob
 {
     public Vector3[] InData;  // arbitary job data
     public Vector3[] OutData; // arbitary job data
 
     protected override void ThreadFunction()
     { 
     }
     protected override void OnFinished()
     { 
     }

 }
  Job myJob;
 void Start ()
 {
     myJob = new Job(inData);
     myJob.Start(); // Don't touch any data in the job class after you called Start until IsDone is true.
 }
 void Update()
 {
     if (myJob != null)
     {
         if (myJob.Update())
         {
             // Alternative to the OnFinished callback
             myJob = null;
         }
     }
 }
  yield return StartCoroutine(myJob.WaitFor());
 */

public enum ConflictResolutionStrategy { Wait = 0, Interrupt, Abort }

public enum KeepOrRemove { Keep = 0, Remove }

public enum MinMax { Min = 0, Max }

public enum VelocityType { Linear, Angular }

public enum SmoothType { None, EaseInOut, EaseIn, EaseOut, Exp, Log }

public enum ColorProperty { r, g, b, a }

public enum BlendMode { Opaque, Cutout, Fade, Transparent }

public enum Direction_1D { Left, Right }

public enum Direction_2D { Left, Right, Up, Down }

public enum Direction_3D { Left, Right, Up, Down, Forward, Backward }

public enum AndroidMarketPlace { Unknown = 0, GooglePlay, Amazon, SamsungStore }

public struct Polar
{
    public static Polar zero { get { Polar polar; polar.r = 0; polar.θ = 0; return polar; } }
    public float r;
    public float θ;
    public float a
    {
        get { return θ * 360 / (2 * Mathf.PI); }
        set { θ = (value / 360) * 2 * Mathf.PI; }
    }
}

public class TimedMessage
{
    public TimedMessage() { }
    public TimedMessage(MonoBehaviour caller, string message, Action<bool> callback, float timeRemaining = Mathf.Infinity)
    {
        this.message = message;
        this.timeRemaining = timeRemaining;
        caller.StartCoroutine(Countdown(callback));
    }
    ~TimedMessage() { message = null; timeRemaining = 0; }

    public float timeRemaining = Mathf.Infinity;
    public string message;

    public IEnumerator Countdown(Action<bool> callback)
    {
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        message = null;
        timeRemaining = 0;
        callback(true);
    }

    public bool isValid
    {
        get
        {
            return message != null && message.CompareTo("") != 0 && timeRemaining > 0;
        }
    }
}


[Serializable]
/// <summary>
/// Derive a new class from this base class before use:
/// [Serializable] public class DictionaryOfStringAndInt : SerializableDictionary<string, int>
/// {
///     public DictionaryOfStringAndInt() : base() { }
///     public DictionaryOfStringAndInt(Dictionary<string, int> obj) : base(obj) { }
/// }
/// </summary> 
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private int customCount = 0;

    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public SerializableDictionary() { }
    public SerializableDictionary(Dictionary<TKey, TValue> obj)
    {
        keys = new List<TKey>();
        values = new List<TValue>();

        foreach (KeyValuePair<TKey, TValue> kVP in obj)
        {
            keys.Add(kVP.Key);
            values.Add(kVP.Value);
        }
        OnAfterDeserialize();
    }

    // save the dictionary to lists
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // load dictionary from lists
    public void OnAfterDeserialize()
    {
        this.Clear();

        if (customCount > 0)
        {
            keys.Add(default(TKey));
            customCount = 0;
        }

        if (keys.Count > values.Count)
        {
            for (int i = 0; i < keys.Count - values.Count; i++)
                values.Add(default(TValue));
        }
        if (keys.Count != values.Count)
        {
            throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

        }

        for (int i = 0; i < keys.Count; i++)
            this.Add(keys[i], values[i]);
    }
}

public class TriggerCheck : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (TC_OnTriggerEnter != null)
            TC_OnTriggerEnter(this, other);
    }
    void OnTriggerExit(Collider other)
    {
        if (TC_OnTriggerExit != null)
            TC_OnTriggerExit(this, other);
    }
    void OnTriggerStay(Collider other)
    {
        if (TC_OnTriggerStay != null)
            TC_OnTriggerStay(this, other);
    }
    void OnCollisionEnter(Collision other)
    {
        if (TC_OnCollisionEnter != null)
            TC_OnCollisionEnter(this, other);
    }
    void OnCollisionExit(Collision other)
    {
        if (TC_OnCollisionExit != null)
            TC_OnCollisionExit(this, other);
    }
    void OnCollisionStay(Collision other)
    {
        if (TC_OnCollisionStay != null)
            TC_OnCollisionStay(this, other);
    }

    public delegate void OnTriggerChangeHandler(TriggerCheck self, Collider other);
    public OnTriggerChangeHandler TC_OnTriggerEnter, TC_OnTriggerExit, TC_OnTriggerStay;
    public delegate void OnCollisionChangeHandler(TriggerCheck self, Collision other);
    public OnCollisionChangeHandler TC_OnCollisionEnter, TC_OnCollisionExit, TC_OnCollisionStay;
}

/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// 
/// As a note, this is made as MonoBehaviour because we need Coroutines.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    // For Sanity check - should always be 1
    private static int members = 0;

    private static object _lock = new object();

    public static T ForceCreate()
    {
        return instance;
    }

    public static T instance
    {
        get
        {
            if (applicationIsQuitting && Application.isPlaying)
            {
#if UNITY_EDITOR
                //   Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                //      "' already destroyed on application quit." +
                //       " Won't create again - returning null.");
#endif
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (members > 1)
                    {
#if UNITY_EDITOR
                        Debug.LogError("[Singleton] Something went really wrong " +
                            " - there should never be more than 1 singleton!" +
                            " Reopenning the scene might fix it.");
#endif
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();

                        // DontDestroyOnLoad(singleton);
#if UNITY_EDITOR
                        //                        if (Application.isPlaying)
                        //                            Debug.Log("[Singleton] An instance of " + typeof(T) +
                        //                                " is needed in the scene, so '" + singleton +
                        //                                "' was created with DontDestroyOnLoad.");
#endif
                    }
                    else
                    {
#if UNITY_EDITOR
                        //                        Debug.Log("[Singleton] Using instance already created: " +
                        //                            _instance.gameObject.name);
#endif
                    }
                }

                return _instance;
            }
        }
    }

    private void Awake()
    {
        // Already have a member - who the fuck are you?
        if (members > 0)
        {
            this.Log("Already have an instance of this singleton. Destroying.", LogType.Warning);
            Destroy(this);
            return;
        }
        applicationIsQuitting = false;
        members++;
        Initialize();
    }

    protected virtual void Initialize() { DontDestroyOnLoad(gameObject); }
    protected virtual void DeInitialize() { }

    protected static bool applicationIsQuitting = false;
    /// <summary>
    /// When Unity quits, it destroys objects in a random order.
    /// In principle, a Singleton is only destroyed when application quits.
    /// If any script calls Instance after it have been destroyed, 
    ///   it will create a buggy ghost object that will stay on the Editor scene
    ///   even after stopping playing the Application. Really bad!
    /// So, this was made to be sure we're not creating that buggy ghost object.
    /// </summary>
    public void OnDestroy()
    {
        StopAllCoroutines();
        if (_instance == this) members--;
        if (members == 0)
        {
            applicationIsQuitting = true;
            _instance = null;
        }
        DeInitialize();
    }
}

public class Filter_PeakDetection
{
    private List<float> pastValues = new List<float>();
    public int windowSize;
    public float threshold;
    public bool additiveThreshold = false;

    /// <summary>
    /// The larger the window, the harder it is for a peak to register (favors global maxima). This filter has a delay of windowSize / 2.
    /// </summary>
    /// <param name="windowSize"></param>
    public Filter_PeakDetection(int windowSize, float threshold, bool additiveThreshold)
    {
        this.windowSize = windowSize;
        this.threshold = threshold;
        this.additiveThreshold = additiveThreshold;
    }

    public bool AddAndQuery(float newValue, bool debug = false)
    {
        pastValues.Add(newValue);

        // We are only interested if we found a peak.
        // We will detect the peak when it is at -WINDOW_SIZE / 2
        // Tested with intensity = Mathf.Cos(2 * Mathf.PI * Time.time); (peak every second)
        int midIdx = pastValues.Count - windowSize;
        bool isPeak = true;
        float diff = 0;
        if (pastValues.Count >= 2 * windowSize)
        {
            for (int i = pastValues.Count - 1; i >= pastValues.Count - 2 * windowSize; i--)
            {
                if (i == midIdx) continue;
                diff += Mathf.Abs(pastValues[midIdx] - pastValues[i]);
                isPeak &= (pastValues[midIdx] > pastValues[i]);
            }

            // The peak has to be high above the average value
            if (additiveThreshold)
                isPeak &= pastValues[midIdx] > pastValues.Average() + threshold;
            else
            {
                float avg = pastValues.Average();
                if (avg > 0)
                    isPeak &= pastValues[midIdx] > pastValues.Average() * threshold;
                else
                    isPeak &= pastValues[midIdx] > pastValues.Average() / threshold;
            }
        }

        if (isPeak && debug)
            this.Log("Peak!");

        return isPeak;
    }
}

public class Filter_RunningAverage
{
    public Filter_RunningAverage(float newObservationWeight, float amplifier)
    {
        this.newObservationWeight = newObservationWeight; this.amplifier = amplifier;
    }

    public float runningAverage = float.NaN;
    public float newObservationWeight = 0.5f;
    private float amplifier = 1;

    public float AddAndQuery(float newObservation)
    {
        float val = newObservation;
        // Recalculate the average
        if (float.IsNaN(runningAverage))
            runningAverage = val;
        else
            runningAverage = Mathf.Lerp(runningAverage,
                val, newObservationWeight);

        // Amplify it (to see what's going on)
        val -= runningAverage;
        val *= amplifier;
        val += runningAverage;

        return val;
    }
}

public class Filter_IIR
{
    private bool initialized = false;

    List<float> pastInputValues = new List<float>(), pastOutputValues = new List<float>();
    List<float> a = new List<float>(), b = new List<float>();
    float a0;

    public void MakeBandpass_Min_Max(float f_min, float f_max, float f_sample)
    {
        if (f_max < f_min)
        {
            float temp = f_min;
            f_min = f_max;
            f_max = temp;
        }
        MakeBandpass_Center_Bandwidth((f_min + f_max) / 2f, f_max - f_min, f_sample);
    }
    // http://dspguide.com/ch19/3.htm
    // The narrowest bandwidth that can be obtain with
    // single precision is about 0.0003 of the sampling frequency
    public void MakeBandpass_Center_Bandwidth(float f_center, float bandwidth, float f_sample)
    {
        if (f_sample <= 0 || f_center <= 0 || bandwidth <= 0)
        {
            Debug.Log("All inputs must be positive!");
            return;
        }
        if (f_center > f_sample / 2f || bandwidth > f_sample / 2f)
        {
            Debug.Log("Insufficient sampling frequency!");
            return;
        }

        // Express everything as a fraction of f_sample
        f_center /= f_sample;
        bandwidth /= f_sample;

        float cos_two_PI_f = Mathf.Cos(2 * Mathf.PI * f_center);

        float R = 1 - 3 * bandwidth;
        float R_squared = Mathf.Pow(R, 2);

        float K = (1 - 2 * R * cos_two_PI_f + R_squared)
            / (2 - 2 * cos_two_PI_f);

        a.Clear();
        b.Clear();

        a0 = 1 - K;
        a.Add(2 * (K - R) * cos_two_PI_f);
        a.Add(R_squared);

        b.Add(2 * R * cos_two_PI_f);
        b.Add(-R_squared);

        a.SetCapacity(a.Count);
        b.SetCapacity(b.Count);

        pastInputValues.SetCapacity(a.Count);
        pastOutputValues.SetCapacity(a.Count);

        initialized = true;
    }

    public Filter_IIR() { }

    /// <summary>
    /// Initialize a new filter
    /// </summary>
    /// <param name="a">Input Coefficients</param>
    /// <param name="b">Output Coefficients</param>
    public Filter_IIR(float a0, List<float> a, List<float> b)
    {
        if (a.Count != b.Count)
        {
            Debug.LogError("Different count in filter past values " + a.Count + " vs " + b.Count);
        }
        pastInputValues.SetCapacity(a.Count);
        pastOutputValues.SetCapacity(a.Count);

        initialized = true;
    }

    /// <summary>
    /// Advances a moment in the filter.
    /// </summary>
    /// <param name="newInputValue">The next input received (x0)</param>
    /// <returns>The latest output (y0). Returns the input anything goes wrong.</returns>
    public float AddAndQuery(float newInputValue)
    {
        if (!initialized)
        {
            Debug.LogError("Filter not initialized!");
            return newInputValue;
        }

        float weightSum = 0;

        float newOutputValue = a0 * newInputValue;
        weightSum += a0;

        for (int i = 0; i < pastInputValues.Count; i++)
        {
            // Debug.Log(i + " / " + pastInputValues.Count);
            newOutputValue += a[a.Count - i - 1] * pastInputValues[i];
            newOutputValue += b[b.Count - i - 1] * pastOutputValues[i];

            // weightSum += a[i] + b[i];
        }

        // newOutputValue /= weightSum;

        pastInputValues.Enqueue(newInputValue);

        pastOutputValues.Enqueue(newOutputValue);

        // Wait until we are ready
        if (pastInputValues.Count == pastInputValues.Capacity)
            return newOutputValue;
        else
            return newInputValue;
    }

    public enum Type { Bandpass }

}

[Serializable]
public struct HSBColor
{
    public float h;
    public float s;
    public float b;
    public float a;

    public HSBColor(float h, float s, float b, float a)
    {
        this.h = h;
        this.s = s;
        this.b = b;
        this.a = a;
    }

    public HSBColor(float h, float s, float b)
    {
        this.h = h;
        this.s = s;
        this.b = b;
        this.a = 1f;
    }

    public HSBColor(Color col)
    {
        HSBColor temp = FromColor(col);
        h = temp.h;
        s = temp.s;
        b = temp.b;
        a = temp.a;
    }

    public static HSBColor FromColor(Color color)
    {
        HSBColor ret = new HSBColor(0f, 0f, 0f, color.a);

        float r = color.r;
        float g = color.g;
        float b = color.b;

        float max = Mathf.Max(r, Mathf.Max(g, b));

        if (max <= 0)
        {
            return ret;
        }

        float min = Mathf.Min(r, Mathf.Min(g, b));
        float dif = max - min;

        if (max > min)
        {
            if (g == max)
            {
                ret.h = (b - r) / dif * 60f + 120f;
            }
            else if (b == max)
            {
                ret.h = (r - g) / dif * 60f + 240f;
            }
            else if (b > g)
            {
                ret.h = (g - b) / dif * 60f + 360f;
            }
            else
            {
                ret.h = (g - b) / dif * 60f;
            }
            if (ret.h < 0)
            {
                ret.h = ret.h + 360f;
            }
        }
        else
        {
            ret.h = 0;
        }

        ret.h *= 1f / 360f;
        ret.s = (dif / max) * 1f;
        ret.b = max;

        return ret;
    }

    public static Color ToColor(HSBColor hsbColor)
    {
        float r = hsbColor.b;
        float g = hsbColor.b;
        float b = hsbColor.b;
        if (hsbColor.s != 0)
        {
            float max = hsbColor.b;
            float dif = hsbColor.b * hsbColor.s;
            float min = hsbColor.b - dif;

            float h = hsbColor.h * 360f;

            if (h < 60f)
            {
                r = max;
                g = h * dif / 60f + min;
                b = min;
            }
            else if (h < 120f)
            {
                r = -(h - 120f) * dif / 60f + min;
                g = max;
                b = min;
            }
            else if (h < 180f)
            {
                r = min;
                g = max;
                b = (h - 120f) * dif / 60f + min;
            }
            else if (h < 240f)
            {
                r = min;
                g = -(h - 240f) * dif / 60f + min;
                b = max;
            }
            else if (h < 300f)
            {
                r = (h - 240f) * dif / 60f + min;
                g = min;
                b = max;
            }
            else if (h <= 360f)
            {
                r = max;
                g = min;
                b = -(h - 360f) * dif / 60 + min;
            }
            else
            {
                r = 0;
                g = 0;
                b = 0;
            }
        }

        return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), hsbColor.a);
    }

    public Color ToColor()
    {
        return ToColor(this);
    }

    public override string ToString()
    {
        return "H:" + h + " S:" + s + " B:" + b;
    }

    public static HSBColor Lerp(HSBColor a, HSBColor b, float t)
    {
        float h, s;

        //check special case black (color.b==0): interpolate neither hue nor saturation!
        //check special case grey (color.s==0): don't interpolate hue!
        if (a.b == 0)
        {
            h = b.h;
            s = b.s;
        }
        else if (b.b == 0)
        {
            h = a.h;
            s = a.s;
        }
        else {
            if (a.s == 0)
            {
                h = b.h;
            }
            else if (b.s == 0)
            {
                h = a.h;
            }
            else {
                // works around bug with LerpAngle
                float angle = Mathf.LerpAngle(a.h * 360f, b.h * 360f, t);
                while (angle < 0f)
                    angle += 360f;
                while (angle > 360f)
                    angle -= 360f;
                h = angle / 360f;
            }
            s = Mathf.Lerp(a.s, b.s, t);
        }
        return new HSBColor(h, s, Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));
    }

    public static void Test()
    {
        HSBColor color;

        color = new HSBColor(Color.red);
        Debug.Log("red: " + color);

        color = new HSBColor(Color.green);
        Debug.Log("green: " + color);

        color = new HSBColor(Color.blue);
        Debug.Log("blue: " + color);

        color = new HSBColor(Color.grey);
        Debug.Log("grey: " + color);

        color = new HSBColor(Color.white);
        Debug.Log("white: " + color);

        color = new HSBColor(new Color(0.4f, 1f, 0.84f, 1f));
        Debug.Log("0.4, 1f, 0.84: " + color);

        Debug.Log("164,82,84   .... 0.643137f, 0.321568f, 0.329411f  :" + ToColor(new HSBColor(new Color(0.643137f, 0.321568f, 0.329411f))));
    }
}

public struct TimeStamped<T> : IEquatable<T> where T : struct
{
    public TimeStamped(T obj)
    {
        _value = obj;
        lastTimeUpdated = Time.time;
    }

    public float timeSinceLastUpdate { get { return Time.time - lastTimeUpdated; } }
    public T value { get { return _value; } set { _value = value; lastTimeUpdated = Time.time; } }
    private T _value;
    private float lastTimeUpdated;

    // Assign from T to Timestamped variable
    public static implicit operator TimeStamped<T>(T obj)
    {
        return new TimeStamped<T>(obj);
    }

    // Assign from timestamped variable to T
    public static implicit operator T(TimeStamped<T> obj)
    {
        return obj.value;
    }
    public bool Equals(T other)
    {
        return other.Equals(value);
    }
}

public class StructEventArgs<T> : EventArgs, IEquatable<T> where T : struct
{
    T value;
    public StructEventArgs(T value)
    {
        this.value = value;
    }

    public static implicit operator StructEventArgs<T>(T obj)
    {
        return new StructEventArgs<T>(obj);
    }

    // Assign from timestamped variable to T
    public static implicit operator T(StructEventArgs<T> obj)
    {
        return obj.value;
    }
    public bool Equals(T other)
    {
        return other.Equals(value);
    }
}

public class EventArgs<T> : EventArgs, IEquatable<T>
{
    public T value;
    public EventArgs(T value)
    {
        this.value = value;
    }
    public static implicit operator EventArgs<T>(T obj)
    {
        return new EventArgs<T>(obj);
    }

    // Assign from timestamped variable to T
    public static implicit operator T(EventArgs<T> obj)
    {
        return obj.value;
    }
    public bool Equals(T other)
    {
        return other.Equals(value);
    }
}


public class AnimatorHelper_MB : MonoBehaviour
{
    public EventHandler<StructEventArgs<int>> onAnimatorIK;

    void OnAnimatorIK(int layer)
    {
        if (onAnimatorIK != null)
            onAnimatorIK(this, new StructEventArgs<int>(layer));
    }
}

public class TimeQueue<T>
{
    private Dictionary<T, float> countDown;

    public EventHandler<EventArgs<T>> onObjectFinished;

    public TimeQueue()
    {
        The_Helper_MB.onUpdate += Update;
        countDown = new Dictionary<T, float>();
    }

    ~TimeQueue()
    {
        The_Helper_MB.onUpdate -= Update;
    }

    public void AddOrUpdate(T t, float time)
    {
        countDown.TryAdd(t, time);
        countDown[t] = Mathf.Max(countDown[t], time);
    }

    public void Remove(T t)
    {
        countDown[t] = 0;
    }

    public float GetTimeLeft(T t)
    {
        if (!countDown.ContainsKey(t)) return 0;
        return countDown[t];
    }

    private void Update(object sender, EventArgs e)
    {
        List<T> keys = new List<T>(countDown.Keys);

        foreach (T key in keys)
        {
            countDown[key] = Mathf.Max(0, countDown[key] - Time.deltaTime);
            if (countDown[key] == 0)
            {
                countDown.Remove(key);
                if (onObjectFinished != null)
                    onObjectFinished(this, new EventArgs<T>(key));
            }
        }
    }
}