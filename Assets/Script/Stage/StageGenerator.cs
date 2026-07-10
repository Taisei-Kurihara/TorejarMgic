using System.Collections.Generic;
using UnityEngine;

/// <summary> ローグ的ダンジョンの自動生成. 部屋+通路をタグ+角度で配置. </summary>
public static class StageGenerator
{
    /// <summary> 部屋の矩形定義. </summary>
    private struct RoomRect
    {
        public int X, Z, Width, Height;
        public Vector2Int Center => new Vector2Int(X + Width / 2, Z + Height / 2);
    }

    /// <summary> 通路が長いと判定する閾値 (マンハッタン距離). </summary>
    private const int LongCorridorThreshold = 6;

    /// <summary> 追加接続の距離上限. </summary>
    private const float ExtraConnectionMaxDist = 12f;

    /// <summary> ステージ生成. </summary>
    /// <param name="gridSize">グリッドのマス数.</param>
    /// <param name="tileSize">1マスのUnity上サイズ (meter).</param>
    /// <param name="roomCount">生成する部屋数 (0=自動).</param>
    public static StageData Generate(Vector2Int gridSize, float tileSize, int roomCount = 0)
    {
        var data = new StageData(gridSize, tileSize);

        if (roomCount <= 0)
        {
            roomCount = Mathf.Max(2, (gridSize.x * gridSize.y) / 30);
        }

        // 1. 部屋を仮配置 (位置のみ決定).
        var rooms = PlaceRooms(data, roomCount);

        if (rooms.Count < 2)
        {
            // 部屋が少ない場合はそのまま書き込んで終了.
            ClearGrid(data);
            for (int i = 0; i < rooms.Count; i++) CarveRoom(data, rooms[i]);
            AssignStartGoal(data, rooms);
            AssignObjectAddresses(data);
            Debug.Log($"StageGenerator: 生成完了 ({gridSize.x}x{gridSize.y}, 部屋数={rooms.Count})");
            return data;
        }

        // 2. MST接続リストを構築 (通路はまだ掘らない).
        var connections = BuildMSTConnections(rooms);

        // 3. 長い直線通路を短縮するため部屋をずらす.
        CompactLongCorridors(rooms, connections);

        // 4. グリッドをクリアして全部屋を再書き込み.
        ClearGrid(data);
        for (int i = 0; i < rooms.Count; i++) CarveRoom(data, rooms[i]);

        // 5. MST通路を掘る.
        foreach (var conn in connections)
        {
            CarveCorridor(data, rooms[conn.x].Center, rooms[conn.y].Center);
        }

        // 6. 追加接続 (円環ループ + 接近した部屋同士).
        AddExtraConnections(data, rooms, connections);

        // 7. 通路タグを再判定.
        ClassifyCorridorTiles(data);

        // 8. スタート/ゴール.
        AssignStartGoal(data, rooms);

        // 9. Addressablesアドレス名を設定.
        AssignObjectAddresses(data);

        Debug.Log($"StageGenerator: 生成完了 ({gridSize.x}x{gridSize.y}, 部屋数={rooms.Count}, 接続数={connections.Count})");
        return data;
    }

    // ================================================================
    // 部屋配置
    // ================================================================

    /// <summary> 部屋をランダム配置. 重複チェック付き. </summary>
    private static List<RoomRect> PlaceRooms(StageData data, int roomCount)
    {
        var rooms = new List<RoomRect>();
        int maxAttempts = roomCount * 20;

        for (int attempt = 0; attempt < maxAttempts && rooms.Count < roomCount; attempt++)
        {
            int w = Random.Range(3, Mathf.Min(8, data.GridSize.x - 2));
            int h = Random.Range(3, Mathf.Min(8, data.GridSize.y - 2));
            int x = Random.Range(1, data.GridSize.x - w - 1);
            int z = Random.Range(1, data.GridSize.y - h - 1);

            var room = new RoomRect { X = x, Z = z, Width = w, Height = h };

            if (IsRoomOverlapping(rooms, room, -1)) continue;

            rooms.Add(room);
        }

        return rooms;
    }

    /// <summary> 部屋同士の重複チェック (1マス余白付き). ignoreIndex で自身を除外. </summary>
    private static bool IsRoomOverlapping(List<RoomRect> existing, RoomRect newRoom, int ignoreIndex)
    {
        for (int i = 0; i < existing.Count; i++)
        {
            if (i == ignoreIndex) continue;
            var r = existing[i];
            if (newRoom.X - 1 < r.X + r.Width + 1 &&
                newRoom.X + newRoom.Width + 1 > r.X - 1 &&
                newRoom.Z - 1 < r.Z + r.Height + 1 &&
                newRoom.Z + newRoom.Height + 1 > r.Z - 1)
            {
                return true;
            }
        }
        return false;
    }

