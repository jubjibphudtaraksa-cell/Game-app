using System;
using UnityEngine;
public enum Team { Attackers, Defenders }
public enum HitZoneType { Head, Body, Leg }

public class HitZone : MonoBehaviour {   // ใส่บน Collider ย่อย (หัว/ตัว/ขา)
    public HitZoneType zone; public Health owner;
    void Awake(){ if(!owner) owner = GetComponentInParent<Health>(); }
}

public class Health : MonoBehaviour {
    public Team team; public int hp = 100, armor = 0;
    public bool Alive => hp > 0;
    public event Action<Health, Health, string> OnDied; // victim, attacker, weapon
    public event Action OnChanged;
    public void TakeDamage(int dmg, Health attacker, string weapon) {
        if (!Alive) return;
        if (armor > 0) { int ab = Mathf.Min(armor, Mathf.RoundToInt(dmg * .5f)); armor -= ab; dmg -= ab; }
        hp = Mathf.Max(0, hp - dmg); OnChanged?.Invoke();
        if (hp == 0) OnDied?.Invoke(this, attacker, weapon);
    }
    public void ResetFor(int newHp, int newArmor){ hp = newHp; armor = newArmor; OnChanged?.Invoke(); }
}
