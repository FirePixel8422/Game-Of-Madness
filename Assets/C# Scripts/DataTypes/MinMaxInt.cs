


/// <summary>
/// A lightweight struct that holds a float min and float max.
/// </summary>
[System.Serializable]
public struct MinMaxInt
{
    public int min;
    public int max;

    public MinMaxInt(int min, int max)
    {
        this.min = min;
        this.max = max;
    }
}