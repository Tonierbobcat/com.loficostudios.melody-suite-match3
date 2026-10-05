using UnityEngine;

public class TileObject : MonoBehaviour
{
    
    public int Row;
    public int Column;
    
    public void Init(int row, int column)
    {
        Row = row;
        Column = column;
    }
}
