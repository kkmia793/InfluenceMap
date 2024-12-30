using System.Collections.Generic;
using UnityEngine;

public class AppleManager : MonoBehaviour
{
    public GameObject applePrefab;
    public int maxApples = 6;
    public float cellSize = 1.0f;

    private List<GameObject> activeApples = new List<GameObject>();

    void Start()
    {
        GenerateApples(maxApples);
    }

    void Update()
    {
        if (activeApples.Count < maxApples)
        {
            GenerateApples(maxApples - activeApples.Count);
        }
    }

    void GenerateApples(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2Int cellPosition;
            do
            {
                int x = Random.Range(0, InfluenceMap.Instance.gridWidth);
                int y = Random.Range(0, InfluenceMap.Instance.gridHeight);
                cellPosition = new Vector2Int(x, y);
            } 
            while (InfluenceMap.Instance.IsObstacleCell(cellPosition)); 
            
            Vector2 position = InfluenceMap.Instance.CellToWorld(cellPosition);
            
            GameObject apple = Instantiate(applePrefab, position, Quaternion.identity);
            activeApples.Add(apple);
        }
    }

    public void RemoveApple(GameObject apple)
    {
        activeApples.Remove(apple);
        Destroy(apple);
    }

    public List<GameObject> GetActiveApples()
    {
        return activeApples;
    }
}