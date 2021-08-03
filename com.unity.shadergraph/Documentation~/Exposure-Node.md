# Exposure Node

The Exposure Node allows you to get the Camera's exposure value from the current or previous frame.

## Render pipeline compatibility

| **Node** | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------- | ----------------------------------- | ------------------------------------------ |
| Exposure | No                                  | Yes                                        |

## Ports

| name | **Direction** | type | description |
|--- | --- | --- | --- |
|**Output** |Output | float | The exposure value.|

## Exposure Type

You can use Exposure Type to select which exposure value to get.
| name | description |
|--- | ---|
| **CurrentMultiplier** | Gets the Camera's exposure value from the current frame. |
| **InverseCurrentMultiplier** | Gets the inverse of the Camera's exposure value from the current frame. |
| **PreviousMultiplier** | Gets the Camera's exposure value from the previous frame. |
| **InversePreviousMultiplier** | Gets the inverse of the Camera's exposure value from the previous frame. |