using UnityEngine;

public class VenomStrike : MonoBehaviour
{
    [SerializeField] private int stacksApplied = 2;
    [SerializeField] private GameObject testTarget;

    public int StacksApplied
    {
        get
        {
            return stacksApplied;
        }
    }

    private void Awake()
    {
        ValidateValues();
    }

    public void SetStacksApplied(int newStacksApplied)
    {
        stacksApplied = newStacksApplied;
        ValidateValues();
    }

    private void ValidateValues()
    {
        if (stacksApplied <= 0)
        {
            Debug.LogWarning(gameObject.name + " venom strike stacks must be above 0, setting it to 1");
            stacksApplied = 1;
        }
    }

    public bool TryUseOn(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        Health targetHealth = target.GetComponent<Health>();
        Poison targetPoison = target.GetComponent<Poison>();
        if (targetHealth == null || targetPoison == null)
        {
            Debug.LogWarning(target.name + " has no Health or Poison, Venom Strike did nothing");
            return false;
        }

        if (targetHealth.IsDead)
        {
            return false;
        }

        Debug.Log(gameObject.name + " used Venom Strike on " + target.name);
        targetPoison.AddStacks(stacksApplied);
        return true;
    }

    // kept so the current controller still compiles, use TryUseOn
    public void UseOn(GameObject target)
    {
        TryUseOn(target);
    }

    [ContextMenu("Test Venom Strike")]
    private void TestVenomStrike()
    {
        TryUseOn(testTarget);
    }
}