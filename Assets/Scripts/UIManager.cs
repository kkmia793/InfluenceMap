using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text timerText; 
    public Text itemCountText;
    
    public void UpdateTimerDisplay(float timeRemaining)
    {
        if (timerText != null)
        {
            int seconds = Mathf.FloorToInt(timeRemaining);
            timerText.text = seconds.ToString();
        }
    }
    
    public void UpdateItemCountDisplay(int itemCount)
    {
        if (itemCountText != null)
        {
            itemCountText.text = $"{itemCount}";
        }
    }
}