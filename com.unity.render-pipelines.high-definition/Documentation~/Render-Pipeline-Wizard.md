# High Definition Render Pipeline Wizard

The High Definition Render Pipeline (HDRP) includes the **HD Render Pipeline Wizard** to help you configure your Unity Project so that it is compatible with HDRP. 

To open the **Render Pipeline Wizard**, go to **Window > Render Pipeline** and select **HD Render Pipeline Wizard**.

![](Images/RenderPipelineWizard1.png)

## Packages

At the top of the window, there is an information box that shows you the currently installed version of HDRP, as well as the latest version of HDRP that is compatible with your current Unity version.

You also have a button allow you to creates a local instance of the [High Definition Render Pipeline Config package](HDRP-Config-Package.md) in the **LocalPackage** folder of your HDRP Project. If already installed, some information about its location are displayed below.

## Default Path Settings

| **Property**                               | **Description**                                              |
| ------------------------------------------ | ------------------------------------------------------------ |
| **Default Resources Folder**               | Set the folder name that the Render Pipeline Wizard uses when it loads or creates resources. Click the **Populate / Reset** button to populate the **Default Resources Folder** with the resources that HDRP needs to render a Scene (for details, see [Populating the default resources folder](#populating-the-default-resources-folder)). If a default Asset already exists in the folder then clicking the Populate/Reset button resets the existing Asset. |
| **Install Configuration Editable Package** | Creates a local instance of the [High Definition Render Pipeline Config package](HDRP-Config-Package.md) in the **LocalPackage** folder of your HDRP Project. |

### Populating the default resources folder

When you click **Populate/Reset**, HDRP generates the following Assets:

- **DefautRenderingSettings**: The default [Volume Profile](Volume-Profile.md) that the template Scene uses to render visual elements like shadows, fog, and the sky.
- **DefautPostprocessingSettings**: The default [Volume Profile](Volume-Profile.md) that the template Scene uses for post-processing effects.
- **HDRenderPipellineAsset**: The [HDRP Asset](HDRP-Asset.md) that Unity uses to configure HDRP settings for the Unity Project.
- **Foliage Diffusion Profile**: A [Diffusion Profile](Diffusion-Profile.md) that simulates sub-surface light interaction with foliage.
- **Skin Diffusion Profile**: A [Diffusion Profile](Diffusion-Profile.md) that simulates sub-surface light interaction with skin.

## Configuration Checking

Your Unity Project must adhere to all the configuration tests in this section for HDRP to work correctly. If a test fails, a message explains the issue and you can click a button to fix it. This helps you quickly fix any major issues with your HDRP Project. The Render Pipeline Wizard can load or create any resources that are missing by placing new resources in the **Default Resources Folder**.

There are three tabs that you can use to set up your HDRP Project for different use cases.
* [HDRP](#HDRPTab): Use this tab to set up a default HDRP Project.
* [HDRP + VR](#VRTab): Use this tab to set up your HDRP Project and enable support for virtual reality.
* [HDRP + DXR](#DXRTab): Use this tab to set up your HDRP Project and enable support for ray tracing. 

<a name="HDRPTab"></a>

### HDRP

This tab provides you with configuration options to help you make your Unity Project use HDRP.

| **Configuration Option**           | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Color Space**                  | Checks to make sure **Color Space** is set to **Linear**. HDRP only supports **Linear Color Space** because it gives more physically accurate results than **Gamma**.<br />Press the **Fix** button to set the **Color Space** to **Linear**. |
| **Lightmap Encoding**            | Checks to make sure **Lightmap Encoding** is set to **High Quality**, which is the only mode that HDRP supports. <br />Press the **Fix** button to make Unity encode lightmaps in **High Quality** mode. This fixes lightmaps for all platforms. |
| **Shadows**                      | Checks to make sure **Shadow Quality** is set to **All**. Unity hides this option when you install HDRP, and automatically sets it to **All**. <br />Press the **Fix** button to set **Shadow Quality** to **All**. |
| **Shadowmask Mode**              | Checks to make sure **Shadowmask Mode** is set to **Distance Shadowmask** at the Project level. This allows you to change the **Shadowmask Mode** on a per-[Light](Light-Component.md) level. <br />Press the **Fix** button to set the **Shadowmask Mode** to **Distance Shadowmask**. |
| **Asset Configuration**          | Checks every configuration in this section. <br />Press the **Fix All** button to fix every configuration in this section. |
| **- Assigned**                   | Checks to make sure you have assigned an [HDRP Asset](HDRP-Asset.md) to the **Scriptable Render Pipeline Settings** field (menu: **Edit** > **Project Settings** > **Graphics**).<br />Press the **Fix** button to open a pop-up that allows you to either assign an HDRP Asset or create and assign a new one. |
| **- Runtime Resources**          | Checks to make sure that your HDRP Asset references a [**Render Pipeline Resources**](HDRP-Asset.md) Asset.<br />Press the **Fix** button to reload the runtime resources for the HDRP Asset. |
| **- Editor Resources**           | Checks to make sure that your HDRP Asset references a [**Render Pipeline Editor Resources**](HDRP-Asset.md)  Asset.<br />Press the **Fix** button to reload the runtime resources for the HDRP Asset. |
| **- Diffusion Profile**          | Checks to make sure that your HDRP Asset references a [**Diffusion Profile**](Diffusion-Profile.md) Asset.<br />Press the **Fix** button to reload the runtime resources for the HDRP Asset. |
| **Default Volume Profile** | Checks to make sure you have assigned a **Default Volume Profile Asset** in **Edit** > **Project Settings** > **HDRP Default Settings** .<br />Press the **Fix** button to open a pop-up that allows you to either assign a Profile or create and assign a new one. |

<a name="VRTab"></a>

### HDRP + VR

This tab provides all of the configuration options from the [HDRP tab](#HDRPTab) as well as extra configuration options to help you set your HDRP Project up to support virtual reality. If you can not find an option in this section of the documentation, check the [HDRP tab](#HDRPTab) above. This is only supported on Windows OS.

| **Configuration Option**     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Legacy VR System**    | Checks to make sure **Virtual Reality Supported** is disabled. This was the legacy system that is being deprecated. <br />Press the **Fix** button to disable **Virtual Reality Supported**. |
| **XR Management Package** | Check to make sure the **XR Management Package** is installed.<br />Press the **Fix** button to install it. |
| **- Oculus Plugin** | This cannot be checked directly by the wizard. So it is basically info on procedure to check it.<br />You should install the plugin manually in **Edit** > **Project Settings** > **XR Plugin Manager** |
| **- Single-Pass Instancing** | This cannot be checked directly by the wizard. So it is basically info on procedure to check it.<br />You should check in **Edit** > **Project Settings** > **XR Plugin Manager** > **Oculus** that **Stereo Rendering Mode** use **Single-Pass Instancing** |
| **XR Legacy Helpers Package** | Check to make sure the **XR Legacy Helpers Package** is installed. It is require to handle inputs with the **TrackedPoseDriver** component.<br />Press the **Fix** button to install it. |

<a name="DXRTab"></a>

### HDRP + DXR

This tab provides all of the configuration options from the [HDRP tab](#HDRPTab) as well as extra configuration options to help you set your HDRP Project up to support ray tracing. If you can not find an option in this section of the documentation, check the [HDRP tab](#HDRPTab) above. This is only supported on Windows OS.

Note that every **Fix** will be deactivated if your Hardware or OS do not support DXR.

| **Configuration Option**          | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Hardware and OS** | Check that your hardware and OS are compatible with using DXR. |
| **Auto Graphics API**            | Checks to make sure **Auto Graphics API** is disabled in your Player Settings for the current platform. DXR needs to use **Direct3D 12**. <br />Press the **Fix** button to disable **Auto Graphics API**. |
| **Direct3D 12**                  | Checks to make sure **Direct3D 12** is the first Graphic API set in Player Settings for the current plateform. <br />Press the **Fix** button to make Unity use **Direct3D 12**. |
| **Static Batching** | **Static Batching** is not supported while using DXR.<br />Press the **Fix** button to deactivate it. |
| **Screen Space Shadow**          | Checks to make sure **Screen Space Shadow** is enabled in the current [HDRP Asset](HDRP-Asset.md). <br />Press the **Fix** button to enable **Screen Space Shadow**. |
| **Reflections** | Checks to make sure **Screen Space Reflection** is enabled in the current [HDRP Asset](HDRP-Asset.md). <br />Press the **Fix** button to enable **Screen Space Reflection**. |
| **DXR Activated**                | Checks to make sure **DXR Activated** is enabled in the current [HDRP Asset](HDRP-Asset.md). <br />Press the **Fix** button to enable **DXR Activated**. |
| **DXR Resources**               | Checks to make sure that your HDRP Asset references an **HD Render Pipeline RayTracing Resources**  Asset. <br />Press the **Fix** button to reload the raytracing resources for the HDRP Asset. |
| **DXR Shader Config** | Checks to make sure that the **ShaderConfig.cs.hlsl**, in the **High Definition RP Config** package referenced in your Project, has **SHADEROPTIONS_RAYTRACING** set to **1**. <br />Press the **Fix** button to create a local copy of the **High Definition RP Config** package and, in the **ShaderConfig.cs.hlsl**, set **SHADEROPTIONS_RAYTRACING** to **1**. |

## Project Migration Quick-links

When upgrading a project from the built-in render pipeline to HDRP, you need to do upgrade your Materials. Use the following utility functions to help with the upgrade process:

- **Upgrade Project Materials to High Definition Materials**: Upgrades every Material in your Unity Project to HDRP Materials.
- **Upgrade Selected Materials to High Definition Materials**: Upgrades every Material currently selected to HDRP Materials.

The lighting will not match as HDRP use a different attenuation function than built-in and use correct math to handle lighting model. There is no function that can convert the look. Thus the lighting will require to be redone.
