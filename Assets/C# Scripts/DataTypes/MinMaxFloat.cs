


/// <summary>
/// A lightweight struct that holds a float min and float max.
/// </summary>
[System.Serializable]
public struct MinMaxFloat
{
    public float min;
    public float max;

    public MinMaxFloat(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}