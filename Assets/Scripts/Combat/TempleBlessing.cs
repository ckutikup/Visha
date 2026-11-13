using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class TempleBlessing : MonoBehaviour
{
    private bool isEnabled = false;
    private bool isSpent = false;

    public event Action OnBlessingTriggered;

    public bool IsEnabled
    {
        get
        {
            return isEnabled;
        }
    }

    public bool IsSpent
    {
        get
        {
            return isSpent;
        }
    }

    public bool IsAvailable
    {
        get
        {
            return isEnabled && !isSpent;
        }
    }

    public void EnableForEncounter()
    {
        isEnabled = true;
        isSpent = false;
    }

    public void DisableForEncounter()
    {
        isEnabled = false;
        isSpent = false;
    }

    public void RestoreForRetry()
    {
        if (isEnabled)
        {
            isSpent = false;
        }
    }

    public void Spend()
    {
        if (!IsAvailable)
        {
            return;
        }

        isSpent = true;
        Debug.Log(gameObject.name + " survived with Temple's Blessing");
        OnBlessingTriggered?.Invoke();
    }

    [ContextMenu("Test Enable Blessing")]
    private void TestEnable()
    {
        EnableForEncounter();
    }
}