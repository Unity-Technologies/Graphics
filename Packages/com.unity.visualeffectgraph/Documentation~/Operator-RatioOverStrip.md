# Ratio Over Strip

Menu Path : **Operator > Attribute > Ratio Over Strip**

The **Ratio Over Strip** Operator returns the ratio of the particle index relative to the total count of particles in that strip, as a value between 0.0 and 1.0.

```
t = particleIndexInStrip / (particleCountInStrip - 1)
```

## Operator properties

| **Output** | **Type** | **Description**                                   |
| ---------- | -------- | ------------------------------------------------- |
| **t**      | float    | The ratio of the particle index relative to the total particle count in that strip. |

## Details

If the system you use this Operator in does not have strips, Unity returns 0 instead.
