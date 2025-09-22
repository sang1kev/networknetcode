using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 3x3 월드 조립기 (중앙 1,1은 비워둠; 코너 4칸은 서브 미로; 엣지 중앙 4칸은 단순 커넥터)
/// - MonoBehaviour는 이 파일만 쓰면 됩니다.
/// - 미로 데이터는 MazeFactory(Non-MonoBehaviour)를 호출해 받아옵니다.
/// - 맵 중심은 (0,0)
/// </summary>
public class World3Creator : MonoBehaviour
{
    [Header("Sub-Maze(코너) 셀 그래프 크기 (홀수 권장)")]
    public int subCellsX = 11;
    public int subCellsY = 11;

    [Header("Sub-Maze 셀 하나의 타일 크기 & 벽 두께")]
    public int cellSize = 16;
    public int wallThickness = 2;

    [Header("Tilemaps")]
    public Tilemap floorTilemap;       // 길(보라)
    public Tilemap wallTilemap;        // 벽(검정)

    [Header("Tiles")]
    public TileBase floorTile;
    public TileBase wallTile;

    [Header("연결부 두께 (기본: 방 두께와 동일)")]
    public bool corridorSameAsRoom = true;
    public int connectorThicknessOverride = 12;

    // 내부 상태
    private System.Random rng;
    private int subW, subH;        // 한 개 서브맵의 타일 폭/높이
    private int worldW, worldH;    // 전체 맵 타일 폭/높이
    private int roomSize;          // 방 두께 (= cellSize - 2*wallThickness)

    void Start()
    {
        rng = new System.Random();

        // 서브맵 타일 폭/높이
        roomSize = Mathf.Max(2, cellSize - 2 * Mathf.Max(1, wallThickness));
        subW = subCellsX * cellSize;
        subH = subCellsY * cellSize;
        worldW = 3 * subW;
        worldH = 3 * subH;

        BuildWorld();
    }

    // ============== 메인 빌드 ==============
    void BuildWorld()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        // 코너 좌표 (슈퍼 그리드)
        var corners = new List<Vector2Int> {
            new Vector2Int(0,0), // 좌하
            new Vector2Int(2,0), // 우하
            new Vector2Int(0,2), // 좌상
            new Vector2Int(2,2)  // 우상
        };

        foreach (var corner in corners)
        {
            // 1) 서브 미로 데이터 생성 (팩토리 호출)
            var data = MazeCreator.GenerateUniformRoomsMaze(
                subCellsX, subCellsY, cellSize, wallThickness, rng);

            // 2) 서브 미로를 월드 타일맵에 스탬프
            StampSubMaze(data, corner);
        }

        // 3) 엣지 중앙 4칸 연결(간단한 굵은 복도)
        CarveEdgeConnectors();

