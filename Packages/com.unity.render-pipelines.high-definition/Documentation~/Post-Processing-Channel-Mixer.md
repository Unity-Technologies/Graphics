# Channel Mixer

The Channel Mixer effect modifies the influence of each input color channel on the overall mix of the output channel. For example, if you increase the influence of the green channel on the overall mix of the red channel, all areas of the final image that are green (including neutral/monochrome) tint to a more reddish hue.

## Using Channel Mixer

**Channel Mixer** uses the [Volume](Volumes.md) framework, so to enable and modify **Channel Mixer** properties, you must add a **Channel Mixer** override to a [Volume](Volumes.md) in your Scene. To add **Channel Mixer** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Post-processing** and select **Channel Mixer**. HDRP now applies **Channel Mixer** to any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Post-processingChannelMixer1.png)

### Output channels

Before you modify the influence of each input channel, you must select the output color channel to influence. To do this, click the button for the channel that you want to set the influence for.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Red Output Channel**      | Use the slider to set the influence of each channel on the red output channel. |
| **Green Output Channel**    | Use the slider to set the influence of each channel on the green output channel. |
| **Blue Output Channel**     | Use the slider to set the influence of each channel on the blue output channel. |
