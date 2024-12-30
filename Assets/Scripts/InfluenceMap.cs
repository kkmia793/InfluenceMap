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
    
    public float threatWeight = 0.4f;
    public float itemWeight = 0.6f;

    public int gridWidth { get; private set; }
    public int gridHeight { get; private set; }
    private Vector3Int gridOrigin;

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

    private void UpdateThreatMap()
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
    }

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

    // 敵の脅威度
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

    // アイテムの狙いやすさ
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