    // ================================================================
    // MST接続
    // ================================================================

    /// <summary> MST (最小全域木) で接続リストを構築. Vector2Int(from, to). </summary>
    private static List<Vector2Int> BuildMSTConnections(List<RoomRect> rooms)
    {
        var connections = new List<Vector2Int>();
        if (rooms.Count < 2) return connections;

        var connected = new HashSet<int> { 0 };
        var remaining = new HashSet<int>();
        for (int i = 1; i < rooms.Count; i++) remaining.Add(i);

        while (remaining.Count > 0)
        {
            int bestFrom = -1, bestTo = -1;
            float bestDist = float.MaxValue;

            foreach (int c in connected)
            foreach (int r in remaining)
            {
                float dist = Vector2Int.Distance(rooms[c].Center, rooms[r].Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestFrom = c;
                    bestTo = r;
                }
            }

            if (bestTo < 0) break;

            connections.Add(new Vector2Int(bestFrom, bestTo));
            connected.Add(bestTo);
            remaining.Remove(bestTo);
        }

        return connections;
    }

    // ================================================================
    // 長通路の短縮 (部屋ずらし)
    // ================================================================

    /// <summary> 長い通路を持つ接続の部屋をずらして短縮. </summary>
    private static void CompactLongCorridors(List<RoomRect> rooms, List<Vector2Int> connections)
    {
        for (int iter = 0; iter < 3; iter++)
        {
            bool anyMoved = false;

            for (int ci = 0; ci < connections.Count; ci++)
            {
                var conn = connections[ci];
                var roomA = rooms[conn.x];
                var roomB = rooms[conn.y];

                int manhattan = Mathf.Abs(roomA.Center.x - roomB.Center.x)
                              + Mathf.Abs(roomA.Center.y - roomB.Center.y);

                if (manhattan < LongCorridorThreshold) continue;

                // roomB を roomA 方向にずらす.
                var shifted = TryShiftRoom(rooms, conn.y, roomA.Center);
                if (shifted.HasValue)
                {
                    rooms[conn.y] = shifted.Value;
                    anyMoved = true;
                }
            }

            if (!anyMoved) break;
        }
    }

    /// <summary> 部屋を target 方向にずらす. 重複しない最大移動量を返す. </summary>
    private static RoomRect? TryShiftRoom(List<RoomRect> rooms, int roomIndex, Vector2Int target)
    {
        var room = rooms[roomIndex];
        var center = room.Center;

        int dx = target.x - center.x;
        int dz = target.y - center.y;

        // 半分ずらす.
        int shiftX = dx / 2;
        int shiftZ = dz / 2;

        if (shiftX == 0 && shiftZ == 0) return null;

        // 段階的に試行 (大きい移動→小さい移動).
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var candidate = new RoomRect
            {
                X = room.X + shiftX,
                Z = room.Z + shiftZ,
                Width = room.Width,
                Height = room.Height
            };

            // 範囲チェック.
            if (candidate.X < 1 || candidate.Z < 1
                || candidate.X + candidate.Width >= rooms[0].Width + rooms[0].X + 100 // 簡易上限.
                || candidate.Z + candidate.Height >= rooms[0].Height + rooms[0].Z + 100)
            {
                shiftX /= 2;
                shiftZ /= 2;
                if (shiftX == 0 && shiftZ == 0) return null;
                continue;
            }

            if (!IsRoomOverlapping(rooms, candidate, roomIndex))
            {
                return candidate;
            }

            // 重複したら移動量を縮小.
            shiftX /= 2;
            shiftZ /= 2;
            if (shiftX == 0 && shiftZ == 0) return null;
        }

