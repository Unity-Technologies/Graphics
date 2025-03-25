# Create a fabric material

## Create a Cotton/Wool shader material

New Materials in HDRP use the [Lit shader](lit-material.md) by default. To create a Cotton/Wool Material from scratch, create a Material and then make it use the Cotton/Wool shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select **Create** > **Material**. This adds a new Material to your Unity Project’s Asset folder.

3. Click the **Shader** drop-down at the top of the Material Inspector, and select **HDRP** > **Fabric** > **Cotton/Wool**.

Refer to [Cotton/Wool Material Inspector reference](cotton-wool-material-inspector-reference.md) for more information.

### Import the Cotton/Wool Fabric Sample

HDRP comes with Cotton/Wool Material samples to further help you get started. To find this Sample:

1. Go to **Windows** > **Package Management** > **Package Manager**, and select **High Definition RP** from the package list.
2. In the main window that shows the package's details, find the **Samples** section.
3. To import a Sample into your Project, click the **Import into Project** button. This creates a **Samples** folder in your Project and imports the Sample you selected into it. This is also where Unity imports any future Samples into.
4. In the Asset window, go to **Samples** > **High Definition RP** > **11.0** and open the **Fabric** scene. Here you can see the sample materials set up in-context in the scene, and available for you to use.

## Create a Silk shader material

New Materials in HDRP use the [Lit shader](lit-material.md) by default. To create a Silk Material from scratch, create a Material and then make it use the Silk shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.

3. Click the **Shader** drop-down at the top of the Material Inspector, and select **HDRP > Fabric > Silk**.

Refer to [Silk Material Inspector reference](silk-material-inspector-reference.md) for more information.

### Importing the Silk Fabric Sample

The High Definition Render Pipeline (HDRP) also comes with Silk Material samples to further help you get started. To find this Sample:

1. Go to **Windows** > **Package Management** > **Package Manager**, and select **High Definition RP** from the package list.
2. In the main window that shows the package's details, find the **Samples** section.
3. To import a Sample into your Project, click the **Import into Project** button. This creates a **Samples** folder in your Project and imports the Sample you selected into it. This is also where Unity imports any future Samples into.
4. In the Asset window, go to **Samples** > **High Definition RP** > **11.0** and open the **Fabric** scene. Here you can see the silk sample material set up in-context in the scene, and available for you to use.

## Creating a Fabric Shader Graph

To create a Fabric material in Shader Graph, use one of the following methods:

* Modify an existing Shader Graph.

    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button, and select **HDRP**.
    3. In the **Material** drop-down, select **Fabric**.

* Create a new Shader Graph. Go to **Assets** > **Create** > **Shader Graph** > **HDRP** and click **Fabric Shader Graph**.

Refer to [Fabric Master Stack reference](fabric-master-stack-reference.md) for more information.




