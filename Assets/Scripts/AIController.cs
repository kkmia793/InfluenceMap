using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AIController : MonoBehaviour
{
    public float moveSpeed = 2f; 
    private Vector2Int initialCell; 
    private Vector3 initialPosition; 

    private Vector2Int currentCell; 
    private Vector2Int targetCell;  
    private Vector2Int previousCell;
    
    private bool isMoving = false; 
    private bool isDamage = false;
    
    private Queue<Vector2Int> movementHistory; // 最近の移動履歴
    
    private int historyLimit = 4; // 移動履歴の記憶数
    private int itemCount = 0;

    private Animator aiAnimator; 

    void Start()
    {
        aiAnimator = GetComponent<Animator>(); 
        
        currentCell = InfluenceMap.Instance.WorldToCell(transform.position);
        targetCell = currentCell;
        previousCell = currentCell;
        initialCell = currentCell; 
        initialPosition = transform.position; 
        movementHistory = new Queue<Vector2Int>();

        itemCount = 0;
    }

    void Update()
    {
        if (isDamage == true)
        {
            return;
        }

        if (isMoving)
        {
            MoveTowardsTarget();
        }
        else
        {
            targetCell = FindNextCell();
            if (targetCell != currentCell)
            {
                isMoving = true;
            }
        }
    }

    Vector2Int FindNextCell()
    {
        InfluenceMap influenceMap = InfluenceMap.Instance;

        Vector2Int[] adjacentCells = new Vector2Int[]
        {
            currentCell + Vector2Int.up,
            currentCell + Vector2Int.down,
            currentCell + Vector2Int.left,
            currentCell + Vector2Int.right
        };

        Vector2Int bestCell = currentCell;
        float bestScore = float.MinValue;

        foreach (var cell in adjacentCells)
        {
            if (!IsCellMovable(cell)) continue;
            if (cell == previousCell) continue;
            if (IsInLoop(cell)) continue; 

            float score = influenceMap.GetCellScore(cell.x, cell.y);
            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        if (bestCell != currentCell)
        {
            UpdateMovementHistory(bestCell);
        }

        return bestCell;
    }

    bool IsCellMovable(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < InfluenceMap.Instance.gridWidth &&
               cell.y >= 0 && cell.y < InfluenceMap.Instance.gridHeight &&
               !InfluenceMap.Instance.IsObstacleCell(cell);
    }

    void MoveTowardsTarget()
    {
        Vector2 targetWorldPosition = InfluenceMap.Instance.CellToWorld(targetCell);
        Vector2 direction = (targetWorldPosition - (Vector2)transform.position).normalized;
        
        aiAnimator.SetFloat("InputX", direction.x);
        aiAnimator.SetFloat("InputY", direction.y);
        aiAnimator.SetBool("isMoving", true);

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            direction = new Vector2(Mathf.Sign(direction.x), 0);
        }
        else
        {
            direction = new Vector2(0, Mathf.Sign(direction.y));
        }

        transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;

        if (Vector2.Distance(transform.position, targetWorldPosition) < 0.1f)
        {
            transform.position = targetWorldPosition;
            previousCell = currentCell;
            currentCell = targetCell;
            isMoving = false;
            
            aiAnimator.SetBool("isMoving", false);
        }
    }

    void UpdateMovementHistory(Vector2Int newCell)
    {
        movementHistory.Enqueue(newCell);

        if (movementHistory.Count > historyLimit)
        {
            movementHistory.Dequeue();
        }
    }

    bool IsInLoop(Vector2Int cell)
    {
        int loopCount = 0;

        foreach (var historyCell in movementHistory)
        {
            if (historyCell == cell)
            {
                loopCount++;
            }
        }

        // ループ回数が一定以下なら許容
        return loopCount > 2;
    }
    
    /*
    private IEnumerator StopMovementForSeconds(float seconds)
    {
        isDamage = true; 
        aiAnimator.SetBool("isMoving", false); 
        yield return new WaitForSeconds(seconds); 
        isDamage = false;
    }
    */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            itemCount++;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateItemCount(itemCount);
            }
        }

        if (other.CompareTag("Enemy"))
        {
            ResetPosition(); 
        }
    }

    private void ResetPosition()
    {
        transform.position = initialPosition;
        
        currentCell = initialCell;
        targetCell = currentCell;
        previousCell = currentCell;
        
        isMoving = false;
        
        aiAnimator.SetBool("isMoving", false);
    }



}
