# Synchronize water surfaces

If you use multiple water surfaces, you can synchronize the water simulation of each surface. For example, if you need to make sure the water surface is the same for all the players in a multiplayer game.

- To synchronize water surfaces as if the game just started, use the `water.simulationStart` API. For example:

	```cs
	water.simulationStart = DateTime.Now;
	```

- To set the exact water surface synchronization time in seconds:
	
	```cs
	water.simulationTime = 0;
	```

- To sync water surfaces and simplify network sync by removing latency concerns, copy the reference water surface's absolute time:

	```cs
	water.simulationStart = referenceSurface.simulationStart;
	```

- To sync water surfaces and  make local synchronization easier, copy the reference water surface's simulation time.

	```cs
	water.simulationTime = referenceSurface.simulationTime;
	```

> [!NOTE]
> To synchronize multiple water surfaces as one, the size of each surface must be an integer multiple of the repetition size, which should also be a multiple of 10. For example, two 50 m x 50 m surfaces with a repetition size of 50 tiles correctly, but two 45 m x 45 m surfaces with a repetition size of 45 shows a seam.
