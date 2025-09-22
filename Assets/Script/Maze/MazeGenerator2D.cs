using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class MazeGenerator2D : MonoBehaviour
{
    [Header("Graph(미로) 크기 - 셀 단위 (홀수 권장)")]
    public int cellsX = 9;   // 가로 셀 수
    public int cellsY = 9;   // 세로 셀 수

    [Header("셀 타일 폭(Stride) - 1개의 셀이 차지하는 타일 폭/높이")]
    public int cellSize = 12;         // 셀의 전체 폭/높이(타일 수). roomSizeMax + 여백 + 복도 너비보다 충분히 커야 함.

    [Header("방 크기 (타일 단위, 랜덤 범위)")]
    public int roomSizeMin = 6;       // 각 셀 안 방의 최소 크기
    public int roomSizeMax = 10;      // 각 셀 안 방의 최대 크기 (roomSizeMax <= cellSize - 2*roomMargin 추천)
    public int roomMargin = 1;        // 방과 셀 경계 사이 여백(벽 두께용)

    [Header("복도 두께 (타일)")]
    public int corridorWidth = 2;     // 방-방을 연결하는 통로 두께

    [Header("Tilemaps (Grid 아래 두 개 생성해서 연결)")]
    public Tilemap floorTilemap;      // 바닥 전용
    public Tilemap wallTilemap;       // 벽 전용

    [Header("Tiles")]
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase entranceTile;
    public TileBase exitTile;

    [Header("입구/출구 (셀 좌표, Inspector에서 조정 가능)")]
    public Vector2Int entranceCell = new Vector2Int(0, 0);       // 기본: 왼쪽 아래
    public Vector2Int exitCell = new Vector2Int(8, 4);       // 기본: 오른쪽 가운데(예시)

    // ---- 내부 상태 ----
    private System.Random rng;
    private bool[,] visited;               // DFS 방문
    private bool[,] openRight;             // (x,y) -> (x+1,y)로 연결되어 있으면 true
    private bool[,] openUp;                // (x,y) -> (x,y+1)로 연결되어 있으면 true

    // 방 정보(월드 타일 좌표계)
    private RectInt[,] roomRects;          // 각 셀에 배치된 방의 실제 사각형(타일 좌표)
    private Vector2Int[,] roomCenters;     // 각 셀 방의 중심(타일 좌표)

    void Start()
    {
        // 파라미터 보정
        if (cellsX < 2) cellsX = 2;
        if (cellsY < 2) cellsY = 2;
        if (roomSizeMin < 2) roomSizeMin = 2;
        if (roomSizeMax < roomSizeMin) roomSizeMax = roomSizeMin;
        if (corridorWidth < 1) corridorWidth = 1;
        if (roomMargin < 0) roomMargin = 0;

        // roomSizeMax가 cellSize를 넘지 않도록
        int maxAllowed = Mathf.Max(2, cellSize - 2 * roomMargin);
        roomSizeMax = Mathf.Clamp(roomSizeMax, roomSizeMin, maxAllowed);

        rng = new System.Random();

        GenerateGraph();           // DFS로 연결 그래프 생성
        BuildRoomsAndCorridors();  // 방/복도 배치(바닥 칠하기)
        FloodWalls();              // 바닥이 아닌 곳을 벽으로 채우기
        PlaceEntranceExit();       // 입구/출구 타일
    }

    // ============================================
    // 1) DFS로 스패닝 트리(그래프) 만들기
    // ============================================
    void GenerateGraph()
    {
        visited = new bool[cellsX, cellsY];
        openRight = new bool[cellsX, cellsY];    // 크기는 [cellsX-1, cellsY]만 써짐
        openUp = new bool[cellsX, cellsY];    // 크기는 [cellsX, cellsY-1]만 써짐

        Stack<Vector2Int> st = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(0, 0);
        st.Push(start);
        visited[start.x, start.y] = true;

        while (st.Count > 0)
        {
            var cur = st.Peek();
            var neighbors = new List<Vector2Int>();

            // 후보 이웃
            if (cur.x > 0 && !visited[cur.x - 1, cur.y]) neighbors.Add(new Vector2Int(cur.x - 1, cur.y));
            if (cur.x < cellsX - 1 && !visited[cur.x + 1, cur.y]) neighbors.Add(new Vector2Int(cur.x + 1, cur.y));
            if (cur.y > 0 && !visited[cur.x, cur.y - 1]) neighbors.Add(new Vector2Int(cur.x, cur.y - 1));
            if (cur.y < cellsY - 1 && !visited[cur.x, cur.y + 1]) neighbors.Add(new Vector2Int(cur.x, cur.y + 1));

            if (neighbors.Count == 0)
            {
                st.Pop();
                continue;
            }

            var next = neighbors[rng.Next(neighbors.Count)];

            // 연결 표시
            if (next.x == cur.x + 1 && next.y == cur.y) openRight[cur.x, cur.y] = true;
            else if (next.x == cur.x - 1 && next.y == cur.y) openRight[next.x, next.y] = true; // (= cur's left)
            else if (next.y == cur.y + 1 && next.x == cur.x) openUp[cur.x, cur.y] = true;
            else if (next.y == cur.y - 1 && next.x == cur.x) openUp[next.x, next.y] = true;     // (= cur's down)

            visited[next.x, next.y] = true;
            st.Push(next);
        }
    }

    // ============================================
    // 2) 방/복도 배치 (바닥 칠하기)  ?? 맵 중심 (0,0)
    // ============================================
    void BuildRoomsAndCorridors()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        roomRects = new RectInt[cellsX, cellsY];
        roomCenters = new Vector2Int[cellsX, cellsY];

        // 전체 맵 타일 폭/높이
        int totalW = cellsX * cellSize;
        int totalH = cellsY * cellSize;
        int offX = totalW / 2;   // 맵 중심이 (0,0)이 되도록 오프셋
        int offY = totalH / 2;

        // 2-1) 각 셀에 방 생성(랜덤 크기, 중앙 정렬)
        for (int cx = 0; cx < cellsX; cx++)
        {
            for (int cy = 0; cy < cellsY; cy++)
            {
                // 셀 영역(타일 좌표)
                int cellOriginX = cx * cellSize - offX;
                int cellOriginY = cy * cellSize - offY;

                int roomW = rng.Next(roomSizeMin, roomSizeMax + 1);
                int roomH = rng.Next(roomSizeMin, roomSizeMax + 1);

                // 셀 안에서 중앙 정렬 + 여백 유지
                roomW = Mathf.Min(roomW, cellSize - 2 * roomMargin);
                roomH = Mathf.Min(roomH, cellSize - 2 * roomMargin);

                int startX = cellOriginX + (cellSize - roomW) / 2;
                int startY = cellOriginY + (cellSize - roomH) / 2;

                var rect = new RectInt(startX, startY, roomW, roomH);
                roomRects[cx, cy] = rect;
                roomCenters[cx, cy] = new Vector2Int(startX + roomW / 2, startY + roomH / 2);

                // 바닥 타일로 방 채우기
                FillRect(floorTilemap, rect, floorTile);
            }
        }

        // 2-2) 인접한 셀(그래프 연결)에 복도 깔기
        for (int cx = 0; cx < cellsX; cx++)
        {
            for (int cy = 0; cy < cellsY; cy++)
            {
                // 오른쪽 이웃과 연결?
                if (cx < cellsX - 1 && openRight[cx, cy])
                {
                    var a = roomRects[cx, cy];
                    var b = roomRects[cx + 1, cy];

                    // 수평 복도: A의 오른쪽 ~ B의 왼쪽
                    int x0 = a.xMax;
                    int x1 = b.xMin;
                    // 두 방의 세로 영역 겹치는 부분 중심에 복도 위치
                    int overlapMin = Mathf.Max(a.yMin, b.yMin);
                    int overlapMax = Mathf.Min(a.yMax, b.yMax);
                    int y = (overlapMin <= overlapMax)
                          ? (overlapMin + overlapMax) / 2
                          : (roomCenters[cx, cy].y + roomCenters[cx + 1, cy].y) / 2; // 겹침 없으면 센터 라인

                    var cor = MakeCorridorRectHorizontal(x0, x1, y, corridorWidth);
                    FillRect(floorTilemap, cor, floorTile);
                }

                // 위쪽 이웃과 연결?
                if (cy < cellsY - 1 && openUp[cx, cy])
                {
                    var a = roomRects[cx, cy];
                    var b = roomRects[cx, cy + 1];

                    // 수직 복도: A의 위 ~ B의 아래
                    int y0 = a.yMax;
                    int y1 = b.yMin;

                    // 두 방의 가로 영역 겹치는 부분 중심에 복도 위치
                    int overlapMin = Mathf.Max(a.xMin, b.xMin);
                    int overlapMax = Mathf.Min(a.xMax, b.xMax);
                    int x = (overlapMin <= overlapMax)
                          ? (overlapMin + overlapMax) / 2
                          : (roomCenters[cx, cy].x + roomCenters[cx, cy + 1].x) / 2;

                    var cor = MakeCorridorRectVertical(y0, y1, x, corridorWidth);
                    FillRect(floorTilemap, cor, floorTile);
                }
            }
        }
    }

    // 수평 복도 사각형 생성
    RectInt MakeCorridorRectHorizontal(int x0, int x1, int yCenter, int width)
    {
        if (x1 < x0) { int t = x0; x0 = x1; x1 = t; }
        int half = Mathf.Max(1, width / 2);
        return new RectInt(x0, yCenter - half, Mathf.Max(1, x1 - x0 + 1), Mathf.Max(1, width));
    }

    // 수직 복도 사각형 생성
    RectInt MakeCorridorRectVertical(int y0, int y1, int xCenter, int width)
    {
        if (y1 < y0) { int t = y0; y0 = y1; y1 = t; }
        int half = Mathf.Max(1, width / 2);
        return new RectInt(xCenter - half, y0, Mathf.Max(1, width), Mathf.Max(1, y1 - y0 + 1));
    }

    // Rect 영역 타일 채우기
    void FillRect(Tilemap tm, RectInt r, TileBase tile)
    {
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
                tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    // ============================================
    // 3) 빈 곳을 벽으로 채우기 (벽은 얇게)
    // ============================================
    void FloodWalls()
    {
        var bounds = floorTilemap.cellBounds;
        // 혹은 전체 맵 범위를 정확히 지정하고 싶으면:
        // int totalW = cellsX * cellSize; int totalH = cellsY * cellSize;
        // bounds = new BoundsInt(-totalW/2, -totalH/2, 0, totalW, totalH, 1);

        foreach (var p in bounds.allPositionsWithin)
        {
            if (floorTilemap.GetTile(p) == null)
                wallTilemap.SetTile(p, wallTile);
        }
    }

    // ============================================
    // 4) 입구/출구 배치 (셀 좌표 → 방 중앙 근처에 표시)
    // ============================================
    void PlaceEntranceExit()
    {
        entranceCell.x = Mathf.Clamp(entranceCell.x, 0, cellsX - 1);
        entranceCell.y = Mathf.Clamp(entranceCell.y, 0, cellsY - 1);
        exitCell.x = Mathf.Clamp(exitCell.x, 0, cellsX - 1);
        exitCell.y = Mathf.Clamp(exitCell.y, 0, cellsY - 1);

        var eCenter = roomCenters[entranceCell.x, entranceCell.y];
        var xCenter = roomCenters[exitCell.x, exitCell.y];

        floorTilemap.SetTile(new Vector3Int(eCenter.x, eCenter.y, 0), entranceTile);
        floorTilemap.SetTile(new Vector3Int(xCenter.x, xCenter.y, 0), exitTile);
    }
}
