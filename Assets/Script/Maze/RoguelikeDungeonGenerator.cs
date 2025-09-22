using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 랜덤 크기의 "큰 방 + 굵은 복도" 로그라이크 던전 생성기.
/// - 미로(DFS 스패닝 트리)는 '연결 뼈대'로만 사용
/// - 각 셀에 큰 방(랜덤 크기, 중앙 정렬) 생성
/// - 연결된 셀 사이에 폭이 있는 복도 생성(겹치면 그대로, 겹치지 않으면 L자 복도)
/// - 바닥 먼저 칠하고, 나머지를 벽으로 채움
/// - 맵 중심은 (0,0)
/// - 외곽은 벽으로 감쌈
/// </summary>
public class RoguelikeDungeonGenerator : MonoBehaviour
{
    [Header("그래프(셀) 크기 (홀수 권장)")]
    public int cellsX = 11;
    public int cellsY = 11;

    [Header("셀 하나의 타일 크기 (셀 폭/높이)")]
    public int cellSize = 16;

    [Header("벽 두께(셀 경계 여백)")]
    public int wallThickness = 2;      // 셀 경계에 남길 벽/여백 두께

    [Header("Tilemaps")]
    public Tilemap floorTilemap;       // 길(보라)
    public Tilemap wallTilemap;        // 벽(검정)

    [Header("Tiles")]
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase entranceTile;
    public TileBase exitTile;

    [Header("입구/출구 (셀 좌표 기준)")]
    public Vector2Int entranceCell = new Vector2Int(0, 0);
    public Vector2Int exitCell = new Vector2Int(10, 5);

    private System.Random rng;
    private bool[,] visited;
    private bool[,] openRight;
    private bool[,] openUp;

    private RectInt[,] roomRects;      // 각 셀의 방 영역(타일 좌표)
    private int roomSize;              // cellSize - 2*wallThickness

    void Start()
    {
        wallThickness = Mathf.Max(1, wallThickness);
        int minRoom = 2;
        int minAllowedCell = minRoom + 2 * wallThickness;
        if (cellSize < minAllowedCell) cellSize = minAllowedCell;

        roomSize = cellSize - 2 * wallThickness;

        rng = new System.Random();

        GenerateGraph();                 // 1) 연결 그래프(DFS)
        BuildRoomsAndOpenCorridors();    // 2) 동일 크기 방 + 방 두께 복도
        CarveGatewaysToEdge();           // 3) ? 입구/출구에서 맵 가장자리까지 길을 먼저 뚫음
        FloodWalls();                    // 4) 빈 곳 = 벽
        FrameOuterPerimeter();           // 5) 외곽 벽 (바닥은 덮지 않음)
        PlaceEntranceExit();             // 6) 입/출구 타일 표시
    }

    // ---------------- DFS 스패닝 트리 ----------------
    void GenerateGraph()
    {
        visited = new bool[cellsX, cellsY];
        openRight = new bool[cellsX, cellsY];
        openUp = new bool[cellsX, cellsY];

        Stack<Vector2Int> st = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(0, 0);
        st.Push(start);
        visited[start.x, start.y] = true;

        while (st.Count > 0)
        {
            var cur = st.Peek();
            var neighbors = new List<Vector2Int>();

            if (cur.x > 0 && !visited[cur.x - 1, cur.y]) neighbors.Add(new Vector2Int(cur.x - 1, cur.y));
            if (cur.x < cellsX - 1 && !visited[cur.x + 1, cur.y]) neighbors.Add(new Vector2Int(cur.x + 1, cur.y));
            if (cur.y > 0 && !visited[cur.x, cur.y - 1]) neighbors.Add(new Vector2Int(cur.x, cur.y - 1));
            if (cur.y < cellsY - 1 && !visited[cur.x, cur.y + 1]) neighbors.Add(new Vector2Int(cur.x, cur.y + 1));

            if (neighbors.Count == 0) { st.Pop(); continue; }

            var next = neighbors[rng.Next(neighbors.Count)];

            if (next.x == cur.x + 1 && next.y == cur.y) openRight[cur.x, cur.y] = true;
            else if (next.x == cur.x - 1 && next.y == cur.y) openRight[next.x, next.y] = true;
            else if (next.y == cur.y + 1 && next.x == cur.x) openUp[cur.x, cur.y] = true;
            else if (next.y == cur.y - 1 && next.x == cur.x) openUp[next.x, next.y] = true;

            visited[next.x, next.y] = true;
            st.Push(next);
        }
    }

    // ---- 동일 크기 방 + 방 두께 복도 (맵 중심 0,0) ----
    void BuildRoomsAndOpenCorridors()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        roomRects = new RectInt[cellsX, cellsY];

        int totalW = cellsX * cellSize;
        int totalH = cellsY * cellSize;
        int offX = totalW / 2;
        int offY = totalH / 2;

        // 방(모두 동일 크기) 채우기
        for (int cx = 0; cx < cellsX; cx++)
        {
            for (int cy = 0; cy < cellsY; cy++)
            {
                int cellOriginX = cx * cellSize - offX;
                int cellOriginY = cy * cellSize - offY;

                int startX = cellOriginX + wallThickness;
                int startY = cellOriginY + wallThickness;

                var rect = new RectInt(startX, startY, roomSize, roomSize);
                roomRects[cx, cy] = rect;

                FillRect(floorTilemap, rect, floorTile);
            }
        }

        // 연결된 이웃 사이 ‘벽 구간’만 방 두께로 개방(복도 폭 = roomSize)
        int gap = 2 * wallThickness;

