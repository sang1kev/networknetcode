using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGen : MonoBehaviour
{
    [Header("Maze Size (홀수 권장)")]
    public int width = 21;
    public int height = 21;

    [Header("Tilemap & RuleTiles")]
    public Tilemap tilemap;          
    public RuleTile wallTile;        
    public RuleTile floorTile;       
    public RuleTile entranceTile;    
    public RuleTile exitTile;        

    [Header("Entrance & Exit (Grid 좌표)")]
    public Vector2Int entrancePos;   
    public Vector2Int exitPos;       

    private int[,] maze;

    public int floorWidth = 2;

    void Start()
    {
        GenerateMaze();
        DrawMaze();
    }

    void GenerateMaze()
    {
        // 0 = wall, 1 = path
        maze = new int[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                maze[x, y] = 0;

        // Maze using DFS 
        Vector2Int start = new Vector2Int(1, 1);
        maze[start.x, start.y] = 1;
        CarvePath(start);

        entrancePos = new Vector2Int(width / 2, 0);
        exitPos = new Vector2Int(width - 1, height / 2);

        // Prevent entrance or exit to be generated double
        maze[entrancePos.x, entrancePos.y] = 1;
        if (entrancePos.y + 1 < height)
        {
            maze[entrancePos.x, entrancePos.y + 1] = 1;
        }

        maze[exitPos.x, exitPos.y] = 1;
        if (exitPos.x - 1 >= 0)
        {
            maze[exitPos.x - 1, exitPos.y] = 1;
        }
        
        // Making path to exit
        EnsurePath(entrancePos, exitPos);
    }

    void CarvePath(Vector2Int pos)
    {
        // Suffle DFS dir
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        System.Random rand = new System.Random();
        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2Int tmp = dirs[i];
            int r = rand.Next(i, dirs.Length);
            dirs[i] = dirs[r];
            dirs[r] = tmp;
        }

        foreach (var dir in dirs)
        {
            Vector2Int next = pos + dir * 2;
            if (next.x > 0 && next.x < width - 1 && next.y > 0 && next.y < height - 1)
            {
                if (maze[next.x, next.y] == 0)
                {
                    maze[pos.x + dir.x, pos.y + dir.y] = 1;
                    maze[next.x, next.y] = 1;
                    CarvePath(next);
                }
            }
        }
    }

    void EnsurePath(Vector2Int start, Vector2Int goal)
    {
        // find path entrance to exit
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(start);
        parent[start] = start;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        bool found = false;

        while (queue.Count > 0 && !found)
        {
            Vector2Int cur = queue.Dequeue();
            foreach (var dir in dirs)
            {
                Vector2Int next = cur + dir;
                if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height) continue;
                if (!parent.ContainsKey(next))
                {
                    parent[next] = cur;
                    if (maze[next.x, next.y] == 1)
                        queue.Enqueue(next);
                    if (next == goal)
                    {
                        found = true;
                        break;
                    }
                }
            }
        }

        // No path then make path
        if (!found)
        {
            Vector2Int cur = goal;
            while (cur != start)
            {
                maze[cur.x, cur.y] = 1;
                cur = parent.ContainsKey(cur) ? parent[cur] : start;
            }
        }
    }

    void DrawMaze()
    {
        tilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if ((x == exitPos.x && y == exitPos.y) || (x == entrancePos.x && y == entrancePos.y))
                    continue;
                if (maze[x, y] == 1)
                {
                    for (int dx = 0; dx < floorWidth; dx++)
                    {
                        for (int dy = 0; dy < floorWidth; dy++)
                        {
                            Vector3Int pos = new Vector3Int(x + dx, y + dy, 0);
                            tilemap.SetTile(pos, floorTile);
                        }
                    }
                }
                else
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(pos, wallTile);
                }
            }
        }

        // 입구/출구 타일
        tilemap.SetTile(new Vector3Int(entrancePos.x, entrancePos.y, 0), entranceTile);
        tilemap.SetTile(new Vector3Int(exitPos.x, exitPos.y, 0), exitTile);
    }
}
