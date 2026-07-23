using UnityEngine;

/// <summary>Minimal item definition used by dialogue requirements and actions.</summary>
[CreateAssetMenu(menuName = "Game/Items/Item Data", fileName = "ItemData")]
public sealed class ItemData : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
}
