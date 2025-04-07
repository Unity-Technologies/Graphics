# Set up ray tracing

## Integrate ray tracing into your HDRP Project

Before you use ray tracing features in your HDRP Project, you need to set up your HDRP Project for ray tracing support. HDRP only supports ray tracing using the DirectX 12 API, so ray tracing only works in the Unity Editor or the Windows Unity Player when they render with DirectX 12. You need to change the default graphics API of your HDRP project from DirectX 11 to DirectX 12.

There are two ways to do this:

* [Use the Render Pipeline Wizard](#WizardSetup)

* [Manual setup](#ManualSetup)

Once you have completed one of these, move onto [Final setup](#final-setup).

<a name="WizardSetup"></a>

### Render Pipeline Wizard setup

You can use the [Render Pipeline Wizard](Render-Pipeline-Wizard.md) to set up ray tracing in your HDRP Project.

1. To open the HDRP Wizard, go to **Window** > **Rendering** > **HDRP Wizard**.

2. Select the **HDRP + DXR** tab.

3. Click the **Fix All** button.

To enable ray tracing for specific effects, enable the ray tracing features in the [HDRP Asset](#ManualSetup-EnableAssetFeatures).

For information on how to set up ray tracing for your Scene, see [final setup](#final-setup).

<a name="ManualSetup"></a>

### Manual setup

To set up ray tracing manually, you need to:

1. [Make your HDRP project use DirectX 12](#ManualSetup-EnablingDX12).
2. [Disable static batching on your HDRP project](#ManualSetup-DisablingStaticBatching).
3. [Enable and configure ray tracing in your HDRP Asset](#ManualSetup-EnablingRayTracing).
4. [Ensure ray tracing resources are properly assigned](#ManualSetup-RayTracingResources).
5. (Optional) [Enable ray-traced effects in your HDRP Asset](#ManualSetup-EnableAssetFeatures).

<a name="ManualSetup-EnablingDX12"></a>

#### Upgrade to DirectX 12

HDRP enables DirextX12 by default. To enable DirectX 12 manually:

1. Open the Project Settings window (menu: **Edit** > **Project Settings**), then select the **Player** tab.
2. Select the **Other Settings** drop-down, and in the **Rendering** section, disable Auto Graphics API for Windows. This exposes the Graphics APIs for Windows section.
3. In the **Graphics APIs for Windows** section, click the plus (**+**) button and select **Direct3d12**.
4. Unity uses Direct3d11 by default. To make Unity use Direct3d12, move **Direct3d12 (Experimental)** to the top of the list.
5. To apply the changes, you may need to restart the Unity Editor. If a window prompt appears telling you to restart the Editor, click **Restart Editor** in the window.

The Unity Editor window should now include the &lt;DX12&gt; tag in the title bar:

![](Images/RayTracingGettingStarted1.png)

<a name="ManualSetup-DisablingStaticBatching"></a>

#### Disable static batching

Next, you need to disable static batching, because HDRP doesn't support this feature with ray tracing in **Play Mode**. To do this:

1. Open the Project Settings window (menu:  **Edit** > **Project Settings**), then select the **Player** tab.
2. Select the **Other Settings** drop-down, then in the **Rendering** section, disable **Static Batching**.

<a name="ManualSetup-EnablingRayTracing"></a>

#### Enable ray tracing in the HDRP Asset 

Now that Unity is running in DirectX 12, and you have disabled [static batching](https://docs.unity3d.com/Manual/DrawCallBatching.html), enable and configure ray tracing in your [HDRP Asset](HDRP-Asset.md). The previous steps configured Unity to support ray tracing; the following step enables it in your HDRP Unity Project.

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the **Rendering** section, enable **Realtime Ray Tracing**. This triggers a recompilation, which makes ray tracing available in your HDRP Project.

<a name="ManualSetup-RayTracingResources"></a>

#### Verify ray tracing resources

To verify that HDRP has assigned ray tracing resources:

1. Open the Project Settings window (menu: **Edit** > **Project Settings**), then select the **HDRP Default Settings** tab.
2. Find the **Render Pipeline Resources** field and make sure there is a Render Pipeline Resources Asset assigned to it.

<a name="ManualSetup-EnableAssetFeatures"></a>

#### Enable ray-traced effects in the HDRP Asset (Optional)

HDRP uses ray tracing to replace certain rasterized effects. To use a ray tracing effect in your Project, you must first enable the rasterized version of the effect. The four effects that require you to modify your HDRP Asset  are:

* **Screen Space Shadows**
* **Screen Space Reflections**
*  **Transparent Screen Space Reflections**
* **Screen Space Global Illumination**

To enable the above effects in your HDRP Unity Project:

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. Go to **Lighting** > **Reflections** and enable **Screen Space Reflection**.
3. After enabling **Screen Space Reflections**, go to **Lighting** > **Reflections** and enable **Transparent Screen Space Reflection**.
4. Go to **Lighting** > **Shadows** and enable **Screen Space Shadows**.
5. Go to **Lighting** > **Lighting** and enable **Screen Space Global Illumination**.

Your HDRP Project now fully supports ray tracing. For information on how to set up ray tracing for your Scene, see [final setup](#final-setup).

### Final setup

Now that your HDRP Project supports ray tracing, there are steps you must complete to use it in your Scene.

1. [Frame Settings validation](#frame-settings)
2. [Build settings validation](#build-settings)
3. [Scene validation](#scene-validation)


#### Frame Settings

To make HDRP calculate ray tracing effects for [Cameras](hdrp-camera-component-reference.md) in your Scene, make sure your Cameras use [Frame Settings](Frame-Settings.md) that have ray tracing enabled. You can enable ray tracing for all Cameras by default, or you can enable ray tracing for specific Cameras in your Scene.

To enable ray tracing by default:

1. From the main menu, select **Edit** &gt; **Project Settings**.
2. In the **Project Settings** window, go to the **Pipeline Specific Settings** section, then select the **HDRP** tab.
3. Under **Frame Settings (Default Values)** &gt; **Camera** &gt; **Rendering**, enable **Ray Tracing**.

To enable ray tracing for a specific camera:

1. Select the camera in the scene or **Hierarchy** window to view it in the **Inspector** window.
2. In the **Rendering** section, enable **Custom Frame Settings**. This exposes frame settings for this camera only.
3. Use the foldout (triangle) to expand **Rendering**, then enable **Ray Tracing**.

#### Build settings

To build your Project to a Unity Player, ray tracing requires that the build uses 64 bits architecture. To set your build to use 64 bits architecture:

1. Open the Build Settings window (menu: **File > Build Settings**).
2. From the **Architecture** drop-down, select **x86_64**.

#### Scene validation

To check whether it's possible to use ray tracing in a Scene, HDRP includes a menu option that validates each GameObject in the Scene. If you don't setup GameObjects correctly, this process throws warnings in the Console window. For the list of things this option checks for, see [Menu items](Menu-Items.md#other). To use it:
1. Click **Edit** > **Render Pipeline** > **HD Render Pipeline**  > **Check Scene Content for Ray Tracing**.
2. In the Console window, check if there are any warnings.