        return null;
    }

    // ================================================================
    // 追加接続 (円環ループ)
    // ================================================================

    /// <summary> MST以外の追加接続を生成. 円環ループを作る. </summary>
    private static void AddExtraConnections(StageData data, List<RoomRect> rooms, List<Vector2Int> connections)
    {
        if (rooms.Count < 3) return;

        // 各部屋の接続数をカウント.
        var connectionCount = new int[rooms.Count];
        foreach (var conn in connections)
        {
            connectionCount[conn.x]++;
            connectionCount[conn.y]++;
        }

        // 既存接続をセットで管理 (重複防止).
        var existingPairs = new HashSet<long>();
        foreach (var conn in connections)
        {
            int a = Mathf.Min(conn.x, conn.y);
            int b = Mathf.Max(conn.x, conn.y);
            existingPairs.Add((long)a * 10000 + b);
        }

        // 接近した未接続部屋ペアを候補として収集.
        var candidates = new List<(int from, int to, float dist)>();
        for (int i = 0; i < rooms.Count; i++)
        for (int j = i + 1; j < rooms.Count; j++)
        {
            int a = Mathf.Min(i, j);
            int b = Mathf.Max(i, j);
            if (existingPairs.Contains((long)a * 10000 + b)) continue;

            float dist = Vector2Int.Distance(rooms[i].Center, rooms[j].Center);
            if (dist <= ExtraConnectionMaxDist)
            {
                candidates.Add((i, j, dist));
            }
        }

        // 距離順にソート.
        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        // 接続数が少ない部屋を優先して追加接続.
        foreach (var (from, to, dist) in candidates)
        {
            // 少なくとも片方の部屋が接続数3未満なら追加.
            if (connectionCount[from] >= 3 && connectionCount[to] >= 3) continue;

            CarveCorridor(data, rooms[from].Center, rooms[to].Center);
            connections.Add(new Vector2Int(from, to));
            connectionCount[from]++;
            connectionCount[to]++;

            int a = Mathf.Min(from, to);
            int b = Mathf.Max(from, to);
            existingPairs.Add((long)a * 10000 + b);
        }
    }

    // ================================================================
    // 部屋の書き込み
    // ================================================================

    /// <summary> グリッド全体をNoneにクリア. </summary>
    private static void ClearGrid(StageData data)
    {
        for (int x = 0; x < data.GridSize.x; x++)
        for (int z = 0; z < data.GridSize.y; z++)
        {
            data.SetTile(x, z, new StageTile());
        }
    }

    /// <summary> 部屋をグリッドに書き込み. 壁/角/床を設定. </summary>
    private static void CarveRoom(StageData data, RoomRect room)
    {
        for (int x = room.X; x < room.X + room.Width; x++)
        for (int z = room.Z; z < room.Z + room.Height; z++)
        {
            if (!data.IsInBounds(x, z)) continue;

            bool isLeft   = (x == room.X);
            bool isRight  = (x == room.X + room.Width - 1);
            bool isBottom = (z == room.Z);
            bool isTop    = (z == room.Z + room.Height - 1);

            if ((isLeft || isRight) && (isBottom || isTop))
            {
                int rot = GetCornerRotation(isLeft, isRight, isBottom, isTop);
                data.SetTile(x, z, new StageTile(StageTileType.RoomCorner, rot));
            }
            else if (isLeft || isRight || isBottom || isTop)
            {
                int rot = GetWallRotation(isLeft, isRight, isBottom, isTop);
                data.SetTile(x, z, new StageTile(StageTileType.RoomWall, rot));
            }
            else
            {
                data.SetTile(x, z, new StageTile(StageTileType.RoomFloor, 0));
            }
        }
    }

    private static int GetCornerRotation(bool left, bool right, bool bottom, bool top)
    {
        if (left  && bottom) return 0;
        if (right && bottom) return 1;
        if (right && top)    return 2;
        if (left  && top)    return 3;
        return 0;
    }

    private static int GetWallRotation(bool left, bool right, bool bottom, bool top)
    {
        if (bottom) return 0;
        if (right)  return 1;
        if (top)    return 2;
        if (left)   return 3;
        return 0;
    }

    // ================================================================
    // 通路の書き込み
    // ================================================================

    /// <summary> L字通路を掘る (X方向→Z方向). </summary>
    private static void CarveCorridor(StageData data, Vector2Int from, Vector2Int to)
    {
        int x = from.x;
        int z = from.y;

        int dx = (to.x > x) ? 1 : -1;
        while (x != to.x)
        {
            TrySetCorridor(data, x, z);
            x += dx;
        }

        int dz = (to.y > z) ? 1 : -1;
        while (z != to.y)
        {
            TrySetCorridor(data, x, z);
            z += dz;
        }

        TrySetCorridor(data, x, z);
    }

    private static void TrySetCorridor(StageData data, int x, int z)
    {
        if (!data.IsInBounds(x, z)) return;

        var tile = data.GetTile(x, z);
        if (tile == null) return;

        switch (tile.Type)
        {
            case StageTileType.None:
                data.SetTile(x, z, new StageTile(StageTileType.CorridorStraight, 0));
                break;

            case StageTileType.RoomWall:
            case StageTileType.RoomCorner:
                tile.Type = StageTileType.RoomDoorWall;
                break;
        }
    }

    // ================================================================
    // 通路タグ判定
    // ================================================================

    /// <summary> 全通路マスの隣接状態からタグ(種類)と角度を再判定. </summary>
    private static void ClassifyCorridorTiles(StageData data)
    {
        for (int x = 0; x < data.GridSize.x; x++)
        for (int z = 0; z < data.GridSize.y; z++)
        {
            var tile = data.GetTile(x, z);
            if (tile == null) continue;
            if (!IsCorridorLike(tile.Type)) continue;

            bool north = IsConnectable(data, x, z + 1);
            bool south = IsConnectable(data, x, z - 1);
            bool east  = IsConnectable(data, x + 1, z);
            bool west  = IsConnectable(data, x - 1, z);
            int count = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);

            if (count == 4)
            {
                tile.Type = StageTileType.CorridorCross;
                tile.Rotation = 0;
            }
            else if (count == 3)
            {
                tile.Type = StageTileType.CorridorT;
                if (!north) tile.Rotation = 0;
                else if (!east) tile.Rotation = 1;
                else if (!south) tile.Rotation = 2;
                else tile.Rotation = 3;
            }
            else if (count == 2)
            {
                if ((north && south) || (east && west))
                {
                    tile.Type = StageTileType.CorridorStraight;
                    tile.Rotation = (north && south) ? 0 : 1;
                }
                else
                {
                    tile.Type = StageTileType.CorridorL;
                    if (north && east)  tile.Rotation = 0;
                    else if (east && south) tile.Rotation = 1;
                    else if (south && west) tile.Rotation = 2;
                    else tile.Rotation = 3;
                }
            }
            else if (count == 1)
            {
                tile.Type = StageTileType.CorridorDeadEnd;
                if (north) tile.Rotation = 0;
                else if (east)  tile.Rotation = 1;
                else if (south) tile.Rotation = 2;
                else tile.Rotation = 3;
            }
        }
    }

    private static bool IsCorridorLike(StageTileType type)
    {
        return type == StageTileType.CorridorStraight
            || type == StageTileType.CorridorL
            || type == StageTileType.CorridorT
            || type == StageTileType.CorridorCross
            || type == StageTileType.CorridorDeadEnd;
    }

    private static bool IsConnectable(StageData data, int x, int z)
    {
        if (!data.IsInBounds(x, z)) return false;
        return data.IsWalkable(x, z);
    }

    // ================================================================
    // ユーティリティ
    // ================================================================

    private static void AssignStartGoal(StageData data, List<RoomRect> rooms)
    {
        if (rooms.Count >= 2)
        {
            data.StartPos = rooms[0].Center;
            data.GoalPos = rooms[rooms.Count - 1].Center;
        }
        else if (rooms.Count == 1)
        {
            data.StartPos = rooms[0].Center;
            data.GoalPos = rooms[0].Center;
        }
    }

    private static void AssignObjectAddresses(StageData data)
    {
        for (int x = 0; x < data.GridSize.x; x++)
        for (int z = 0; z < data.GridSize.y; z++)
        {
            var tile = data.GetTile(x, z);
            if (tile == null) continue;
            tile.ObjectAddress = GetAddressForType(tile.Type);
        }
    }

    private static string GetAddressForType(StageTileType type)
    {
        return type switch
        {
            StageTileType.RoomFloor        => StageAddressNames.RoomFloor,
            StageTileType.RoomWall         => StageAddressNames.RoomWall,
            StageTileType.RoomCorner       => StageAddressNames.RoomCorner,
            StageTileType.RoomDoorWall     => StageAddressNames.RoomDoorWall,
            StageTileType.CorridorStraight => StageAddressNames.CorridorStraight,
            StageTileType.CorridorL        => StageAddressNames.CorridorL,
            StageTileType.CorridorT        => StageAddressNames.CorridorT,
            StageTileType.CorridorCross    => StageAddressNames.CorridorCross,
            StageTileType.CorridorDeadEnd  => StageAddressNames.CorridorDeadEnd,
            _ => ""
        };
    }
}

/// <summary> ステージ用 Addressables アドレス名定数. </summary>
public static class StageAddressNames
{
    public const string RoomFloor        = "Stage_RoomFloor";
    public const string RoomWall         = "Stage_RoomWall";
    public const string RoomCorner       = "Stage_RoomCorner";
    public const string RoomDoorWall     = "Stage_RoomDoorWall";
    public const string CorridorStraight = "Stage_CorridorStraight";
    public const string CorridorL        = "Stage_CorridorL";
    public const string CorridorT        = "Stage_CorridorT";
    public const string CorridorCross    = "Stage_CorridorCross";
    public const string CorridorDeadEnd  = "Stage_CorridorDeadEnd";
}
