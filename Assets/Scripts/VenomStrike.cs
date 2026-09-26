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
    public void UseOn(GameObject target)
    {
        if (target == null)
        {
            return;
        }
        Poison targetPoison = target.GetComponent<Poison>();
        if (targetPoison == null)
        {
            Debug.LogWarning(target.name + " has no Poison component, Venom Strike did nothing ");
            return;
        }
        Debug.Log(gameObject.name + " used Venom Strike on " + target.name);
        targetPoison.AddStacks(stacksApplied);
    }
    [ContextMenu("Test Venom Strike")]
    private void TestVenomStrike()
    {
        UseOn(testTarget);
    }

}