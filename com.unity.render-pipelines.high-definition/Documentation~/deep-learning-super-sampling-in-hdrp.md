# Deep learning super sampling (DLSS)

NVIDIA Deep Learning Super Sampling (DLSS) is a rendering technology that uses artificial intelligence to increase graphics performance. The High Definition Render Pipeline (HDRP) natively supports DLSS. For more information about DLSS see [Deep learning super sampling](https://docs.unity3d.com/2021.2/Documentation/Manual/deep-learning-super-sampling.html).

## Requirements and compatibility

This section includes HDRP-specific requirements and compatibility information for DLSS. For information about the general requirements and compatibility of DLSS, see [Deep learning super sampling](https://docs.unity3d.com/2021.2/Documentation/Manual/deep-learning-super-sampling.html).

### Platforms

HDRP supports DLSS on the following platforms:

DirectX 11 on Windows 64 bit
DirectX 12 on Windows 64 bit
Vulkan on Windows 64 bit
HDRP does not support DLSS for Metal, Linux, Windows using x86 architecture (Win32), or any other platform.

To build your project for Windows, use x86_64 architecture (Win64).

For information about the hardware requirements of DLSS, see [NVIDIA'S DLSS requirements](https://developer.nvidia.com/nvidia-dlss-access-program).

## Using DLSS

To use DLSS in your scene, you must:

1. Add the NVIDIA package.
2. Enable DLSS in your HDRP Asset.
3. Enable DLSS for each Camera you want to use it with.
4. Set the DLSS quality mode.

### Adding the NVIDIA package

To add the NVIDIA package to your Unity project, there are two methods you can use. To install it automatically:

1. Select an [HDRP Asset](HDRP-Asset.md) and view it in the Inspector.
2. Go to **Rendering** > **Dynamic Resolution** and click **Install NVIDIA Package**.

To install it manually:

1. Open the [Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html) (menu: **Window** > **Package Manager**).
2. Select **Packages**, then select **Built-in**.
3. In the packages list view, find and select the NVIDIA package.
4. In the bottom right of the package-specific detail view, select **Enable**.

### Enabling DLSS

After you install the NVIDIA package, more properties appear in [HDRP Assets](HDRP-Asset.md) and [Cameras](HDRP-Camera.md). This allows you to enable DLSS in your HDRP project. To do this:

1. Select the HDRP Asset you want to enable DLSS for and view it in the Inspector.
2. Go to **Rendering** > **Dynamic Resolution** and select **Enable**.
3. In the dynamic resolution section, select the **Enable DLSS** property.

Your Unity project now supports DLSS and you can now enable DLSS for Cameras in your scene. Enabling DLSS in the HDRP Asset exposes other properties that you can use to customize DLSS. For information about these properties, see the [HDRP Asset](HDRP-Asset.md) documentation.

1. In the Hierarchy or Scene view, select a Camera and view it in the Inspector.
2. Select **Allow Dynamic Resolution** to expose the DLSS settings. For more information see the [Dynamic Resolution](Dynamic-Resolution.md) guide.
3. Select **Allow DLSS**.
4. Enable **Allow DLSS** to expose other properties that you can use to customize DLSS for the Camera. For information about these properties, see the [Camera](HDRP-Camera.md) documentation.

### DLSS and Dynamic Resolution

The **Use Optimal Settings** checkbox in the [HDRP Assets](HDRP-Asset.md) is enabled by default. This means that DLSS sets the dynamic resolution scale automatically.
If you disable this checkbox DLSS uses the same dynamic resolution scale set by the project. For more information see the [Dynamic Resolution](Dynamic-Resolution.md) guide.

### Mip bias in DLSS

To enable automatic mip bias correction when you enable DLSS, open the [HDRP Asset](HDRP-Asset.md) and enable the **Use Mip Bias** checkbox.

If you need a specific custom mip bias for a Texture, create a custom sampler that samples from TextureInput, SamplerInput, UV, and MipBias. To do this, enter the following script into the Node Settings ' Body field. The images below display this example:

```glsl
Out = SAMPLE_TEXTURE2D_BIAS(TextureInput, SamplerInput, UV, MipBias);
```

![](Images/CustomMipSupportNode.png)


![](Images/CustomMipSupportNodeExample.png)


### Setting the DLSS quality mode

DLSS now works in your project, but you can change the quality mode to customize DLSS towards performance or quality. You can do this on a project level or a per-Camera level. For information about the available quality modes, see [Quality modes](https://docs.unity3d.com/2021.2/Documentation/Manual/deep-learning-super-sampling.html).

To change the DLSS quality mode for your whole project:

1. Select the HDRP Asset that has DLSS enabled and view it in the Inspector.
2. Go to **Rendering** > **Dynamic Resolution** > **DLSS** and set the **Mode** property to the quality mode you want.

To override the DLSS quality mode for a particular Camera:

1. In the Hierarchy or Scene view, select a Camera and view it in the Inspector.
2. Select **Use Custom Quality**.
3. Set the **Mode** property to the quality mode you want.
