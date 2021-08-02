# Constant Rate

Menu Path : **Spawn > Constant Rate**

The Constant Rate Block adds a spawn count over time at a constant rate. For instance, if the rate is 10, this block tiggers 10 spawn events per second for its Spawn Context. A rate below one is also valid, if the rate is 0.5, the rate is once every two seconds.

## Block compatibility

This Block is compatible with the following Contexts:

- [Spawn](Context-Spawn.md)

## Block properties

| **Input** | **Type** | **Description**            |
| --------- | -------- | -------------------------- |
| **Rate**  | float    | The spawn rate per second. |

## Remarks

You can emulate this Block with the following equivalent custom spawner callback implementation: 

```C#

class ConstantRateEquivalent : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        [Min(0), Tooltip("Sets the number of particles to spawn per second.")]
        public float Rate = 10;
    }

    static private readonly int rateID = Shader.PropertyToID("Rate");

    public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }

    public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        if (state.playing)
        {
            float currentRate = vfxValues.GetFloat(rateID);
            state.spawnCount += currentRate * state.deltaTime;
        }
    }

    public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }
}
```

