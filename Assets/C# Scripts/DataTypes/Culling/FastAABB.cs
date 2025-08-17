using Unity.Burst;
using Unity.Mathematics;



[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
public struct FastAABB
{
    public float3 center;
    public float3 extents;

    public FastAABB(float3 _center, float3 _extents)
    {
        center = _center;
        extents = _extents;
    }
}