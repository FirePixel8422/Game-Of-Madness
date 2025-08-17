using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public struct FrustumCullingJobParallel : IJobParallelFor
{
    [ReadOnly][NoAlias] public Bounds meshBounds;
    [ReadOnly][NoAlias] public NativeArray<FastFrustumPlane> frustumPlanes;

    [ReadOnly][NoAlias] public NativeArray<Matrix4x4> matrices;
    [ReadOnly][NoAlias] public int startIndex;

    [NativeDisableParallelForRestriction]
    [WriteOnly][NoAlias] public NativeList<Matrix4x4>.ParallelWriter culledMatrices;



    public void Execute(int index)
    {
        float4x4 targetMatrix = matrices[startIndex + index];

        FastAABB transformedBounds = TransformBounds(meshBounds, targetMatrix);

        //if mesh with targetMatrix is visible in the frustum, add it to visibleMatrices
        if (IsAABBInsideFrustum(frustumPlanes, transformedBounds))
        {
            culledMatrices.AddNoResize(targetMatrix);
        }
    }

    
    private bool IsAABBInsideFrustum(NativeArray<FastFrustumPlane> planes, FastAABB bounds)
    {
        for (int i = 0; i < 6; i++)
        {
            float3 normal = planes[i].normal;
            float distance = planes[i].distance;

            // AABB extents
            float3 extents = bounds.extents;
            float3 center = bounds.center;

            float projectedCenter = math.dot(center, normal);
            float projectedExtents =
                math.abs(extents.x * normal.x) +
                math.abs(extents.y * normal.y) +
                math.abs(extents.z * normal.z);

            if (projectedCenter + projectedExtents < -distance)
                return false;
        }
        return true;
    }

    private FastAABB TransformBounds(Bounds localBounds, float4x4 matrix)
    {
        float3 extents = localBounds.extents;

        float3 axisX = math.mul(matrix, new float4(extents.x, 0f, 0f, 0f)).xyz;
        float3 axisY = math.mul(matrix, new float4(0f, extents.y, 0f, 0f)).xyz;
        float3 axisZ = math.mul(matrix, new float4(0f, 0f, extents.z, 0f)).xyz;

        float3 worldExtents = new float3(
            math.abs(axisX.x) + math.abs(axisY.x) + math.abs(axisZ.x),
            math.abs(axisX.y) + math.abs(axisY.y) + math.abs(axisZ.y),
            math.abs(axisX.z) + math.abs(axisY.z) + math.abs(axisZ.z)
        );

        float3 center = math.mul(matrix, new float4(localBounds.center.x, localBounds.center.y, localBounds.center.z, 1f)).xyz;

        return new FastAABB(center, worldExtents * 2f);
    }
}