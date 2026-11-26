using UnityEngine;

/// <summary>
/// The building pieces of one team, in the same order as the RESOURCES panel slots
/// (Wood / Stone / Metal / Special tabs, 12 pieces each).
/// </summary>
[CreateAssetMenu(menuName = "Castle Battle/Piece Catalog")]
public class PieceCatalog : ScriptableObject
{
    public GameObject[] wood = new GameObject[12];
    public GameObject[] stone = new GameObject[12];
    public GameObject[] metal = new GameObject[12];
    public GameObject[] special = new GameObject[12];

    public GameObject Get(int category, int index)
    {
        GameObject[] a = category == 0 ? wood : category == 1 ? stone : category == 2 ? metal : special;
        if (a == null || index < 0 || index >= a.Length) return null;
        return a[index];
    }
}
