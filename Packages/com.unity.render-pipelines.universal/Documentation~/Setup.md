# Requirements and setup

Install the following Editor and package versions to begin working with the **2D Renderer**:

- **Unity 2021.2.0b1** or later

- **Universal Render Pipeline** version 10 or higher (available via the Package Manager)

## 2D Renderer Setup
1. Create a new Project using the [2D template](https://docs.unity3d.com/Manual/ProjectTemplates.html).![](Images/2D/New_Project_With_Template.png)


2. Create a new **Pipeline Asset** and **Renderer Asset** by going to the **Assets** menu and selecting **Create > Rendering > URP Asset (with 2D Renderer)**.
   ![](Images/2D/2d-urp12-create-renderer-asset.png)
   <br/>

3. Enter the name for both the Pipeline and Renderer Assets. The name is automatically applied to both, wi th "_Renderer" appended to the name of the Renderer Asset.
   ![](Images/2D/2d-urp12-pipeline-renderer-assets.png)
   <br/>

4. The Renderer Asset is automatically assigned to the Pipeline Asset.
   ![](Images/2D/2d-urp12-pipeline-renderer-assigned.png)
   <br/>

5. To set the graphics quality settings, there are two options:

   **Option 1: For a single setting across all platforms**
   1. Go to **Edit > Project Settings** and select the **Graphics** category.
   ![](Images/2D/2d-urp12-graphics-srpsettings.png)
   <br/>
   2. Drag the **Pipeline Asset** created earlier to the **Scriptable Render Pipeline Settings** box, and adjust the quality settings.
   <br/>

   **Option 2: For settings per quality level**
   1. Go to **Edit > Project Settings** and select the [Quality](https://docs.unity3d.com/Manual/class-QualitySettings.html) category.
   ![](Images/2D/2d-urp12-graphics-qualitysettings.png)
   <br/>
   2. Select the quality levels to be included in your Project.
   3. Drag the **Pipeline Asset** created earlier to the **Rendering** box.
   ![](Images/2D/2d-urp12-graphics-quality-add-rendering-asset.png)
   <br/>
   4. Repeat steps 2-3 for each quality level and platform included in your Project.

The **2D Renderer** is now set up for your Project.

**Note:** If you use the **2D Renderer** in your Project, some of the options related to 3D rendering in the **Universal Render Pipeline Asset** will not affect or impact on your final app or game.
