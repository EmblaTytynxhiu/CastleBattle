using UnityEngine;
using UnityEngine.UI;

/// <summary>Ammo buttons at the bottom of the screen during the battle (also keys 1-6).</summary>
public class AmmoBar : MonoBehaviour
{
    public Image[] slots;
    public Image[] icons;
    public Text[] counts;
    public Sprite slotNormal, slotSelected, slotLocked;

    CannonController cannon;

    public void Bind(CannonController c) { cannon = c; }

    public void Select(int i)
    {
        if (cannon != null && cannon.IsMyTurn) cannon.SelectAmmo(i);
    }

    void Update()
    {
        if (cannon == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            bool has = cannon.HasAmmo((AmmoType)i);
            if (slots[i]) slots[i].sprite = !has ? slotLocked : ((int)cannon.selectedAmmo == i ? slotSelected : slotNormal);
            if (icons[i]) icons[i].color = has ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            if (counts[i]) counts[i].text = i == 0 ? "∞" : cannon.ammo[i].ToString();
        }
    }
}
