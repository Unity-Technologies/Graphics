# Virtual Reality in the High Definition Render Pipeline

To use Virtual Reality (VR) in the High Definition Render Pipeline (HDRP), you must enable VR in your Unity Project. To do this, see the [VR tab](Render-Pipeline-Wizard.md#VRTab) in the Render Pipeline Wizard.

Please refer to [Unity XR](https://docs.unity3d.com/Manual/XR.html) documentation for more information about XR developement with Unity.

## Recommended Settings

Unity has designed HDRP to fully support Single-Pass Instanced mode. This mode gives you the best performance on all platforms.
HDRP also supports multi-pass but this is slower on the CPU and some features, like Auto Exposure, can cause issues.
If you encounter a problem with a specific feature, you can disable it in your Project’s [HDRP Asset](HDRP-Asset.md).

You can also watch the presentation from Unite Copenhagen (October 2019) to learn more tips: [Maximizing visual fidelity in VR: HDRP support](https://youtu.be/_WkSAn55EBM)

## Supported Platforms and Devices

* **PC with DX11**:
  * Oculus Rift & Rift S (Oculus XR Plugin, Windows 10, DirectX 11)
  * Windows Mixed Reality (Windows XR Plugin, Windows 10, DirectX 11)
* **PS4**:
  * PlayStationVR
  * Open VR*

**Note**: Valve is currently developing their OpenVR Unity XR plugin for 2019.3 and beyond.
For more information, see [Unity XR platform updates](https://blogs.unity3d.com/2020/01/24/unity-xr-platform-updates/) on the Unity blog, and [XR Plugin Architecture](https://docs.unity3d.com/Manual/XRPluginArchitecture.html) in the Unity Manual.
The XR Plugin architecture links to the OpenVR desktop package and has further info and recommendations.

## Resolution Control
There are multiple methods that you can use to control the resolution of your render targets in HDRP, but be aware that HDRP doesn't support every method available in standard Unity using the built-in render pipeline. [XRSettings.renderViewportScale](https://docs.unity3d.com/ScriptReference/XR.XRSettings-renderViewportScale.html) has no effect in HDRP and generates a warning if you use it. Use one of the following methods instead:
* **Dynamic Resolution**: You can use the [dynamic resolution system](Dynamic-Resolution.md) to change the resolution at runtime. This is the best method to use if you want to change the resolution at runtime.
* **Eye Texture**: You can set the device back-buffer resolution by changing [XRSettings.eyeTextureResolutionScale](https://docs.unity3d.com/ScriptReference/XR.XRSettings-eyeTextureResolutionScale.html). This is a resource intensive operation that reallocates all render targets.

Be aware that SteamVR will apply a default 150% supersampling value. You can change this value in the settings of SteamVR.

## Enable VR single-pass after startup

**Note**: Due to some technical limitations that Unity will resolve in later versions of HDRP, you need the following code in your script if your app doesn't boot directly in VR mode:

```csharp
private void Awake()
{
    UnityEngine.Rendering.TextureXR.maxViews = 2;
}
```

## C# defines

You can use the following defines to include or exclude code from your scripts.

* ENABLE_VR: The C++ side of the engine sets this define to specify if the platform supports VR.
* ENABLE_VR_MODULE: Unity sets this define if your Project includes the [built-in VR module com.unity.modules.vr](https://docs.unity3d.com/Manual/upm-ui-disable.html).
* ENABLE_XR_MODULE: Unity sets this define if your Project includes the [built-in XR module com.unity.modules.xr](https://docs.unity3d.com/Manual/upm-ui-disable.html).
