using System;
using UnityEngine;

public class Fade : MonoBehaviour
{
    [SerializeField] private int reductionPercent = 50;
    private bool isActive = false;
    private bool canUse = true;
    public event Action OnFadeChanged;
    public bool IsActive
    {
        get
        {
            return isActive;
        }
    }
    public bool CanUse
    {
        get
        {
            return canUse;
        }
    }
    public int ReductionPercent
    {
        get
        {
            return reductionPercent;
        }
    }
    private void Awake()
    {
        ValidateValues();
    }
    public void SetReductionPercent(int newReductionPercent)
    {
        reductionPercent = newReductionPercent;
        ValidateValues();
    }
    private void ValidateValues()
    {
        if (reductionPercent < 0)
        {
            Debug.LogWarning(gameObject.name + " fade reduction can't be below 0, setting it to 0");
            reductionPercent = 0;
        }

        if (reductionPercent > 100)
        {
            Debug.LogWarning(gameObject.name + " fade reduction can't be above 100, setting it to 100");
            reductionPercent = 100;
        }
    }
    public bool TryUse()
    {
        if (!canUse)
        {
            Debug.Log(gameObject.name + " can't use Fade two turns in a row");
            return false;
        }
        isActive = true;
        canUse = false;
        Debug.Log(gameObject.name + " used Fade");
        OnFadeChanged?.Invoke();
        return true;
    }
    public int ReduceDamage(int damage)
    {
        if (!isActive || damage <= 0)
        {
            return damage;
        }
        int reducedDamage = damage * (100 - reductionPercent) / 100;
        Debug.Log(gameObject.name + " Fade reduced " + damage + " damage to " + reducedDamage);
        return reducedDamage;
    }
    public void EndEnemyPhase()
    {
        if (!isActive)
        {
            return;
        }
        isActive = false;
        OnFadeChanged?.Invoke();
    }
    public void OnOtherAbilityUsed()
    {
        if (canUse)
        {
            return;
        }
        canUse = true;
        OnFadeChanged?.Invoke();
    }
    public void ResetFade()
    {
        isActive = false;
        canUse = true;
        OnFadeChanged?.Invoke();
    }
    [ContextMenu("Test Use Fade")]
    private void TestUse()
    {
        TryUse();
    }
    [ContextMenu("Test Reduce 10 Damage")]
    private void TestReduce()
    {
        Debug.Log("10 damage becomes " + ReduceDamage(10));
    }
    [ContextMenu("Test End Enemy Phase")]
    private void TestEndPhase()
    {
        EndEnemyPhase();
    }
    [ContextMenu("Test Other Ability Used")]
    private void TestOtherAbility()
    {
        OnOtherAbilityUsed();
    }
}