        // 4) 빈 곳을 벽으로 채우고 외곽 프레임
        FloodWalls();
        FrameOuterPerimeter();
    }

    // ============== 서브 미로 스탬프 ==============
    void StampSubMaze(MazeCreator.MazeData data, Vector2Int superCell)
    {
        // 이 서브맵의 좌하 원점(월드 기준) ? 전체 맵 중심 (0,0)에 정렬
        int worldMinX = superCell.x * subW - worldW / 2;
        int worldMinY = superCell.y * subH - worldH / 2;

        // 1) 방 채우기
        for (int cx = 0; cx < data.cellsX; cx++)
        {
            for (int cy = 0; cy < data.cellsY; cy++)
            {
                var r = data.roomRects[cx, cy];                 // 로컬 좌표
                var wr = new RectInt(worldMinX + r.x, worldMinY + r.y, r.width, r.height);
                FillRect(floorTilemap, wr, floorTile);
            }
        }

        // 2) 방 두께 복도 (gap = 2*wallThickness)
        int gap = 2 * data.wallThickness;
        for (int cx = 0; cx < data.cellsX; cx++)
        {
            for (int cy = 0; cy < data.cellsY; cy++)
            {
                var a = data.roomRects[cx, cy];

                if (cx < data.cellsX - 1 && data.openRight[cx, cy])
                {
                    var cor = new RectInt(a.xMax, a.yMin, gap, data.roomSize);
                    var wr = new RectInt(worldMinX + cor.x, worldMinY + cor.y, cor.width, cor.height);
                    FillRect(floorTilemap, wr, floorTile);
                }
                if (cy < data.cellsY - 1 && data.openUp[cx, cy])
                {
                    var cor = new RectInt(a.xMin, a.yMax, data.roomSize, gap);
                    var wr = new RectInt(worldMinX + cor.x, worldMinY + cor.y, cor.width, cor.height);
                    FillRect(floorTilemap, wr, floorTile);
                }
            }
        }
    }

    // ============== 엣지 중앙 커넥터 ==============
    void CarveEdgeConnectors()
    {
        int thick = corridorSameAsRoom ? roomSize : Mathf.Max(1, connectorThicknessOverride);

        // (1,0): 하단 가로(좌하 ↔ 우하)
        CarveHorizontalConnector(new Vector2Int(0, 0), new Vector2Int(2, 0), thick);

        // (1,2): 상단 가로(좌상 ↔ 우상)
        CarveHorizontalConnector(new Vector2Int(0, 2), new Vector2Int(2, 2), thick);

        // (0,1): 좌측 세로(좌하 ↔ 좌상)
        CarveVerticalConnector(new Vector2Int(0, 0), new Vector2Int(0, 2), thick);

        // (2,1): 우측 세로(우하 ↔ 우상)
        CarveVerticalConnector(new Vector2Int(2, 0), new Vector2Int(2, 2), thick);
    }

    void CarveHorizontalConnector(Vector2Int leftCorner, Vector2Int rightCorner, int thickness)
    {
        int leftMinX = leftCorner.x * subW - worldW / 2;
        int leftMinY = leftCorner.y * subH - worldH / 2;
        int rightMinX = rightCorner.x * subW - worldW / 2;
        int rightMinY = rightCorner.y * subH - worldH / 2;

        // 같은 row라 Y 중앙이 동일
        int yCenter = leftMinY + subH / 2;
        int y = yCenter - thickness / 2;

        // 좌 서브맵의 오른쪽 방 영역에서 약간 파고 들어가 시작, 우 서브맵 왼쪽에서 종료
        int x0 = leftMinX + subW - roomSize / 2;
        int x1 = rightMinX + roomSize / 2;

        var cor = new RectInt(Mathf.Min(x0, x1), y, Mathf.Abs(x1 - x0), thickness);
        FillRect(floorTilemap, cor, floorTile);
    }

    void CarveVerticalConnector(Vector2Int bottomCorner, Vector2Int topCorner, int thickness)
    {
        int botMinX = bottomCorner.x * subW - worldW / 2;
        int botMinY = bottomCorner.y * subH - worldH / 2;
        int topMinX = topCorner.x * subW - worldW / 2;
        int topMinY = topCorner.y * subH - worldH / 2;

        int xCenter = botMinX + subW / 2;
        int x = xCenter - thickness / 2;

        int y0 = botMinY + subH - roomSize / 2;
        int y1 = topMinY + roomSize / 2;

        var cor = new RectInt(x, Mathf.Min(y0, y1), thickness, Mathf.Abs(y1 - y0));
        FillRect(floorTilemap, cor, floorTile);
    }

    // ============== 벽 채우기 & 외곽 프레임 ==============
    void FloodWalls()
    {
        var bounds = new BoundsInt(-worldW / 2, -worldH / 2, 0, worldW, worldH, 1);
        foreach (var p in bounds.allPositionsWithin)
        {
            if (floorTilemap.GetTile(p) == null)
                wallTilemap.SetTile(p, wallTile);
        }
    }

    void FrameOuterPerimeter()
    {
        int minX = -worldW / 2;
        int minY = -worldH / 2;
        int maxX = minX + worldW - 1;
        int maxY = minY + worldH - 1;

        for (int x = minX; x <= maxX; x++)
        {
            var p1 = new Vector3Int(x, minY, 0);
            var p2 = new Vector3Int(x, maxY, 0);
            if (floorTilemap.GetTile(p1) == null) wallTilemap.SetTile(p1, wallTile);
            if (floorTilemap.GetTile(p2) == null) wallTilemap.SetTile(p2, wallTile);
        }
        for (int y = minY; y <= maxY; y++)
        {
            var p1 = new Vector3Int(minX, y, 0);
            var p2 = new Vector3Int(maxX, y, 0);
            if (floorTilemap.GetTile(p1) == null) wallTilemap.SetTile(p1, wallTile);
            if (floorTilemap.GetTile(p2) == null) wallTilemap.SetTile(p2, wallTile);
        }
    }

    // ============== 유틸 ==============
    void FillRect(Tilemap tm, RectInt r, TileBase tile)
    {
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
                tm.SetTile(new Vector3Int(x, y, 0), tile);
    }
}

