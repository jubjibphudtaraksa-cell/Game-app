using UnityEngine;

public class WeaponData : ScriptableObject {
    public string weaponName; public int cost; public bool isSidearm;
    public int damage = 26; public float headMul = 3f, legMul = .85f, fireRate = 8f;
    public bool automatic = true; public int magSize = 30, reserve = 90; public float reloadTime = 2.4f;
    public float baseSpread = .3f, moveSpread = 2f, range = 200f, recoilUp = .4f, recoilSide = .15f, pitch = 1f;

    public Vector2 Recoil(int shot) {
        float up = recoilUp * (shot < 8 ? 1f : .55f);
        float side = Mathf.Sin(shot * .9f) * recoilSide * (shot < 3 ? .3f : 1f);
        return new Vector2(side, up);
    }
}

// อาวุธชื่อใหม่ทั้งหมด (ไม่ใช้ชื่อ/ค่าของ VALORANT)
public static class WeaponDB {
    static WeaponData M(string n, int cost, bool side, int dmg, float head, float rate, bool auto, int mag, int res, float rel, float spr, float mspr, float up, float sd, float pitch) {
        var w = ScriptableObject.CreateInstance<WeaponData>();
        w.weaponName = n; w.cost = cost; w.isSidearm = side; w.damage = dmg; w.headMul = head; w.fireRate = rate; w.automatic = auto;
        w.magSize = mag; w.reserve = res; w.reloadTime = rel; w.baseSpread = spr; w.moveSpread = mspr; w.recoilUp = up; w.recoilSide = sd; w.pitch = pitch;
        return w;
    }
    // index 0 = Pip (ปืนพกฟรีประจำตัว)
    public static WeaponData[] Create() {
        return new WeaponData[] {
            M("Pip",      0,    true,  25,  3f,   6.5f, false, 12, 36, 1.5f, .30f, 1.5f, .50f, .10f, 1.10f),
            M("Whisper",  500,  true,  28,  3f,   6.5f, false, 13, 39, 1.5f, .22f, 1.5f, .45f, .08f, 1.30f),
            M("Warden",   850,  true,  60,  3f,   2.5f, false, 6,  24, 2.2f, .20f, 3.0f, 1.20f, .20f, .75f),
            M("Hornet",   1500, false, 25,  3f,   13f,  true,  30, 90, 2.2f, .35f, 2.0f, .30f, .20f, 1.15f),
            M("Mastiff",  2000, false, 34,  3.4f, 9f,   true,  24, 72, 2.5f, .30f, 2.5f, .38f, .18f, .95f),
            M("Falcon",   2700, false, 38,  3.5f, 11f,  true,  30, 90, 2.5f, .28f, 3.0f, .42f, .22f, 1.30f),
            M("Raptor",   2900, false, 40,  4f,   9.5f, true,  25, 75, 2.5f, .25f, 3.5f, .48f, .25f, .85f),
            M("Longbow",  4500, false, 140, 1.7f, .8f,  false, 5,  10, 3.7f, .05f, 7.0f, 2.00f, .10f, .55f),
        };
    }
}
