using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class StressTestAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var mode = SimulationMode.getCurrentMode();

        switch (mode.type)
        {
            case SimulationMode.ModeType.Color:
                Entities.WithAll<ColorAnimated>()
                    .ForEach((ref MaterialColor color, in SpawnIndex index) =>
                    {
                        float indexAdd = 0.0123f * (float) index.Value;
                        var delta = new float3(math.cos(mode.time + indexAdd), math.sin(mode.time + indexAdd), 0) *
                                    mode.deltaTime;

                        color.Value = color.Value + float4(delta.x, delta.y, delta.z, 0);
                    }).ScheduleParallel();
                break;
            case SimulationMode.ModeType.Position:
                Entities.WithAll<ColorAnimated, LocalToWorld>()
                    .ForEach((ref Translation translation, in SpawnIndex index) =>
                    {
                        float indexAdd = 0.00123f * (float) index.Value;

                        int y = index.Value / index.Height;
                        int x = index.Value - (y * index.Height);
                        var pos = new float4(x, math.sin(mode.time + indexAdd) * 4.0f, y, 1);

                        translation.Value = pos.xyz;
                    }).ScheduleParallel();
                break;
            case SimulationMode.ModeType.PositionAndColor:
                Entities.WithAll<ColorAnimated, LocalToWorld>()
                    .ForEach((ref MaterialColor color, ref Translation translation, in SpawnIndex index) =>
                    {
                        float indexAdd = 0.005f * (float) index.Value;

                        var delta = new float3(math.cos(mode.time + indexAdd), math.sin(mode.time + indexAdd), 0) *
                                    mode.deltaTime;
                        color.Value = color.Value + float4(delta.x, delta.y, delta.z, 0);

                        int y = index.Value / index.Height;
                        int x = index.Value - (y * index.Height);
                        var pos = new float4(x, math.sin(mode.time + indexAdd) * 4.0f, y, 1);

                        translation.Value = pos.xyz;
                    }).ScheduleParallel();
                break;
            default:
                break;
        }
    }
}
