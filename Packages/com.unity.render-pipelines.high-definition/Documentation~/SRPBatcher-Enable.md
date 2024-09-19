# Enable the SRP Batcher

When you use HDRP, Unity enables the SRP Batcher by default. Disabling the SRP Batcher isn't recommended. However, you can temporarily disable the SRP Batcher for debugging purposes.

To enable and disable the SRP Batcher at build time using the Editor:

1. In the Project window, select the [HDRP Asset](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/HDRP-Asset.html).
2. In the Inspector for the asset, enter [Debug mode](https://docs.unity3d.com/6000.0/Documentation/Manual/InspectorOptions). In Debug mode, you can see the properties of  the HDRP Asset, including the SRP Batcher property.
3. Select **Enable** **SRP Batcher** to enable or disable the SRP Batcher.

To enable or disable the SRP Batcher at runtime, toggle the following global variable in your C# code:

```
GraphicsSettings.useScriptableRenderPipelineBatching = true;
```

