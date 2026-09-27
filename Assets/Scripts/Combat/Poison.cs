using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Poison : MonoBehaviour
{
    [SerializeField] private int maxStacks = 5;
    [SerializeField] private int damagePerStack = 2;

    private int poisonStacks = 0;
    private Health health;

    public event Action<int> OnPoisonChanged;
    public int PoisonStacks
    {
        get
        {
            return poisonStacks;
        }
    }
    public int MaxStacks
    {
        get
        {
            return maxStacks;
        }
    }
    private void Awake()
    {
        health = GetComponent<Health>();
        ValidateValues();
    }
    public void SetValues(int newMaxStacks, int newDamagePerStack)
    {
        maxStacks = newMaxStacks;
        damagePerStack = newDamagePerStack;
        health = health = GetComponent<Health>();
        ValidateValues();
        poisonStacks = 0;
    }
    private void ValidateValues()
    {
        if(maxStacks <= 0)
        {
            Debug.LogWarning(gameObject.name + " max stacks must be above 0, settings it to 1");
            maxStacks = 1;
        }
         if(damagePerStack <= 0)
        {
            Debug.LogWarning(gameObject.name + " damage per stack must be above 0, setting it to 1");
            damagePerStack = 1;
        }

    }
    public void AddStacks(int stackAmount)
    {
        if (stackAmount <= 0 || health.IsDead)
        {
            return;
        }
        poisonStacks += stackAmount;
        if (poisonStacks > maxStacks)
        {
            poisonStacks = maxStacks;
        }
        Debug.Log(gameObject.name + " poison stacks: " + poisonStacks + "/" + maxStacks);
        OnPoisonChanged?.Invoke(poisonStacks);
    }
    public void Tick()
    {
        if (poisonStacks <= 0 || health.IsDead)
        {
            return;
        }
        int poisonDamage = poisonStacks * damagePerStack;
        Debug.Log(gameObject.name + " takes " + poisonDamage + " poison damage");
        health.TakeDamage(poisonDamage);
    }

    public int ConsumeAllStacks()
    {
        int consumedStacks = poisonStacks;
        poisonStacks = 0;
        Debug.Log(gameObject.name + " poison consumed: " + consumedStacks + " stacks");
        OnPoisonChanged?.Invoke(poisonStacks);
        return consumedStacks;
    }
    [ContextMenu("Test Add 2 Stacks")]
    private void TestAddStacks()
    {
        AddStacks(2);
    }
    [ContextMenu("Test Poison Tick")]
    private void TestTick()
    {
        Tick();
    }
    [ContextMenu("Test Consume Stacks")]
    private void TestConsume()
    {
        ConsumeAllStacks();
    }
}