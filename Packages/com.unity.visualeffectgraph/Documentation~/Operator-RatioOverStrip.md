# Get Ratio Over Strip [0..1]

Menu Path : **Operator** > **Attribute** > **Get Ratio Over Strip [0..1]**

The **Get Ratio Over Strip** attribute returns the ratio of the particle index relative to the total count of particles in that strip, as a value between 0.0 and 1.0.

Because this Operator relies on the **Get Particle Count In Strip** operator, it shares the same limitation: it can return wrong values if you use it in the Initialize context, if the strip index property is not constant. For more information, refer to [Get Attribute: particleCountInStrip](Operator-GetAttributeParticleCountInStrip.md#details).

```
t = particleIndexInStrip / (particleCountInStrip - 1)
```

## Operator properties

| **Output** | **Type** | **Description**                                   |
| ---------- | -------- | ------------------------------------------------- |
| **t**      | float    | The ratio of the particle index relative to the total particle count in that strip. |

## Details

If the system you use this Operator in does not have strips, Unity returns 0 instead.
