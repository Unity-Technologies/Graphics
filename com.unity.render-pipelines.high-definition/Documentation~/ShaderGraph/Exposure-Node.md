# Exposure Node

Allow you to retrieve the exposure value of the current exposure or past frame.

## Output port

| name | type | description
--- | --- | ---
Output | float | The exposure value.

## Exposure Type

The exposure type allow you to choose between these different exposure values:
| name | description |
--- | ---
| CurrentMultiplier | current camera exposure value |
| InverseCurrentMultiplier | the inverse of the current camera exposure value |
| PreviousMultiplier | the previous frame exposure value |
| InversePreviousMultiplier | the inverse of the previous frame exposure value |
