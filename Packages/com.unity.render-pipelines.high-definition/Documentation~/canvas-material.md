# Canvas material

The Canvas Master Stack material type enables you to author Shader Graph shaders that can be applied to [UGUI user interface elements](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UICanvas.html).

## Create a Canvas Shader Graph

To create a Canvas material in Shader Graph, use one of the following methods:

* Modify an existing Shader Graph:
    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets** > **Plus**, and select **HDRP**.
    3. In the **Material** drop-down, select **Canvas**.
* Create a new Shader Graph. Go to **Assets** > **Create** > **Shader Graph** > **HDRP**, and select **Canvas Shader Graph**.

Refer to [Canvas Master Stack reference](canvas-master-stack-reference.md) for more information.

## Limitations

HDRP treats Canvas materials the same as unlit transparent materials. You may experience sorting issues with refractive and prerefraction objects when stacking them on top of each others. You can workaround this limitation by using an Unlit transparent shadergraph for the canvas and controlling the renderqueue and sorting options.
