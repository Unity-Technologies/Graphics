# The High Definition Render Pipeline Config package

The High Definition Render Pipeline (HDRP) uses a separate [package](https://docs.unity3d.com/Manual/Packages.html) to control the availability of some of its features.

For example, you can use it to:

* Enable [ray tracing](Ray-Tracing-Getting-Started.html).
* Enable [camera-relative rendering](Camera-Relative-Rendering.html).
* Control the shadow filtering mode for deferred rendering.

## Using the HDRP Config package

To use the HDRP Config package in your HDRP Project, you need to create a local copy of it and make your Project's package manifest reference it. To do this, in the HDRP Wizard (**Windows > Render Pipeline > HD Render Pipeline Wizard**) we use the **Install Configuration Editable Package**. After pressing the button you should find the **LocalPackage** at the root of your project containing the HDRP config package.  
One important node on **upgrade**: When upgrading the HDRP package, the local package will not be automatically updated and should be upgraded by hand. The recommended way of doing so would be to save a copy of your current local package with the modification, re-install it using the wizard and then re-apply your changes.


## Configuring HDRP using the config package

You can now use the local config package to configure HDRP features. You can edit the **ShaderConfig.cs** file to set which features are available in your HDRP Project. If you edit this file, you must also update the equivalent **ShaderConfig.cs.hlsl** header file (which HDRP Shaders use) so that it mirrors the definitions that you set in **ShaderConfig.cs**. You can update the **ShaderConfig.cs.hlsl** file in two ways. You can either make Unity generate the **ShaderConfig.cs.hlsl** file from the **ShaderConfig.cs** file, which makes sure that the two files are synchronized, or edit the **ShaderConfig.cs.hlsl** file directly, which is faster but it is up to you to synchronize the files when you make changes.

To ensure that the two files are synchronized, you should follow the first method. To do this:

1. Go to **LocalPackages > com.unity.render-pipelines.high-definition-config > Runtime** and open **ShaderConfig.cs**.
2. Edit the values of the properties that you want to change and then save the file.
3. Back in Unity, select **Edit > RenderPipeline > Generate Include Files**.
4. Unity automatically configures your Project and Shaders to use the new configuration.

<a name="Example"></a>
### Example

You can use the method described above to change the shadow filtering mode for the [Lit Shader](Lit-Shader.html) in deferred mode:

1. In the **ShaderConfig.cs** file, set **DeferredShadowFiltering** to **HDShadowFilteringQuality.High**.
2. Generate the **ShaderConfig.cs.hlsl** file (**Edit > RenderPipeline > Generate Include Files**). Now, in the **ShaderConfig.cs.hlsl** file, the **SHADEROPTIONS_DEFERRED_SHADOW_FILTERING** define should be set to **2** (**#define SHADEROPTIONS_DEFERRED_SHADOW_FILTERING (2)**).