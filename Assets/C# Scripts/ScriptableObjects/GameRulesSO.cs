using Unity.Mathematics;
using UnityEngine;



[CreateAssetMenu(fileName = "GameRules", menuName = "ScriptableObjects/GameRulesSO", order = 1)]
public class GameRulesSO : ScriptableObject
{
    [Header("Grid Settings")]
    public int3 gridSize = 25;
    public bool wrapGrid = true;

    [Range(0, 100)]
    public float initialCellAliveChance = 10;

    public uint seed;
    public bool reSeedOnStart = true;

    public MinMaxInt[] birthRange = new MinMaxInt[] { new MinMaxInt(13, 26) };
    public MinMaxInt[] survivalRange = new MinMaxInt[] { new MinMaxInt(14, 19) };
}
