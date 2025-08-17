using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


[BurstCompile]
public struct UpdateCellCycleJobParallelBatched : IJobParallelForBatch
{
    [NativeDisableParallelForRestriction]
    [ReadOnly][NoAlias] public NativeArray<byte> cellStates;

    [ReadOnly][NoAlias] public NativeArray<MinMaxInt> birthRange;
    [ReadOnly][NoAlias] public NativeArray<MinMaxInt> survivalRange;

    [ReadOnly][NoAlias] public int3 gridSize;
    [ReadOnly][NoAlias] public bool wrapGrid;

    [NativeDisableParallelForRestriction]
    [WriteOnly] [NoAlias] public NativeArray<CellData> newCellStateData;


    [BurstCompile]
    public void Execute(int startIndex, int count)
    {
        int cIndex = startIndex;

        for (int i = 0; i < count; i++, cIndex++)
        {
            newCellStateData[cIndex] = GetNextCellStateData(cIndex, wrapGrid);
        }
    }

    [BurstCompile]
    private CellData GetNextCellStateData(int cellId, bool wrap)
    {
        // Convert 1D index → 3D coordinates
        int3 pos = new int3(
            cellId % gridSize.x,
            (cellId / gridSize.x) % gridSize.y,
            cellId / (gridSize.x * gridSize.y)
        );

        byte isAlive = cellStates[cellId];
        int aliveNeighbors = 0;

        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                for (int zOffset = -1; zOffset <= 1; zOffset++)
                {
                    if (xOffset == 0 && yOffset == 0 && zOffset == 0) continue; // skip self

                    int xPos = pos.x + xOffset;
                    int yPos = pos.y + yOffset;
                    int zPos = pos.z + zOffset;

                    if (wrap)
                    {
                        xPos = (xPos + gridSize.x) % gridSize.x;
                        yPos = (yPos + gridSize.y) % gridSize.y;
                        zPos = (zPos + gridSize.z) % gridSize.z;
                    }
                    else if (xPos < 0 || xPos >= gridSize.x || 
                             yPos < 0 || yPos >= gridSize.y ||
                             zPos < 0 || zPos >= gridSize.z)
                    {
                            continue; // out of bounds → skip
                    }

                    int neighborId = xPos + yPos * gridSize.x + zPos * gridSize.x * gridSize.y;

                    // If to check neighbour is alive
                    if (cellStates[neighborId] == 1)
                        aliveNeighbors++;
                }
            }
        }

        // If Cell is now alive
        if (isAlive == 1)
        {
            for (int i = 0; i < survivalRange.Length; i++)
            {
                if (aliveNeighbors >= survivalRange[i].min && aliveNeighbors <= survivalRange[i].max)
                {
                    return new CellData(false, true); // Cell survives
                }
            }
            return new CellData(true, false); // Cell dies
        }
        else
        {
            for (int i = 0; i < birthRange.Length; i++)
            {
                if (aliveNeighbors >= birthRange[i].min && aliveNeighbors <= birthRange[i].max)
                {
                    return new CellData(true, true); // Cell is born
                }
            }
            return new CellData(false, false); // Cell remains dead
        }
    }
}