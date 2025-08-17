using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;


[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
public struct IntArrayFillJobParallel : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    [WriteOnly][NoAlias] public NativeArray<int> array;

    [WriteOnly][NoAlias] public int value;


    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public void Execute(int index)
    {
        array[index] = value;
    }
}