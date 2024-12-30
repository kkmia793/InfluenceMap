using UnityEngine;

public class Apple : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            AppleManager appleManager = Object.FindFirstObjectByType<AppleManager>();
            if (appleManager != null)
            {
                appleManager.RemoveApple(gameObject);
            }
        }
    }
}