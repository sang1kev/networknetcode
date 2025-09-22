using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미로 데이터를 만들어 주는 순수 팩토리(Non-MonoBehaviour).
/// - DFS 스패닝 트리로 연결성(openRight/openUp) 생성
/// - 각 셀에 동일 크기의 방(RectInt) 계산 (cellSize - 2*wallThickness)
/// - 좌표는 (0,0) 기준의 '로컬' 좌표 (타일 단위)
/// </summary>
public static class MazeCreator
{
    public struct MazeData
    {
        public int cellsX, cellsY;
        public int cellSize;
        public int wallThickness;
        public int roomSize;              // cellSize - 2*wallThickness
        public bool[,] openRight;         // (x,y) -> (x+1,y) 연결
        public bool[,] openUp;            // (x,y) -> (x,y+1) 연결
        public RectInt[,] roomRects;      // 각 셀의 방 사각형(로컬 타일 좌표)
    }

    /// <summary>
    /// '균일 방 + 방 두께 복도' 미로 데이터 생성.
    /// </summary>
    public static MazeData GenerateUniformRoomsMaze(
        int cellsX, int cellsY,
        int cellSize, int wallThickness,
        System.Random rng = null)
    {
        if (rng == null) rng = new System.Random();

        wallThickness = Mathf.Max(1, wallThickness);
        int minRoom = 2;
        int minAllowedCell = minRoom + 2 * wallThickness;
        if (cellSize < minAllowedCell) cellSize = minAllowedCell;

        int roomSize = cellSize - 2 * wallThickness;

        // 1) DFS 스패닝 트리
        var openRight = new bool[cellsX, cellsY];
        var openUp = new bool[cellsX, cellsY];
        {
            var visited = new bool[cellsX, cellsY];
            var st = new Stack<Vector2Int>();
            var start = new Vector2Int(0, 0);
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

        // 2) 각 셀의 방 사각형(로컬 좌표)
        var roomRects = new RectInt[cellsX, cellsY];
        for (int cx = 0; cx < cellsX; cx++)
        {
            for (int cy = 0; cy < cellsY; cy++)
            {
                int cellOriginX = cx * cellSize;
                int cellOriginY = cy * cellSize;
                int startX = cellOriginX + wallThickness;
                int startY = cellOriginY + wallThickness;
                roomRects[cx, cy] = new RectInt(startX, startY, roomSize, roomSize);
            }
        }

        return new MazeData
        {
            cellsX = cellsX,
            cellsY = cellsY,
            cellSize = cellSize,
            wallThickness = wallThickness,
            roomSize = roomSize,
            openRight = openRight,
            openUp = openUp,
            roomRects = roomRects
        };
    }
}
