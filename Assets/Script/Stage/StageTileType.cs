/// <summary> ステージマスの種類 (タグ). </summary>
public enum StageTileType
{
    /// <summary> 未使用/空マス. </summary>
    None,
    /// <summary> 部屋の中 (壁を持たない部分). </summary>
    RoomFloor,
    /// <summary> 部屋の壁. </summary>
    RoomWall,
    /// <summary> 部屋の角. </summary>
    RoomCorner,
    /// <summary> 通路に接続される部屋の壁. </summary>
    RoomDoorWall,
    /// <summary> まっすぐの通路. </summary>
    CorridorStraight,
    /// <summary> L字の通路. </summary>
    CorridorL,
    /// <summary> T字の通路. </summary>
    CorridorT,
    /// <summary> 十字の通路. </summary>
    CorridorCross,
    /// <summary> 行き止まりの通路 (使用予定なし、タグのみ). </summary>
    CorridorDeadEnd
}
