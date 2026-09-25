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
    public int PosionStacks
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
    
}