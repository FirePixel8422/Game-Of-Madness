using UnityEngine;


public struct Matrix4X4IdMapper
{
    public int id;
    public Matrix4x4 matrix;


    public Matrix4X4IdMapper(Matrix4x4 _matrix, int _id)
    {
        id = _id;
        matrix = _matrix;
    }


    public void GetData(out Matrix4x4 _matrix, out int _id)
    {
        _matrix = matrix;
        _id = id;
    }
}