# Variable Rate

Menu Path : **Spawn > Variable Rate**

The Variable Rate Block uses a more advanced approach than the [Constant Rate Block](Block-ConstantRate.md). The spawn rate this Block applies is linearly interpolated between two rates within an interval defined by the period.

## Block compatibility

This Block is compatible with the following Contexts:

- [Spawn](Context-Spawn.md)

## Block properties

| **Input**  | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **Rate**   | Vector2  | The two values this Block uses to calculate the spawn rate. The current rate is always a value between the two dimensions of this Vector2. |
| **Period** | Vector2  | The minimum and maximum time period before this Block calculates a new spawn rate. A higher period leads to a smoother change in rate while a small period leads to a higher frequency rate change. |

## Remarks

You can emulate this Block with the following equivalent custom spawner callback implementation:

```C#
class VariableRateEquivalent : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        public Vector2 Rate;
        public Vector2 Period;
    }

    static private readonly int rateID = Shader.PropertyToID("Rate");
    static private readonly int periodID = Shader.PropertyToID("Period");

    public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }

    float m_PrevRate;
    float m_NextRate;
    float m_PrevTime;
    float m_NextTime;

    void AdvanceRate(float totalTime, Vector2 rate, Vector2 period)
    {
        m_PrevRate = m_NextRate;
        m_PrevTime = totalTime;

        m_NextRate = Random.Range(rate.x, rate.y);
        m_NextTime = totalTime + Random.Range(period.x, period.y);
    }

    public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        if (state.newLoop)
            AdvanceRate(state.totalTime, vfxValues.GetVector2(rateID), vfxValues.GetVector2(periodID));

        if (state.playing)
        {
            float range = m_NextTime - m_PrevTime;
            float ratio = 1.0f;
            if (range > 0.0f)
                ratio = Mathf.Clamp01((state.totalTime - m_PrevTime) / range);

            float rate = Mathf.Lerp(m_PrevRate, m_NextRate, ratio);
            if (ratio == 1.0f)
                AdvanceRate(state.totalTime, vfxValues.GetVector2(rateID), vfxValues.GetVector2(periodID));

            state.spawnCount += rate * state.deltaTime;
        }
    }

    public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }
}
```
