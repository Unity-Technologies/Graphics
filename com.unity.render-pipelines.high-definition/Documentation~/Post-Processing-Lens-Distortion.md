# Lens Distortion

The **Lens Distortion** effect distorts the final rendered picture to simulate the shape of a real-world camera lens.

## Using Lens Distortion

**Lens Distortion** uses the [Volume](Volumes.html) framework, so to enable and modify **Lens Distortion** properties, you must add a **Lens Distortion** override to a [Volume](Volumes.html) in your Scene. To add **Lens Distortion** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Lens Distortion**. HDRP now applies **Lens Distortion** to any Camera this Volume affects.

## Properties

![](Images/Post-processingLensDistortion1.png)

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Intensity**    | Use the slider to set the overall strength of the distortion effect. |
| **X Multiplier** | Use the slider to set the distortion intensity on the x-axis. This value acts as a multiplier so you can set this value to 0 to disable distortion on this axis, |
| **Y Multiplier** | Use the slider to set the distortion intensity on the y-axis. This value acts as a multiplier so you can set this value to 0 to disable distortion on this axis, |
| **Center**       | Set the center point of the distortion effect on the screen. |
| **Scale**        | Use the slider to set the value for global screen scaling. This zooms the render to hide the  borders of the screen. When you use a high distortion, pixels on the borders of the screen can break because they rely on information from pixels outside the screen boundaries that don't exist. This property is useful for hiding these broken pixels around the screen border. |