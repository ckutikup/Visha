using UnityEngine;
public class SerpentsBite : MonoBehaviour
{
    [SerializeField] private int damagePerStack = 4;
    [SerializeField] private GameObject testTarget;
    public int DamagePerStack
    {
        get
        {
            return damagePerStack;
        }
    }
    private void Awake()
    {
        ValidateValues();
    }
    public void SetDamagePerStack(int newDamagePerStack)
    {
        damagePerStack = newDamagePerStack;
        ValidateValues();
    }
    private void ValidateValues()
    {
        if (damagePerStack <= 0)
        {
            Debug.LogWarning(gameObject.name + " bite damage per stack must be above 0, setting it to 1");
            damagePerStack = 1;
        }
    }
    public bool CanUseOn(GameObject target)
    {
        if (target == null)
        {
            return false;
        }
        Health targetHealth = target.GetComponent<Health>();
        Poison targetPoison = target.GetComponent<Poison>();
        if (targetHealth == null || targetPoison == null)
        {
            return false;
        }

        if (targetHealth.IsDead)
        {
            return false;
        }
        if (targetPoison.PoisonStacks <= 0)
        {
            return false;
        }
        return true;
    }
    public bool TryUseOn(GameObject target)
    {
        if (!CanUseOn(target))
        {
            Debug.Log(gameObject.name + " can't use Serpent's Bite, target needs poison");
            return false;
        }
        Health targetHealth = target.GetComponent<Health>();
        Poison targetPoison = target.GetComponent<Poison>();
        int consumedStacks = targetPoison.ConsumeAllStacks();
        int biteDamage = consumedStacks * damagePerStack;
        Debug.Log(gameObject.name + " used Serpent's Bite on " + target.name + " for " + biteDamage + " damage");
        targetHealth.TakeDamage(biteDamage);
        return true;
    }

    [ContextMenu("Test Serpent's Bite")]
    private void TestBite()
    {
        TryUseOn(testTarget);
    }
}