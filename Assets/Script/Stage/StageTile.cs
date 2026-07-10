/// <summary> ステージ1マスのデータ. タグ(種類)と角度(90度刻み)を持つ. </summary>
[System.Serializable]
public class StageTile
{
    /// <summary> マスの種類. </summary>
    public StageTileType Type;

    /// <summary> 回転 (0=0度, 1=90度, 2=180度, 3=270度). </summary>
    public int Rotation;

    /// <summary> Addressablesアドレス. </summary>
    public string ObjectAddress;

    public StageTile()
    {
        Type = StageTileType.None;
        Rotation = 0;
        ObjectAddress = "";
    }

    public StageTile(StageTileType type, int rotation = 0)
    {
        Type = type;
        Rotation = rotation & 3;
        ObjectAddress = "";
    }
}
