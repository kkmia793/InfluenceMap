using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    public Tilemap fieldTilemap;
    public Tilemap[] obstacleTilemaps;
    public float moveSpeed = 2f;
    public int randomMoveRange = 4; 
    public float detectionRange = 5f; 
    public Transform[] players;

    private Vector2Int currentCell;
    private Vector2Int previousCell; 
    private Vector2Int targetCell; 
    private bool isMoving = false;
    
    private Animator enemyAnimator;
    
    
    void Awake()
    {
        enemyAnimator = GetComponent<Animator>();
    }
    void Start()
    {
        currentCell = WorldToCell(transform.position);
        previousCell = currentCell;
        SetNewRandomTarget();
    }

    void Update()
    {
        if (!isMoving)
        {
            Transform targetPlayer = FindClosestPlayer();

            if (targetPlayer != null)
            {
                targetCell = WorldToCell(targetPlayer.position);
            }
            else if (currentCell == targetCell)
            {
                SetNewRandomTarget();
            }

            Vector2Int nextCell = GetNextCellTowardsTarget(targetCell);
            if (nextCell != currentCell)
            {
                StartCoroutine(MoveToCell(nextCell));
            }
        }
    }

    Transform FindClosestPlayer()
    {
        float closestDistance = detectionRange;
        Transform closestPlayer = null;

        foreach (var player in players)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }

    void SetNewRandomTarget()
    {
        for (int attempt = 0; attempt < 10; attempt++) 
        {
            int randomX = Random.Range(-randomMoveRange, randomMoveRange + 1);
            int randomY = Random.Range(-randomMoveRange, randomMoveRange + 1);
            Vector2Int candidateTarget = currentCell + new Vector2Int(randomX, randomY);

            if (IsFieldCell(candidateTarget) && !IsObstacleCell(candidateTarget))
            {
                targetCell = candidateTarget;
                return;
            }
        }

        targetCell = currentCell; 
    }

    Vector2Int GetNextCellTowardsTarget(Vector2Int target)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int bestCell = currentCell;
        float bestDistance = Mathf.Infinity;

        foreach (var direction in directions)
        {
            Vector2Int nextCell = currentCell + direction;

            if (IsFieldCell(nextCell) && !IsObstacleCell(nextCell) && nextCell != previousCell)
            {
                float distance = Vector2.Distance((Vector2)nextCell, (Vector2)target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCell = nextCell;
                }
            }
        }

        return bestCell;
    }

    IEnumerator MoveToCell(Vector2Int nextCell)
    {
        isMoving = true;
        Vector3 targetPosition = CellToWorld(nextCell);
        
        Vector2 direction = (targetPosition - transform.position).normalized;
        
        enemyAnimator.SetFloat("InputX", direction.x);
        enemyAnimator.SetFloat("InputY", direction.y);
        enemyAnimator.SetBool("isMoving", true);

        while (Vector2.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        previousCell = currentCell;
        currentCell = nextCell;
        isMoving = false;
        
        enemyAnimator.SetBool("isMoving", false);
    }

    Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3Int cell = fieldTilemap.WorldToCell(worldPosition);
        return new Vector2Int(cell.x, cell.y);
    }

    Vector3 CellToWorld(Vector2Int cellPosition)
    {
        return fieldTilemap.GetCellCenterWorld(new Vector3Int(cellPosition.x, cellPosition.y, 0));
    }

    bool IsFieldCell(Vector2Int cellPosition)
    {
        Vector3Int gridCell = new Vector3Int(cellPosition.x, cellPosition.y, 0);
        return fieldTilemap.HasTile(gridCell);
    }

    bool IsObstacleCell(Vector2Int cellPosition)
    {
        Vector3Int gridCell = new Vector3Int(cellPosition.x, cellPosition.y, 0);

        foreach (var tilemap in obstacleTilemaps)
        {
            if (tilemap.HasTile(gridCell))
            {
                return true;
            }
        }

        return false;
    }
}