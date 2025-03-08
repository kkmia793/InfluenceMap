using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class InfluenceMap : MonoBehaviour
{
    public Grid grid;
    public Tilemap fieldTilemap; 
    public Tilemap[] obstacleTilemaps; 
    
    public float cellSize = 1.0f;
    
    public float maxThreatDistance = 10f;
    public float maxAttractionDistance = 5f;
    
    public float threatWeight = 0.5f;
    public float itemWeight = 0.5f;

    public enum EmotionalState { Neutral, Fearful, Joyful }
    public EmotionalState currentEmotion = EmotionalState.Neutral;

    public int gridWidth { get; private set; }
    public int gridHeight { get; private set; }
    private Vector3Int gridOrigin;
    
    private readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private float[,] threatMap;
    private float[,] itemMap;
    private float[,] combinedMap;
    private SpriteRenderer[,] cellSprites;

    public Transform[] ghosts;
    public AppleManager appleManager;
    public GameObject cellPrefab;

    public static InfluenceMap Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeGridBounds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeMaps();
        InitializeCells();
    }
    
    void InitializeGridBounds()
    {
        if (fieldTilemap != null)
        {
            BoundsInt fieldBounds = fieldTilemap.cellBounds;
            gridOrigin = fieldBounds.min;
            gridWidth = fieldBounds.size.x;
            gridHeight = fieldBounds.size.y;
        }
    }

    void InitializeMaps()
    {
        threatMap = new float[gridWidth, gridHeight];
        itemMap = new float[gridWidth, gridHeight];
        combinedMap = new float[gridWidth, gridHeight];
        cellSprites = new SpriteRenderer[gridWidth, gridHeight];
    }

    void InitializeCells()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 cellWorldPosition = CellToWorld(new Vector2Int(x, y));
                GameObject cell = Instantiate(cellPrefab, cellWorldPosition, Quaternion.identity, transform);
                cell.transform.localScale = Vector3.one * cellSize;

                SpriteRenderer spriteRenderer = cell.GetComponent<SpriteRenderer>();
                cellSprites[x, y] = spriteRenderer;
            }
        }
    }

    public void UpdateInfluenceMap()
    {
        UpdateThreatMap();
        UpdateItemMap();
        CombineMaps();
        UpdateCellColors();
    }

    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        Vector3Int cell = grid.WorldToCell(worldPosition);
        return new Vector2Int(cell.x - gridOrigin.x, cell.y - gridOrigin.y);
    }

    public Vector2 CellToWorld(Vector2Int cellPosition)
    {
        Vector3Int cell = new Vector3Int(cellPosition.x + gridOrigin.x, cellPosition.y + gridOrigin.y, 0);
        return grid.CellToWorld(cell) + new Vector3(cellSize / 2, cellSize / 2, 0);
    }
    
    public bool IsObstacleCell(Vector2Int cellPosition)
    {
        Vector3Int gridCell = new Vector3Int(cellPosition.x + gridOrigin.x, cellPosition.y + gridOrigin.y, 0);
        
        foreach (var tilemap in obstacleTilemaps)
        {
            if (tilemap.HasTile(gridCell))
            {
                return true;
            }
        }
        
        Vector2 worldPosition = CellToWorld(cellPosition);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, cellSize / 2);

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Object")) // 障害物タグ
            {
                return true;
            }
        }

        return false;
    }

    /* private void UpdateThreatMap()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cellPosition = new Vector2Int(x, y);
                if (IsObstacleCell(cellPosition))
                {
                    threatMap[x, y] = 0;
                    continue;
                }

                threatMap[x, y] = CalculateThreat(CellToWorld(cellPosition));
            }
        }
    } */
    
    private void UpdateThreatMap()
    {
        float[,] distanceMap = new float[gridWidth, gridHeight];
        bool[,] visited = new bool[gridWidth, gridHeight];
        PriorityQueue<Vector2Int> priorityQueue = new PriorityQueue<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                distanceMap[x, y] = float.MaxValue;
                threatMap[x, y] = 0;
            }
        }

        foreach (var ghost in ghosts)
        {
            if (ghost == null) continue;

            Vector2Int gridPosition = WorldToCell(ghost.position);
            if (IsValidGridPosition(gridPosition))
            {
                distanceMap[gridPosition.x, gridPosition.y] = 0;
                priorityQueue.Enqueue(gridPosition, 0);
            }
        }
        
        // ダイクストラ法

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();
            int x = current.x;
            int y = current.y;

            if (visited[x, y]) continue;
            visited[x, y] = true;

            float currentDistance = distanceMap[x, y];
            float threat = Mathf.Max(0, maxThreatDistance - currentDistance);
            threatMap[x, y] = threat;

            foreach (var direction in directions)
            {
                Vector2Int neighbor = new Vector2Int(x + direction.x, y + direction.y);

                if (IsValidGridPosition(neighbor) && !visited[neighbor.x, neighbor.y] && !IsObstacleCell(neighbor))
                {
                    float newDistance = currentDistance + cellSize;
                    if (newDistance < distanceMap[neighbor.x, neighbor.y])
                    {
                        distanceMap[neighbor.x, neighbor.y] = newDistance;
                        priorityQueue.Enqueue(neighbor, newDistance);
                    }
                }
            }
        }
    }

    // グリッド内かどうか
    private bool IsValidGridPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridWidth &&
               position.y >= 0 && position.y < gridHeight;
    }
    
    
    

    /*
    private void UpdateItemMap()
    {
        List<GameObject> apples = appleManager.GetActiveApples();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cellPosition = new Vector2Int(x, y);
                if (IsObstacleCell(cellPosition))
                {
                    itemMap[x, y] = 0;
                    continue;
                }

                itemMap[x, y] = CalculateAttractiveness(CellToWorld(cellPosition), apples);
            }
        }
    }
    */
    
    private void UpdateItemMap()
    {
        List<GameObject> apples = appleManager.GetActiveApples();
        float[,] distanceMap = new float[gridWidth, gridHeight];
        bool[,] visited = new bool[gridWidth, gridHeight];
        PriorityQueue<Vector2Int> priorityQueue = new PriorityQueue<Vector2Int>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                distanceMap[x, y] = float.MaxValue;
                itemMap[x, y] = 0;
            }
        }
        
        foreach (var apple in apples)
        {
            if (apple == null) continue;

            Vector2Int gridPosition = WorldToCell(apple.transform.position);
            if (IsValidGridPosition(gridPosition))
            {
                distanceMap[gridPosition.x, gridPosition.y] = 0;
                priorityQueue.Enqueue(gridPosition, 0);
            }
        }

        // ダイクストラ法
        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();
            int x = current.x;
            int y = current.y;

            if (visited[x, y]) continue;
            visited[x, y] = true;

            float currentDistance = distanceMap[x, y];
            float attractiveness = Mathf.Max(0, maxAttractionDistance - currentDistance);
            itemMap[x, y] = attractiveness;

            foreach (var direction in directions)
            {
                Vector2Int neighbor = new Vector2Int(x + direction.x, y + direction.y);

                if (IsValidGridPosition(neighbor) && !visited[neighbor.x, neighbor.y] && !IsObstacleCell(neighbor))
                {
                    float newDistance = currentDistance + cellSize;
                    if (newDistance < distanceMap[neighbor.x, neighbor.y])
                    {
                        distanceMap[neighbor.x, neighbor.y] = newDistance;
                        priorityQueue.Enqueue(neighbor, newDistance);
                    }
                }
            }
        }
    }
    
    
    
    private void UpdateEmotionWeights()
    {
        switch (currentEmotion)
        {
            case EmotionalState.Fearful:
                threatWeight = 0.8f;
                itemWeight = 0.2f;
                break;
            case EmotionalState.Joyful:
                threatWeight = 0.2f;
                itemWeight = 0.8f;
                break;
            default: // 中立
                threatWeight = 0.5f;
                itemWeight = 0.5f;
                break;
        }
    }
    

    private void CombineMaps()
    {
        float minScore = float.MaxValue;
        float maxScore = float.MinValue;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // 敵の脅威度マップ（反転させたもの）とアイテムの狙いやすさマップを積和合成
                combinedMap[x, y] = -threatWeight * threatMap[x, y] + itemWeight * itemMap[x, y];

                if (combinedMap[x, y] > maxScore) maxScore = combinedMap[x, y];
                if (combinedMap[x, y] < minScore) minScore = combinedMap[x, y];
            }
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!IsObstacleCell(new Vector2Int(x, y)))
                {
                    combinedMap[x, y] = (combinedMap[x, y] - minScore) / (maxScore - minScore);
                }
            }
        }
    }

    private void UpdateCellColors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float score = combinedMap[x, y];

                Color color;
                if (score < 0.5f)
                {
                    color = Color.Lerp(Color.red, Color.green, score / 0.5f);
                }
                else
                {
                    color = Color.Lerp(Color.green, Color.blue, (score - 0.5f) / 0.5f);
                }

                cellSprites[x, y].color = color;
            }
        }
    }
    
    /* 元々のやり方（敵の脅威度の計算）
    private float CalculateThreat(Vector2 cellWorldPosition)
    {
        float totalThreat = 0;

        foreach (var ghost in ghosts)
        {
            if (ghost == null) continue;

            float distance = Vector2.Distance(cellWorldPosition, ghost.position);
            if (distance <= maxThreatDistance)
            {
                totalThreat += Mathf.Max(0, maxThreatDistance - distance);
            }
        }
        return totalThreat;
    }
    */
    

    /* 元々のやり方（アイテムの狙いやすさの計算）
    private float CalculateAttractiveness(Vector2 cellWorldPosition, List<GameObject> apples)
    {
        float totalAttractiveness = 0;

        foreach (var apple in apples)
        {
            if (apple == null) continue;

            float distance = Vector2.Distance(cellWorldPosition, apple.transform.position);
            if (distance <= maxAttractionDistance)
            {
                totalAttractiveness += Mathf.Max(0, maxAttractionDistance - distance);
            }
        }
        return totalAttractiveness;
    }
    */

    public float GetCellScore(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            return combinedMap[x, y];
        }
        else
        {
            return 0;
        }
    }
}