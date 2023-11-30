# Convert post-processing scripts

HDRP no longer supports the **Post Processing** package and instead includes its own [implementation for post-processing](Post-Processing-Main.md). If your Project uses the Post Processing package, remove the Post Processing package from the Project. To do this:

1. In the Unity Editor, open the Package Manager window (menu: **Window** > **Package Manager**).
2. Find and select the **Post Processing** package, and click **Remove**.

If your Project uses the Post Processing package's Scripting API to edit post-processing effects, you need to update your scripts to work with the new post-processing effects. To convert the Scene to HDRP post-processing:

1. In the Hierarchy, delete your post-processing GameObject.
2. Create a new **Global Volume** GameObject (menu: **GameObject** > **Volume** > **Global Volume**) and name it "Post-processes".
3. Create a new Volume Profile:

    1. Open the Global Volume's Inspector window and go to the **Volume** component.
    2. Go to **Profile** and select **New**.

4. Add a **Tonemapping** override to the Volume:

    1. Go to **Add Override** > **Post-processing** > **Tonemapping**.
    2. Enable **Mode** and set it to **ACES**.

5. Add a **Bloom** override to the Volume

    1. Go to **Add Override** > **Post-processing** > **Bloom**.
    2. Enable **Intensity** and set it to **0.2**.

**Note**: The result of the Bloom isn't the same as the one in the Post Processing package. This is because HDRP's Bloom effect is physically accurate, and mimics the quality of a camera lens.

5. Add a **Motion Blur** override to the Volume:

    1. Go to **Add Override** > **Post-processing** > **Motion Blur**.
    2. Enable **Intensity** and set it to **0.1**.

6. Add a **Vignette** override to the Volume:

    1. Go to **Add Override** > **Post-processing** > **Vignette**.
    2. Set the following property values:

        * Enable **Intensity** and set it to **0.55**.
        * Enable **Smoothness** and set it to **0.4**.
        * Enable **Roundness** and set it to **0**.

7. Add a **Depth Of Field** override to the Volume:

    1. Go to **Add Override** > **Post-processing** > **Depth Of Field**.
    2. Set the following property values:

        * Enable **Focus Mode** and set it to **Manual**.
        * In the **Near Blur** section:

            1. Enable **Start** and set it to **0**
            2. Enable **End** and set it to 0.5

        * In the **Far Blur** section:

            1. Enable **Start** and set it to **2**.
            2. Enable **End** and set it to **10**. This effect is only visible in the Game view.

8. Select the **Global Settings** GameObject to view it in the Inspector.

9. In the Volume component, add an **Ambient** **Occlusion** override:

    1. Go to **Add Override** > **Lighting** > **Ambient Occlusion**.
    2. Enable **Intensity** and set it to **0.5**.
