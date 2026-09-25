using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;
    private bool isDead = false;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    public int CurrentHealth
    {
        get
        {
            return currentHealth;
        }
    }
    public int MaxHealth
    {
        get
        {
            return maxHealth;
        }
    }
    public bool IsDead
    {
        get
        {
            return isDead;
        }
    }
    private void Awake()
    {
        currentHealth = maxHealth;
    }
    public void TakeDamage(int damageAmount)
    {
        if (isDead || damageAmount <= 0)
        {
            return;
        }

        currentHealth -= damageAmount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        Debug.Log(gameObject.name + " took " + damageAmount + " damage. HP: " + currentHealth + "/" + maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            isDead = true;
            Debug.Log(gameObject.name + " died");
            OnDied?.Invoke();
        }
    }
    public void Heal(int healAmount)
    {
        if (isDead || healAmount <= 0)
        {
            return;
        }

        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log(gameObject.name + " healed "+healAmount + ". HP: " + currentHealth+ "/" + maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    [ContextMenu("Test Take 10 Damage")]
    private void TestDamage()
    {
        TakeDamage(10);
    }
    [ContextMenu("Test Heal 5")]
    private void TestHeal()
    {
        Heal(5);
    }
}