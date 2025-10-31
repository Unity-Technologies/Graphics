# Terrain Properties Node

## Description

Use the Terrain Properties node to input properties from the actively rendered Terrain into the Contexts of a Terrain Lit shader graph.

The Terrain Properties node is compatible only with Terrain Lit shader graphs. You can't use it with other types of shaders.

## Ports

| Name             | Direction        | Type        | Description  |
|:-----------------|:-----------------|:------------|:-------------|
| Max Local Height | Output           | float       | The maximum local height stored in the Terrain heightmap. Unlike the actual values stored in the heightmap, this is not a normalized value. |
| Basemap Distance | Output           | float       | The basemap distance as set in the Terrain's settings. |
| Layers Count     | Output           | uint        | The number of Terrain Layers assigned to the Terrain. |
