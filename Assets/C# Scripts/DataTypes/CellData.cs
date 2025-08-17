using System;


[Serializable]
public struct CellData
{
    private byte _value;

    public bool Changed
    {
        readonly get => (_value & 1) != 0;
        set
        {
            if (value) _value |= 1;
            else _value &= 0b11111110;
        }
    }
    
    public bool State
    {
        readonly get => (_value & 2) != 0;
        set
        {
            if (value) _value |= 2;
            else _value &= 0b11111101;
        }
    }

    public bool IsValueChanged(out bool newState)
    {
        if (Changed)
        {
            newState = State;
            return true;
        }

        newState = false;
        return false;
    }

    public CellData(bool changed, bool state)
    {
        _value = 0;
        if (changed) _value |= 1;
        if (state) _value |= 2;
    }
}
