# Deep learning super sampling (DLSS)

[NVIDIA Deep Learning Super Sampling (DLSS)](https://www.nvidia.com/en-us/geforce/technologies/dlss/) is a rendering technology that uses artificial intelligence to increase graphics performance. The High Definition Render Pipeline (HDRP) natively supports DLSS.

## Requirements and compatibility

This section includes HDRP-specific requirements and compatibility information for DLSS.

### Platforms

HDRP supports DLSS on the following platforms:

* DirectX 11 on Windows 64 bit
* DirectX 12 on Windows 64 bit
* Vulkan on Windows 64 bit

**Note**: HDRP doesn't support DLSS for Metal, Linux, Windows using x86 architecture (Win32), or any other platform.

To build your project for Windows, use x86_64 architecture (Win64).

For information about the hardware requirements of DLSS, see [NVIDIA'S DLSS requirements](https://developer.nvidia.com/nvidia-dlss-access-program).

## Using DLSS

To use DLSS in your scene:

1. Add the NVIDIA package. You can either do this automatically or manually.

    * To install the NVIDIA package automatically:

        1. Select an [HDRP Asset](HDRP-Asset.md) and view it in the Inspector.
        2. Go to **Rendering** > **Dynamic Resolution** and click **Install NVIDIA Package**.

    * To install the NVIDIA package manually:

        1. Open the [Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html) (menu: **Window** > **Package Management** > **Package Manager**).
        2. Select **Packages**, then select **Built-in**.
        3. In the packages list view, find and select the NVIDIA package.
        4. In the bottom right of the package-specific detail view, select **Enable**.

2. Enable DLSS in your HDRP Asset.

    1. Select the HDRP Asset you want to enable DLSS for and view it in the Inspector.
    2. Go to **Rendering** > **Dynamic Resolution** and select **Enable**.
    3. In the **Dynamic Resolution** section, select the **Enable DLSS** property to expose other properties that you can use to customize DLSS. For information about these properties, see the [HDRP Asset](HDRP-Asset.md) documentation.

3. Enable DLSS for each Camera you want to use it with.

    1. In the Hierarchy or Scene view, select a Camera and view it in the Inspector.
    2. Select **Allow Dynamic Resolution** to expose the DLSS settings. For more information see the [Dynamic Resolution](Dynamic-Resolution.md) guide.
    3. Enable **Allow DLSS** to expose other properties that you can use to customize DLSS for the Camera. For information about these properties, see the [Camera](hdrp-camera-component-reference.md) documentation.

4. Set the DLSS quality mode. You can do this on a project level or a per-camera level.

    * To change the DLSS quality mode for your whole project:

        1. Select the HDRP Asset that has DLSS enabled and view it in the Inspector.
        2. Go to **Rendering** > **Dynamic Resolution** > **DLSS** and set the **Mode** property to the quality mode you want.

    * To override the DLSS quality mode for a particular Camera:

        1. In the Hierarchy or Scene view, select a Camera and view it in the Inspector.
        2. Select **Use Custom Quality**.
        3. Set the **Mode** property to the quality mode you want.

5. To fine-tune the DLSS image quality for your project, select a DLSS render preset for each quality mode available under the [HDRP Asset](HDRP-Asset.md)

<a name="qualityandpresets"></a>

### DLSS quality modes and render presets

| Quality mode | Explanation | Upscale ratio | Render percentage |
|- |- |- |- |
| Maximum Quality | Provides the highest image quality but lowers performance. | 1.50  | 67% |
| Balanced | Balances image quality and performance. | 1.72 | 58% |
| Maximum Performance | Increases performance but lowers image quality. | 2.00 | 50% |
| Ultra Performance | Provides the highest performance and the lowest image quality. | 3.00 | 33% | 
| DLAA | Provides AI-assisted anti-aliasing (deep-learning anti-aliasing) without upscaling | 1.00 | 100% |

Each quality mode provides a specific collection of DLSS Render presets.
Available presets are marked as '1' in the table below.

| Render Preset | Maximum Quality | Balanced | Maximum Performance | Ultra Performance | DLAA | Explanation | AI Model |
|- |- |- |- |- |- |- |- |
| Preset F |   |   |  | 1 | 1 | Provides the highest image stability. Default value for UltraPerformance. | CNN |
| Preset J | 1 | 1 | 1|   | 1 | Slightly lowers ghosting but increases flickering.<br/>NVIDIA recommends using **Preset K** instead of **Preset J**. | Transformer |
| Preset K | 1 | 1 | 1|   | 1 |  Provides the highest image quality. | Transformer |

The defaults for each quality mode are:

| **Quality mode** | **Default render preset** |
|- |- |
| **Maximum Quality** | Preset K 
| **Balanced** | Preset K 
| **Maximum Performance** | Preset K 
| **Ultra Performance** | Preset F 
| **DLAA** | Preset K |

DLSS render presets are project-specific. Presets are available only from the HDRP Asset settings. You can't override presets on a per-camera basis.

### DLSS and Dynamic Resolution

The **Use Optimal Settings** checkbox in the [HDRP Assets](HDRP-Asset.md) is enabled by default. This means that DLSS sets the dynamic resolution scale automatically.
If you disable this checkbox DLSS uses the same dynamic resolution scale set by the project. For more information see the [Dynamic Resolution](Dynamic-Resolution.md) guide.

### Mip bias in DLSS

To enable automatic mip bias correction when you enable DLSS, open the [HDRP Asset](HDRP-Asset.md) and enable the **Use Mip Bias** checkbox.

If you need a specific custom mip bias for a Texture, create a custom sampler that samples from TextureInput, SamplerInput, UV, and MipBias. To do this, enter the following script into the Node Settings ' Body field. The images below display this example:

```glsl
Out = SAMPLE_TEXTURE2D_BIAS(TextureInput, SamplerInput, UV, MipBias);
```

![Example: The above script in the Node Settings Body field.](Images/CustomMipSupportNode.png)

![Example: Custom Mip support node in a shader graph.](Images/CustomMipSupportNodeExample.png)

## Additional resources

- [Introduction to changing resolution scale](https://docs.unity3d.com/6000.0/Documentation/Manual/resolution-scale-introduction.html)

