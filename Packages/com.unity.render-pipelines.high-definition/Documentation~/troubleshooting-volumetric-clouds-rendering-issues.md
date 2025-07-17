# Troubleshooting Volumetric Clouds rendering issues

Prevent far-away clouds from disappearing when the camera moves along the y-axis, especially when traversing the clouds.

## Symptoms

Far-away clouds disappear as the camera moves along the y-axis when the **Rendering Space** is set to **World**.

## Cause

To enhance performance, the default number of steps used to evaluate the clouds' transmittance is low. This causes the clouds further away to disappear as the camera moves along the y-axis.

**Note:** In Unity 6, the default rendering space was changed to **World**, making this issue more noticeable than in Unity 2022 LTS, where **Camera** was the default setting.

## Resolution

To prevent far-away clouds from disappearing, increase the **Num Primary Steps** value in the **Quality** section of the **Volumetric Clouds** override.

**Important:** A high **Num Primary Steps** value can hinder performance. Adjust this setting with caution.

## Additional resources

- [Create realistic clouds (volumetric clouds)](create-realistic-clouds-volumetric-clouds.md)