        for (int cx = 0; cx < cellsX; cx++)
        {
            for (int cy = 0; cy < cellsY; cy++)
            {
                var a = roomRects[cx, cy];

                // 오른쪽
                if (cx < cellsX - 1 && openRight[cx, cy])
                {
                    var cor = new RectInt(a.xMax, a.yMin, gap, roomSize);
                    FillRect(floorTilemap, cor, floorTile);
                }
                // 위쪽
                if (cy < cellsY - 1 && openUp[cx, cy])
                {
                    var cor = new RectInt(a.xMin, a.yMax, roomSize, gap);
                    FillRect(floorTilemap, cor, floorTile);
                }
            }
        }
    }

    // ---- ? 입구/출구에서 바깥 테두리까지 길을 미리 뚫기 ----
    void CarveGatewaysToEdge()
    {
        // 맵 전체 타일 경계(외곽)
        int totalW = cellsX * cellSize;
        int totalH = cellsY * cellSize;
        int minX = -totalW / 2;
        int minY = -totalH / 2;
        int maxX = minX + totalW - 1;
        int maxY = minY + totalH - 1;

        // 두 지점에 대해 처리
        CarveOneGateway(entranceCell);
        CarveOneGateway(exitCell);

        void CarveOneGateway(Vector2Int cell)
        {
            cell.x = Mathf.Clamp(cell.x, 0, cellsX - 1);
            cell.y = Mathf.Clamp(cell.y, 0, cellsY - 1);

            var r = roomRects[cell.x, cell.y];

            // 방에서 가장 가까운 외곽을 찾아, 방 두께와 같은 폭으로 직선 통로를 뚫음
            int distLeft = r.xMin - minX;
            int distRight = maxX - r.xMax;
            int distBottom = r.yMin - minY;
            int distTop = maxY - r.yMax;

            int minDist = Mathf.Min(distLeft, distRight, distBottom, distTop);

            if (minDist == distLeft)
            {
                // 좌측 외곽까지
                var cor = new RectInt(minX, r.yMin, r.xMin - minX + 1, roomSize);
                FillRect(floorTilemap, cor, floorTile);
            }
            else if (minDist == distRight)
            {
                // 우측 외곽까지
                var cor = new RectInt(r.xMax, r.yMin, maxX - r.xMax + 1, roomSize);
                FillRect(floorTilemap, cor, floorTile);
            }
            else if (minDist == distBottom)
            {
                // 하단 외곽까지
                var cor = new RectInt(r.xMin, minY, roomSize, r.yMin - minY + 1);
                FillRect(floorTilemap, cor, floorTile);
            }
            else // Top
            {
                var cor = new RectInt(r.xMin, r.yMax, roomSize, maxY - r.yMax + 1);
                FillRect(floorTilemap, cor, floorTile);
            }
        }
    }

    // ---- 빈 곳을 벽으로 채우기 ----
    void FloodWalls()
    {
        int totalW = cellsX * cellSize;
        int totalH = cellsY * cellSize;
        var bounds = new BoundsInt(-totalW / 2, -totalH / 2, 0, totalW, totalH, 1);

        foreach (var p in bounds.allPositionsWithin)
        {
            if (floorTilemap.GetTile(p) == null)
                wallTilemap.SetTile(p, wallTile);
        }
    }

    // ---- 외곽 벽 프레임(바닥 있는 칸은 건드리지 않음) ----
    void FrameOuterPerimeter()
    {
        int totalW = cellsX * cellSize;
        int totalH = cellsY * cellSize;

        int minX = -totalW / 2;
        int minY = -totalH / 2;
        int maxX = minX + totalW - 1;
        int maxY = minY + totalH - 1;

        // 위/아래
        for (int x = minX; x <= maxX; x++)
        {
            var p1 = new Vector3Int(x, minY, 0);
            var p2 = new Vector3Int(x, maxY, 0);
            if (floorTilemap.GetTile(p1) == null) wallTilemap.SetTile(p1, wallTile); // ? 바닥이면 덮지 않음
            if (floorTilemap.GetTile(p2) == null) wallTilemap.SetTile(p2, wallTile);
        }
        // 좌/우
        for (int y = minY; y <= maxY; y++)
        {
            var p1 = new Vector3Int(minX, y, 0);
            var p2 = new Vector3Int(maxX, y, 0);
            if (floorTilemap.GetTile(p1) == null) wallTilemap.SetTile(p1, wallTile);
            if (floorTilemap.GetTile(p2) == null) wallTilemap.SetTile(p2, wallTile);
        }
    }

    // ---- 입구/출구 타일(바닥 위에 표시) ----
    void PlaceEntranceExit()
    {
        entranceCell.x = Mathf.Clamp(entranceCell.x, 0, cellsX - 1);
        entranceCell.y = Mathf.Clamp(entranceCell.y, 0, cellsY - 1);
        exitCell.x = Mathf.Clamp(exitCell.x, 0, cellsX - 1);
        exitCell.y = Mathf.Clamp(exitCell.y, 0, cellsY - 1);

        var eRect = roomRects[entranceCell.x, entranceCell.y];
        var xRect = roomRects[exitCell.x, exitCell.y];

        Vector3Int e = new Vector3Int(eRect.xMin + eRect.width / 2, eRect.yMin + eRect.height / 2, 0);
        Vector3Int ex = new Vector3Int(xRect.xMin + xRect.width / 2, xRect.yMin + xRect.height / 2, 0);

        floorTilemap.SetTile(e, entranceTile);
        floorTilemap.SetTile(ex, exitTile);
    }

    // ---- 유틸 ----
    void FillRect(Tilemap tm, RectInt r, TileBase tile)
    {
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
                tm.SetTile(new Vector3Int(x, y, 0), tile);
    }
}
