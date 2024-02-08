# Create an HDRP Asset

Before you can use HDRP, you need an HDRP Asset, which controls the global rendering settings and creates an instance of the High Definition Render Pipeline. 

The **High-Definition RP** Template creates an HDRP Asset for you, but you can create different HDRP Assets to suit your rendering needs, such as one HDRP Asset for every target platform. An HDRP Asset allows you to enable features for your entire Project in the Editor. It allocates memory for the different features, so you can't edit them at runtime.

A new Project using the HDRP template includes an HDRP Asset file named HDRenderPipelineAsset in the Assets/Settings folder.

If you upgrade a project from a different render pipeline to HDRP and therefore do not use the HDRP template, you need to add an HDRP Asset to your Project. To create and customize an HDRP Asset:

1. In the Unity Editor, go to the Project window and navigate to the folder you want to create your HDRP Asset in. This folder must be inside the **Assets** folder; you can not create Assets in the **Packages** folder.
2. In the main menu, go to **Assets > Create > Rendering** and click **HDRP Asset**.
3. Enter a name for the **HDRP Asset** and press the Return key to confirm it.

When you have created an HDRP Asset, you must assign it it to the pipeline:

1. Navigate to **Edit > Project Settings > Graphics** and locate the **Scriptable Render Pipeline Settings** property at the top.
2. Either drag and drop the HDRP Asset into the property field, or use the object picker (located on the right of the field) to select it from a list of all HDRP Assets in your Project.

Unity now uses the High Definition Render Pipeline (HDRP) in your Unity Project. HDRP does not support gamma space, so your Project must use linear color space, To do this:

1. Navigate to **Edit > Project Settings > Player > Other Settings** and locate the **Color Space** property.
2. Select **Linear** from the **Color Space** drop-down.

You can create multiple HDRP Assets containing different settings. This is useful for Project that support multiple platforms, such as PC, Xbox One and PlayStation 4. In each HDRP Asset, you can change settings to suite the hardware of each platform and then assign the relevant one when building your Project for each platform. For more information on using creating HDRP Assets to target different platforms, see [Quality settings](quality-settings.md).

To change which HDRP Asset your render pipeline uses, either manually select an HDRP Asset in the active Quality Level of the Quality Settings window (as shown above), or use the QualitySettings.renderPipeline property via script.

To change which HDRP Asset your render pipeline uses by default, either manually select an HDRP Asset in the [Graphics settings window](Default-Settings-Window.md), or use the GraphicsSettings.renderPipelineAsset property via script.

When you create an HDRP Asset, open it in the Inspector to edit its properties.