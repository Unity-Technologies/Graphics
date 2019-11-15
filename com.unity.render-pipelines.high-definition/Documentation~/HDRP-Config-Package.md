# The High Definition Render Pipeline Config package

The High Definition Render Pipeline (HDRP) uses a separate [package](https://docs.unity3d.com/Manual/Packages.html) to control the availability of some of its features.

For example, you can use it to:

* Enable [ray tracing](Ray-Tracing-Getting-Started.html).
* Enable [camera-relative rendering](Camera-Relative-Rendering.html).
* Control the shadow filtering mode for deferred rendering.

## Using the HDRP Config package

To use the HDRP Config package in your HDRP Project, you need to create a local copy of it and make your Project's package manifest reference it. To do this:

1. In the **Project** window, create a new folder, inside the **Assets** folder, called **LocalPackages**.

2. Download the Config package from GitHub. To do this:

	1. Go to https://github.com/Unity-Technologies/ScriptableRenderPipeline/tree/master
	2. Click on the **Branch** drop-down and select Release/ the Unity version that you are using. For example, if you are using Unity 2019.3, select **Release/2019.3**.
	3. Select **Clone or download > Download ZIP**.

4. When the download finishes, open the zipped folder and find the **com.unity.render-pipelines.high-definition-config** folder. Move this into the **LocalPackages** folder that you created in step 1.

5. In the **Project** window, right-click the Packages folder and select **Show in Explorer**. Go to **Packages** and open the **manifest.json**.

6. Change the target for **"com.unity.render-pipelines.high-definition-config"** : from the version number to **"file:../LocalPackages/com.unity.render-pipelines.high-definition-config"**.

 

## Configuring HDRP using the config package

You can now use the local config package to configure HDRP features. You can edit the **ShaderConfig.cs** file to set which features are available in your HDRP Project. If you edit this file, you must also update the equivalent **ShaderConfig.cs.hlsl** header file (which HDRP Shaders use) so that it mirrors the definitions that you set in **ShaderConfig.cs**. You can update the **ShaderConfig.cs.hlsl** file in two ways. You can either make Unity generate the **ShaderConfig.cs.hlsl** file from the **ShaderConfig.cs** file, which makes sure that the two files are synchronized, or edit the **ShaderConfig.cs.hlsl** file directly, which is faster but it is up to you to synchronize the files when you make changes.

To ensure that the two files are synchronized, you should follow the first method. To do this:

1. Go to **LocalPackages > com.unity.render-pipelines.high-definition-config > Runtime** and open **ShaderConfig.cs**.
2. Edit the values of the properties that you want to change and then save the file.
3. Back in Unity, select **Edit > RenderPipeline > Generate Include Files**.
4. Unity automatically configures your Project and Shaders to use the new configuration.

### Example

You can use the method described above to change the shadow filtering mode for the [Lit Shader](Lit-Shader.html) in deferred mode. In the **ShaderConfig.cs** file, set **DeferredShadowFiltering** to **HDShadowFilteringQuality.High**. Now, when you generate the ShaderConfig.cs.hlsl file, it should have **#define SHADEROPTIONS_DEFERRED_SHADOW_FILTERING (2)**.