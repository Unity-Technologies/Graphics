# Create hair and fur

## Create a Hair shader material

New Materials in HDRP use the [Lit shader](lit-material.md) by default. To create a Hair Material from scratch, create a Material and then make it use the Hair shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select **Create** > **Material**. This adds a new Material to your Unity Project’s Asset folder.

3. Click the **Shader** drop-down at the top of the Material Inspector, and select **HDRP > Hair**.

Refer to [Hair Material Inspector reference](hair-material-inspector-reference.md) for more information.

## Create a Hair Shader Graph

To create a Hair material in Shader Graph, you can either:

* Modify an existing Shader Graph.
    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button, and select **HDRP**.
    3. In the **Material** drop-down, select **Hair**.

* Create a new Shader Graph.
    1. Go to **Assets > Create >Shader Graph > HDRP** and click **Hair Shader Graph**.

Refer to [Hair Master Stack reference](hair-master-stack-reference.md) for more information.

## Import the Hair Sample

HDRP comes with a Hair Material sample to further help you get started. To find this Sample:

1. Go to **Windows** > **Package Manager**, and select **High Definition RP** from the package list.
2. In the main window that shows the package's details, find the **Samples** section.
3. To import a Sample into your Project, click the **Import into Project** button. This creates a **Samples** folder in your Project and imports the Sample you selected into it. This is also where Unity imports any future Samples into.
4. In the Asset window, go to **Samples** > **High Definition RP** > **11.0** and open the Hair scene. Here you will see the hair sample material set up in-context with a scene, and available for you to use.
