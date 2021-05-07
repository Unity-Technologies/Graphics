# HDRP custom Material Inspectors

Custom Material Inspectors allow you to define how Unity displays properties in the Material Inspector for a particular shader. The High Definition Render Pipeline (HDRP) makes heavy use of this feature to make the editing experience for all of its shaders as simple and intuitive as possible.

This page contains information about how to create custom Material Inspectors in HDRP. For general information about what custom Material Inspectors are and how to assign one to a material, see [custom Material Inspector](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/custom-material-inspector.html).

## UI blocks

Material Inspectors for most HDRP shaders are made of UI blocks. A UI block is a foldable section that contains a named group of properties. For example, **Surface Options** and **Surface Inputs** in the below image are both UI blocks.

![](Images/custom-material-inspector-ui-blocks.png)

The order of the UI block list defines the display order of the UI blocks in the Inspector. The first item in the list renders at the top and the last item renders at the bottom.

For information about how to create UI Blocks for a custom Material Inspector, see [UI blocks](#creating-ui-blocks).

## HDShaderGUI

Every Material Inspector in HDRP is built on top of the `HDShaderGUI` class. This class ensures that Material Inspectors are always compatible with HDRP. To do this, it calls `SetupMaterialKeywordsAndPass` when any changes occur, which patches the Material's keywords, stencil properties, pass configuration, and other important elements.

HDRP provides three helper classes to help create a Material Inspector:

- **UnlitShaderGraphGUI**: Provides a simple interface to create a custom Material Inspector for unlit Materials. For information about how to implement an UnlitShaderGraphGUI, see [custom Unlit Material Inspector](#custom-unlit-material-inspector).
- **LightingShaderGraphGUI**: Provides a simple interface to create a custom Material Inspector for lit Materials. For information about how to implement a LightingShaderGraphGUI, see [custom Lit Material Inspector](#custom-lit-material-inspector).
- **DecalShaderGraphGUI**: Provides a simple interface to create a custom Material Inspector for decal Materials. For information about how to implement a DecalShaderGraphGUI, see [custom Decal Material Inspector](#custom-decal-material-inspector).

However, for more complex use-cases, it might be necessary to directly use the **HDShaderGUI**. For information on how to do this, see the [custom Material Inspector example](#bespoke-material-inspector).

## Creating a Custom Material Inspector in HDRP

This section explains how to create:

- [UI Blocks](#creating-ui-blocks)
- A [Lit Material Inspector](#custom-lit-material-inspector)
- An [Unlit Material Inspector](#custom-unlit-material-inspector)
- A [Decal Material Inspector](#custom-decal-material-inspector)
- A bespoke [custom Material Inspector](#bespoke-material-inspector)

### Creating UI blocks

Create UI blocks to store and organize related material properties. A UI block inherits from the [MaterialUIBlock](../api/UnityEditor.Rendering.HighDefinition.MaterialUIBlock.html) abstract class. For an example of how to implement a UI block, see the following code sample:

```CSharp
using UnityEditor.Rendering.HighDefinition;
using UnityEditor;

class ColorUIBlock : MaterialUIBlock
{
    MaterialProperty colorProperty;

    public override void LoadMaterialProperties()
    {
        colorProperty = FindProperty("_MyColor");
    }

    public override void OnGUI()
    {
        materialEditor.ShaderProperty(colorProperty, "My Color");
    }
}
```


This code sample fetches the `_MyColor` property in the shader and displays it. Note that if the Custom Material Inspector is for a Shader Graph, for the UI block to find the property, you must set the correct reference name in the Shader Graph's Node Settings. To do this:

1. Open the Shader Graph.
2. Select the property to display and view it in the Node Settings tab of the Graph Inspector.
3. Set **Reference** to the name `FindProperty` uses. In this example, it is **_MyColor**.

![](Images/custom-material-inspector-node-settings-example.png)

The following image shows how the Inspector looks for the UI block in the code sample.

![](Images/custom-material-inspector-ui-block-example.png)

#### Implementing a foldout section

By default, UI blocks are not nested in a foldout. The foldouts in other HDRP Material Inspectors use the `MaterialHeaderScope` class. This class specifies the name of the header and whether the section is expanded or not. For an example of how to implement a UI block in a foldout, see the following code sample:


```CSharp
class ColorUIBlock : MaterialUIBlock
{
    ExpandableBit   foldoutBit;

    MaterialProperty colorProperty;

    public ColorUIBlock(ExpandableBit expandableBit)
    {
        foldoutBit = expandableBit;
    }

    public override void LoadMaterialProperties()
    {
        colorProperty = FindProperty("_MyColor");
    }

    public override void OnGUI()
    {
        using (var header = new MaterialHeaderScope("Color Options", (uint)foldoutBit, materialEditor))
        {
            if (header.expanded)
            {
                materialEditor.ShaderProperty(colorProperty, "My Color");
            }
        }
    }
}
```


Note that to track whether the foldout is expanded or not, `MateralHeaderScope` uses an `ExpandableBit`. To assign the `ExpandableBit`, this UI block example has a constructor that takes an ExpandableBit as a parameter. Because Unity serializes the state of each foldout in Editor preferences, you should use the `User[0..19]` part of the ExpandableBit enum to avoid overlap with built-in reserved bits. For an example of how to do this, see the code sample in [Custom Lit Material Inspector](#custom-lit-material-inspector).

You can also hardcode the bit in a UI block but this is not best practice especially if you intend to create a lot of UI blocks that multiple materials share.

The following image shows how the Inspector looks for the UI block in the above code sample.

![](Images/custom-material-inspector-ui-block-foldout-example.png)

#### Block cross-reference

If you need to access another UI block from your current UI block, the `parent` member gives you access to the list of UI blocks in the custom Material Inspector. You can use this to find the UI block and use the result to call a function or get a material property for example.


```CSharp
var surfaceBlock = parent.FirstOrDefault(b => b is SurfaceOptionUIBlock) as SurfaceOptionUIBlock;
```


**Note**: You can’t access the parent in the constructor of a UIBlock so be sure to access it either in `LoadMaterialProperties` or `OnGUI`.

## Examples

This section provides example implementations for the following custom Material Inspectors :

- [Lit](#custom-lit-material-inspector)
- [Unlit](#custom-unlit-material-inspector)
- [Decals](#custom-decal-material-inspector)
- [Bespoke](#bespoke-material-inspector)

### Custom Lit Material Inspector

For Lit Materials, the custom Material Inspector should inherit from `LightingShaderGraphGUI`. The `LightingShaderGraphGUI` represents any Shader Graph that uses lighting. For HDRP, this includes Lit, StackLit, Hair, Fabric, and Eye.

The `LightingShaderGraphGUI` class directly inherits from `HDShaderGUI` and overrides every function that renders the UI. This means that any class that inherits from `LightingShaderGraphGUI` already works correctly, all the new class needs to do is add/remove some UI blocks. For an example of this, see the following code snippet:

```CSharp
using UnityEditor.Rendering.HighDefinition;

public class LightingInspectorExample : LightingShaderGraphGUI
{
    public LightingInspectorExample()
    {
        // Remove the ShaderGraphUIBlock to avoid having duplicated properties in the UI.
        uiBlocks.RemoveAll(b => b is ShaderGraphUIBlock);

        // Insert the color block just after the Surface Option block.
        uiBlocks.Insert(1, new ColorUIBlock(MaterialUIBlock.ExpandableBit.User0));
    }
}
```


This code sample produces the following Inspector:

![](Images/custom-material-inspector-lit-example.png)

### Custom Unlit Material Inspector

For Unlit Materials, the custom Material Inspector should inherit from `UnlitShaderGraphGUI`.

The `UnlitShaderGraphGUI` class directly inherits from `HDShaderGUI` and overrides every function that renders the UI. This means that any class that inherits from `UnlitShaderGraphGUI` already works correctly, all the new class needs to do is add/remove some UI blocks. For an example of this, see the following code snippet:

```CSharp
using UnityEditor.Rendering.HighDefinition;

public class UnlitExampleGUI : UnlitShaderGraphGUI
{
    public UnlitExampleGUI()
    {
        // Remove the ShaderGraphUIBlock to avoid having duplicated properties in the UI.
        uiBlocks.RemoveAll(b => b is ShaderGraphUIBlock);

        // Insert the color block just after the Surface Option block.
        uiBlocks.Insert(1, new ColorUIBlock(MaterialUIBlock.ExpandableBit.User0));
    }
}
```


This code sample produces the following Inspector:

![](Images/custom-material-inspector-unlit-example.png)

### Custom Decal Material Inspector

For Decal Materials, the custom Material Inspector should inherit from `DecalShaderGraphGUI`.

The `DecalShaderGraphGUI` class directly inherits from `HDShaderGUI` and overrides every function that renders the UI. This means that any class that inherits from `DecalShaderGraphGUI` already works correctly, all the new class needs to do is add/remove some UI blocks. For an example of this, see the following code snippet:


```CSharp
using UnityEditor.Rendering.HighDefinition;

public class DecalGUIExample : DecalShaderGraphGUI
{
    public DecalGUIExample()
    {
        // Remove the ShaderGraphUIBlock to avoid having duplicated properties in the UI.
        uiBlocks.RemoveAll(b => b is ShaderGraphUIBlock);

        // Insert the color block just after the Surface Option block.
        uiBlocks.Insert(1, new ColorUIBlock(MaterialUIBlock.ExpandableBit.User0));
    }
}
```


This code sample produces the following Inspector:

![](Images/custom-material-inspector-decal-example.png)

### Bespoke Material Inspector

If you require a more customizable Material Inspector than UI blocks can provide, HDRP enables you to create the Material Inspector almost from scratch. Similar to `[ShaderGUI](https://docs.unity3d.com/ScriptReference/ShaderGUI.html)` in the Built-in Render Pipeline, HDRP does this using `HDShaderGUI`.

```CSharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;

public class ScratchInspectorExample : HDShaderGUI
{
    // In this function, add the code that will render your custom Inspector.
    protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        EditorGUILayout.LabelField("Hello World!");
    }

    // This function will ensure that our material is always using the correct keyword setup.
    protected override void SetupMaterialKeywordsAndPass(Material material) => HDShaderUtils.ResetMaterialKeywords(material);
}
```


Note that `HDShaderGUI` directly inherits from `ShaderGUI`, which means you can override `ShaderGUI` functions such as `OnMaterialPreviewGUI`. The only function you cannot override is `OnGUI` because `HDShaderGUI` seals it. Instead, override the `OnMaterialGUI` function.

The `SetupMaterialKeywordsAndPass` function is very important because it ensures that the Material uses the correct keyword setup. HDRP stores the Material state in the properties themselves, this function reads these properties and then sets up the shader keywords these properties require. For example, if you enable the `doubleSidedEnable` property on a Material, HDRP requires the `_DOUBLESIDED_ON` shader keyword otherwise the material does not work. `SetupMaterialKeywordsAndPass` enables/disables this shader keyword based on the value of the `doubleSidedEnable` property.

This is why it is important to call `ApplyKeywordsAndPassesIfNeeded` when there is a change in the material UI. For example, `LightingShaderGraphGUI` does this just after it displays the UI block UI list like so:

```CSharp
protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
{
    using (var changed = new EditorGUI.ChangeCheckScope())
    {
        m_UIBlocks.OnGUI(materialEditor, props);
        ApplyKeywordsAndPassesIfNeeded(changed.changed, m_UIBlocks.materials);
    }
}
```
