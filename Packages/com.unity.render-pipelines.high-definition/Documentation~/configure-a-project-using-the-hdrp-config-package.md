# The High Definition Render Pipeline Config package

The High Definition Render Pipeline (HDRP) uses a separate [package](https://docs.unity3d.com/Manual/Packages.html) to control the availability of some of its features.

For example, you can use it to:

* Disable Area Light.
* Disable Pre-exposition.
* Enable [camera-relative rendering](Camera-Relative-Rendering.md).
* Increase the size of the tile and cluster light list for rasterization.
* Increase the size of [the Path Tracing light list](Ray-Tracing-Path-Tracing.md).

## Using the HDRP Config package

To use the HDRP Config package in your HDRP Project, you need to create a local copy of it and make your Project's package manifest reference it. You can either do this manually or use the [HDRP Wizard](Render-Pipeline-Wizard.md).

* **Manual**: In your Project's directory, move and rename the folder "**/Library/PackageCache/com.unity.render-pipelines.high-definition-config@[versionnumber]**" to "**/Packages/com.unity.render-pipelines.high-definition-config**".
* **HDRP Wizard**: Open the HDRP Wizard (**Windows > Rendering > HDRP Wizard**) and click the **Install Configuration Editable Package**. This creates a **LocalPackage** folder at the root of your Project and populates it with a compatible HDRP config package.

**Note**: Using the Package Manager to upgrade your HDRP package does not automatically upgrade the local config package. To manually upgrade the local config package:

1. Make a copy of your current config package.
2. Use the HDRP Wizard to create a new, compatible config package.
3. Apply the settings from the old config package to the new config package.


## Configuring HDRP using the config package

You can now use the local config package to configure HDRP features. You can edit the **ShaderConfig.cs** file to set which features are available in your HDRP Project. If you edit this file, you must also update the equivalent **ShaderConfig.cs.hlsl** header file (which HDRP Shaders use) so that it mirrors the definitions that you set in **ShaderConfig.cs**. You can update the **ShaderConfig.cs.hlsl** file in two ways. You can either make Unity generate the **ShaderConfig.cs.hlsl** file from the **ShaderConfig.cs** file, which makes sure that the two files are synchronized, or edit the **ShaderConfig.cs.hlsl** file directly, which is faster but it is up to you to synchronize the files when you make changes.

To ensure that the two files are synchronized, you should follow the first method. To do this:

1. Go to **LocalPackages > com.unity.render-pipelines.high-definition-config > Runtime** and open **ShaderConfig.cs**.
2. Edit the values of the properties that you want to change and then save the file.
3. Back in Unity, select **Edit > RenderPipeline > Generate Include Files**.
4. Unity automatically configures your Project and Shaders to use the new configuration.

<a name="Example"></a>
### Example

You can use the method described above to disable [Camera-Relative rendering](Camera-Relative-Rendering.md):

1. In the **ShaderConfig.cs** file, set **CameraRelativeRendering** to **0**.
2. Generate the **ShaderConfig.cs.hlsl** file (**Edit > RenderPipeline > Generate Include Files**). Now, in the **ShaderConfig.cs.hlsl** file, the **SHADEROPTIONS_CAMERA_RELATIVE_RENDERING** define should be set to **0**
