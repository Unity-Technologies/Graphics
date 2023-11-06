# Convert project with HDRP wizard

Firstly, to install HDRP, add the High Definition RP package to your Unity Project:

1. Open your Unity project.
2. To open the **Package Manager** window, go to **Window** > **Package Manager**.
3. In the **Package Manager** window, in the **Packages:** field, select **Unity Registry** from the menu.
4. Select **High Definition RP** from the list of packages.
5. In the bottom right corner of the Package Manager window, select **Install**.

**Note**: When you install HDRP, Unity automatically attaches two HDRP-specific components to GameObjects in your Scene. It attaches the **HD Additional Light Data** component to Lights, and the **HD Additional Camera Data** component to Cameras. If you don't set your Project to use HDRP, and any HDRP component is present in your Scene, Unity throws errors. To fix these errors, see the following instructions on how to set up HDRP in your Project.

To set up HDRP in your project, use the [HDRP Wizard](Render-Pipeline-Wizard.md).

1. To open the **HD Render Pipeline Wizard** window, go to **Window** > **Rendering** > **HD Render Pipeline Wizard**.
2. In the **Configuration Checking** section, go to the **HDRP** tab and click **Fix All**. This fixes every HDRP configuration issue with your Project.

You have fixed your Project's HDRP configuration issues, but your Scene doesn't render correctly because GameObjects in the Scene still use Shaders made for the Built-in Render Pipeline. To find out how to upgrade Built-in Shaders to HDRP Shaders, see [Convert materials and shaders](convert-from-built-in-convert-materials-and-shaders.md).
