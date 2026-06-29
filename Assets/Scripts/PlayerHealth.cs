using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public bool IsAlive => currentHealth > 0;
    [SerializeField] private int currentHealth = 100;

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if(currentHealth < 0)
        {
            currentHealth = 0;
            
            // Player dead: 
        }
    }
}