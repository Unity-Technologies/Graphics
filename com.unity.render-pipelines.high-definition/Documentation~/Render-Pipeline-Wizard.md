# High Definition Render Pipeline Wizard

The High Definition Render Pipeline (HDRP) includes the **HD Render Pipeline Wizard** to help you configure your Unity Project so that it is compatible with HDRP. 

To open the **Render Pipeline Wizard**, go to **Window > Render Pipeline** and select **HD Render Pipeline Wizard**.

![](Images/RenderPipelineWizard1.png)

## Default Path Settings

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Default Resources Folder** | Set the folder name that the Render Pipeline Wizard uses when it loads or creates resources. Click the **Populate / Reset** button to populate the **Default Resources Folder** with the resources that HDRP needs to render a Scene (for details, see [Populating the default resources folder](#PopulatingFolder)). If a default Asset already exists in the folder then clicking the Populate/Reset button resets the existing Asset. |
| **Default Scene Prefab**     | Set the default Prefab that Unity instantiates in a new Scene when you select **File > New Scene**. To instantly create a Scene Asset with this template, go to **Assets > Create** and click **HD Template Scene**. |
| **Default DXR Scene Prefab** | Set the default Prefab that Unity instantiates in a new Scene that uses ray tracing. |

### Populating the default resources folder

When you click **Populate/Reset**, HDRP generates the following Assets:

- **DefaultSceneRoot**: The Prefab that Unity instantiates in each new HDRP template Scene.
- **DefautRenderingSettings**: The default [Volume Profile](Volume-Profile.html) that the template Scene uses to render visual elements like shadows, fog, and the sky.
- **DefautPostprocessingSettings**: The default [Volume Profile](Volume-Profile.html) that the template Scene uses for post-processing effects.
- **HDRenderPipellineAsset**: The [HDRP Asset](HDRP-Asset.html) that Unity uses to configure HDRP settings for the Unity Project.
- **Foliage Diffusion Profile**: A [Diffusion Profile](Diffusion-Profile.html) that simulates sub-surface light interaction with foliage.
- **Skin Diffusion Profile**: A [Diffusion Profile](Diffusion-Profile.html) that simulates sub-surface light interaction with skin.

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
| **Shadowmask Mode**              | Checks to make sure **Shadowmask Mode** is set to **Distance Shadowmask** at the Project level. This allows you to change the **Shadowmask Mode** on a per-[Light](Light-Component.html) level. <br />Press the **Fix** button to set the **Shadowmask Mode** to **Distance Shadowmask**. |
| **Asset Configuration**          | Checks every configuration in this section. <br />Press the **Fix All** button to fix every configuration in this section. |
| **- Assigned**                   | Checks to make sure you have assigned an [HDRP Asset](HDRP-Asset.html) to the **Scriptable Render Pipeline Settings** field (menu: **Edit** > **Project Settings** > **Graphics**).<br />Press the **Fix** button to open a pop-up that allows you to either assign an HDRP Asset or create and assign a new one. |
| **- Runtime Resources**          | Checks to make sure that your HDRP Asset references a [**Render Pipeline Resources**](HDRP-Asset.html#GeneralProperties) Asset.<br />Press the **Fix** button to reload the runtime resources for the HDRP Asset. |
| **- Editor Resources**           | Checks to make sure that your HDRP Asset references a [**Render Pipeline Editor Resources**](HDRP-Asset.html#GeneralProperties)  Asset.<br />Press the **Fix** button to reload the runtime resources for the HDRP Asset. |
| **- Diffusion Profile**          | Checks to make sure that your HDRP Asset references a [**Diffusion Profile**](Diffusion-Profile.html) Asset.<br />Press the **Fix** button to reload the runtime resources for the HDRP Asset. |
| **Default Scene Prefab**         | Checks to make sure you have assigned something to **Default Scene Prefab** in this wizard.<br />Press the **Fix** button to open a pop-up that allows you to either assign a Prefab or create and assign a new one. |

<a name="VRTab"></a>

### HDRP + VR

This tab provides all of the configuration options from the [HDRP tab](#HDRPTab) as well as extra configuration options to help you set your HDRP Project up to support virtual reality. If you can not find an option in this section of the documentation, check the [HDRP tab](#HDRPTab) above.

| **Configuration Option**     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **VR Activated**                 | Checks to make sure **Virtual Reality Supported** is enabled. To use VR in Unity, you must enable this feature. <br />Press the **Fix** button to enable **Virtual Reality Supported** and make Unity support VR. |

<a name="DXRTab"></a>
### HDRP + DXR

This tab provides all of the configuration options from the [HDRP tab](#HDRPTab) as well as extra configuration options to help you set your HDRP Project up to support ray tracing. If you can not find an option in this section of the documentation, check the [HDRP tab](#HDRPTab) above.

| **Configuration Option**          | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Auto Graphics API**            | Checks to make sure **Auto Graphics API** is disabled in your Player Settings for the current platform. DXR needs to use **Direct3D 12**. <br />Press the **Fix** button to disable **Auto Graphics API**. |
| **Direct3D 12**                  | Checks to make sure **Direct3D 12** is the first Graphic API set in Player Settings for the current plateform. <br />Press the **Fix** button to make Unity use **Direct3D 12**. |
| **Scripting Symbols**            | Checks to make sure **Scripting Symbols** in Player Settings contains **REALTIME_RAYTRACING_SUPPORT** for your current plateform. <br />Press the **Fix** button to add **REALTIME_RAYTRACING_SUPPORT** to **Scripting Symbols**. |
| **Screen Space Shadow**          | Checks to make sure **Screen Space Shadow** is enabled in the current [HDRP Asset](HDRP-Asset.html). <br />Press the **Fix** button to enable **Screen Space Shadow**. |
| **DXR Activated**                | Checks to make sure **DXR Activated** is enabled in the current [HDRP Asset](HDRP-Asset.html). <br />Press the **Fix** button to enable **DXR Activated**. |
| **DXR Resources**               | Checks to make sure that your HDRP Asset references an **HD Render Pipeline RayTracing Resources**  Asset. <br />Press the **Fix** button to reload the raytracing resources for the HDRP Asset. |
| **DXR Shader Config** | Checks to make sure that the **ShaderConfig.cs.hlsl**, in the **High Definition RP Config** package referenced in your Project, has **SHADEROPTIONS_RAYTRACING** set to **1**. <br />Press the **Fix** button to create a local copy of the **High Definition RP Config** package and, in the **ShaderConfig.cs.hlsl**, set **SHADEROPTIONS_RAYTRACING** to **1**. |
| **Default DXR Scene Prefab** | Checks to make sure you have assigned something to **Default DXR Scene Prefab** in this wizard.<br />Press the **Fix** button to open a pop-up that allows you to either assign a Prefab or create and assign a new one. |

## Project Migration Quick-links

When upgrading a project from the built-in render pipeline to HDRP, you need to do upgrade your Lights and your Materials. Use the  following utility functions to help with the upgrade process:

- **Upgrade Project Materials to High Definition Materials**: Upgrades every Material in your Unity Project to HDRP Materials.
- **Upgrade Selected Materials to High Definition Materials**: Upgrades every Material currently selected to HDRP Materials.
- **Upgrade Unity Builtin Scene Light Intensity for High Definition**: Upgrades each Light in the current Scene to HDRP compatible intensity values.

