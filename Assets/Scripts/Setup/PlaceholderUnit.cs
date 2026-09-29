using UnityEngine;

public enum UnitSide
{
    Player,
    Enemy
}

[RequireComponent(typeof(Health))]
public class PlaceholderUnit : MonoBehaviour
{
    public UnitSide side = UnitSide.Player;
    public string displayName = "Placeholder";

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color playerColor = new Color(0.2f, 0.5f, 0.9f);
    [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f);

    void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        spriteRenderer.color = side == UnitSide.Player ? playerColor : enemyColor;
    }
}