using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public InfluenceMap influenceMap; // 影響マップ
    public UIManager uiManager; 

    private float timeLimit = 30f; // 制限時間（秒）
    private bool isGameOver = false; 
    
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (uiManager != null)
        {
            uiManager.UpdateTimerDisplay(timeLimit);
        }
    }

    void Update()
    {
        if (!isGameOver)
        {
            UpdateTimer();
        }

        influenceMap.UpdateInfluenceMap(); 
    }

    private void UpdateTimer()
    {
        timeLimit -= Time.deltaTime;
        
        if (uiManager != null)
        {
            uiManager.UpdateTimerDisplay(timeLimit);
        }
        
        if (timeLimit <= 1 && !isGameOver)
        {
            isGameOver = true;
            HandleGameOver();
        }
    }
    
    public void UpdateItemCount(int itemCount)
    {
        if (uiManager != null)
        {
            uiManager.UpdateItemCountDisplay(itemCount);
        }
    }

    private void HandleGameOver()
    {
        Time.timeScale = 0;
    }
}