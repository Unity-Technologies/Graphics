# Post-processing in the High Definition Render Pipeline

The High Definition Render Pipeline (HDRP) includes its own purpose-built implementation for [post-processing](https://docs.unity3d.com/Manual/PostProcessingOverview.html). This is built into HDRP, so you don't need to install any other package.

Post-processing effects in HDRP use the [Volume](Volumes.md) framework. You add post-processing effects to your Camera in the same way you add any other [Volume Override](Volume-Components.md).

**Note**: HDRP already enables some post-processing effects in the [HDRP Global Settings](Default-Settings-Window.md), go to Section **Volume Profiles**.

**Note**: Some post-processing effects are enabled by default in the [HDRP Global Settings](Default-Settings-Window.md#volume-profiles).

The images below display a Scene with and without HDRP post-processing.

![](Images/PostProcessingMain1.png)
Without post-processing.

![](Images/PostProcessingMain2.png)
With post-processing.
