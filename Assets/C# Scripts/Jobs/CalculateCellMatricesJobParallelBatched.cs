using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


[BurstCompile]
public struct CalculateCellMatricesJobParallelBatched : IJobParallelForBatch
{
    [WriteOnly][NoAlias] public NativeArray<byte> cellStates;
    [ReadOnly][NoAlias] public NativeArray<CellData> newCellStateData;

    [WriteOnly][NoAlias] public NativeList<Matrix4X4IdMapper>.ParallelWriter matrixIdMappers;

    [WriteOnly][NoAlias] public NativeList<int>.ParallelWriter cellIdsForRemoval;

    [ReadOnly][NoAlias] public int3 gridSize;


    [BurstCompile]
    public void Execute(int startIndex, int count)
    {
        int cIndex = startIndex;

        for (int i = 0; i < count; i++, cIndex++)
        {
            // If current cell didnt change, skip it
            if (newCellStateData[cIndex].IsValueChanged(out bool newState) == false) continue;

            cellStates[cIndex] = newState.AsByteBool();

            if (newState == true)
            {
                int x = cIndex % gridSize.x;
                int y = (cIndex / gridSize.x) % gridSize.y;
                int z = cIndex / (gridSize.x * gridSize.y);

                float3 offset = new float3(
                    -0.5f * gridSize.x + 0.5f,
                    -0.5f * gridSize.y + 0.5f,
                    -0.5f * gridSize.z + 0.5f
                );

                float3 worldPos = new float3(x, y, z) + offset;

                matrixIdMappers.AddNoResize(new Matrix4X4IdMapper(float4x4.TRS(worldPos, quaternion.identity, new float3(0.5f)), cIndex));
            }
            else
            {
                cellIdsForRemoval.AddNoResize(cIndex);
            }
        }
    }
}
