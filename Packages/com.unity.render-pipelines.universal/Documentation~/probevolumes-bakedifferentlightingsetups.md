# Bake different lighting setups with Lighting Scenarios

A Lighting Scenario contains the baked lighting data for a scene or Baking Set. You can bake different lighting setups into different Lighting Scenario assets, and change which one the Universal Render Pipeline (URP) uses at runtime.

For example, you can create one Lighting Scenario with the lights on, and another Lighting Scenario with the lights off. At runtime, you can enable the second Lighting Scenario when the player turns the lights off.

## Enable Lighting Scenarios

To use Lighting Scenarios, go to the active [URP Asset](universalrp-asset.md) and enable **Lighting** > **Light Probe Lighting** > **Lighting Scenarios**.

## Add a Lighting Scenario

To create a new Lighting Scenario so you can store baking results inside, do the following:

1. Open the [Adaptive Probe Volumes panel](probevolumes-lighting-panel-reference.md) in the Lighting window.
2. In the **Lighting Scenarios** section, select the **Add** (**+**) button to add a Lighting Scenario.

## Bake into a Lighting Scenario

To bake into a Lighting Scenario, follow these steps:

1. In the **Lighting Scenarios** section, select a Lighting Scenario to make it active.
2. Select **Generate Lighting**. URP stores the baking results in the active Lighting Scenario.

You can set which Lighting Scenario URP uses at runtime using the [ProbeReferenceVolume API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.ProbeReferenceVolume.html).

If you change the active Lighting Scenarios at runtime, URP changes only the indirect lighting data in the Light Probes. You might still need to use scripts to move geometry, modify lights or change direct lighting.

## Blend between Lighting Scenarios

To enable blending between Lighting Scenarios, go to the active [URP Asset](universalrp-asset.md) and enable **Light Probe Lighting** > **Scenario Blending**.

You can blend between Lighting Scenarios at runtime using the [BlendLightingScenario API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.ProbeReferenceVolume.html#UnityEngine_Rendering_ProbeReferenceVolume_BlendLightingScenario_System_String_System_Single_).

For example, the following script does the following:

1. Sets `scenario01` as the active Lighting Scenario.
2. Sets up the number of cells to blend per frame, which can be useful for optimization purposes.
3. Updates the Adaptive Probe Volume blending factor every frame to blend between `scenario01` and `scenario02`.

```
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendLightingScenarios : MonoBehaviour
{
    UnityEngine.Rendering.ProbeReferenceVolume probeRefVolume;
    public string scenario01 = "Scenario01Name";
    public string scenario02 = "Scenario02Name";
    [Range(0, 1)] public float blendingFactor = 0.5f;
    [Min(1)] public int numberOfCellsBlendedPerFrame = 10;

    void Start()
    {
        probeRefVolume = UnityEngine.Rendering.ProbeReferenceVolume.instance;
        probeRefVolume.lightingScenario = scenario01;
        probeRefVolume.numberOfCellsBlendedPerFrame = numberOfCellsBlendedPerFrame;
    }

    void Update()
    {
        probeRefVolume.BlendLightingScenario(scenario02, blendingFactor);
    }
}
```

### Preview blending between Lighting Scenarios

You can use the [Rendering Debugger](features/rendering-debugger.md#probe-volume-panel) to preview transitions between Lighting Scenarios. Follow these steps:

1. Go to **Window** > **Analysis** > **Rendering Debugger** to open the Rendering Debugger.
2. Set **Scenario Blend Target** to a Lighting Scenario.
3. Use **Scenario Blending Factor** to check the effect of blending between the Lighting Scenarios in the Scene view.

### Keep Light Probes the same in different Lighting Scenarios

If you move static geometry between bakes, Light Probe positions might be different. This means you can't blend between Lighting Scenarios, because the number of Light Probes and their positions must be the same in each Lighting Scenario you blend between.

To avoid this, you can prevent URP recomputing probe positions when you bake. Follow these steps:

1. Bake one Lighting Scenario.
2. Set another Lighting Scenario as the active Lighting Scenario.
3. Change your scene lighting or geometry.
4. In the **Probe Placement** section, set **Probe Positions** to **Don't Recalculate**.
5. Select **Generate Lighting** to recompute only the indirect lighting, and skip the probe placement computations.

## Additional resources

- [ProbeReferenceVolume API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.ProbeReferenceVolume.html)
- [Bake multiple scenes together with Baking Sets](probevolumes-usebakingsets.md)
