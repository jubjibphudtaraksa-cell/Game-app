using UnityEngine;
// Create > Tactical > Weapon
[CreateAssetMenu(menuName = "Tactical/Weapon")]
public class WeaponData : ScriptableObject {
    public string weaponName; public int cost;
    public int damage = 26; public float headMul = 4f, legMul = .85f;
    public float fireRate = 10f;          // นัด/วินาที
    public bool automatic = true; public int pellets = 1;
    public int magSize = 30, reserve = 90; public float reloadTime = 2.4f;
    public float baseSpread = .3f, moveSpread = 2f; // องศา
    public float range = 120f;
    public Vector2[] recoil;               // (yaw, pitch) ต่อนัด
    public AudioClip shotSfx; public GameObject muzzleFx, impactFx;
}
/* อาวุธ (ชื่อใหม่ ไม่ซ้ำ VALORANT):
   Pip(ปืนพกพื้นฐาน) Whisper(ปืนพกเก็บเสียง) Warden(ฮันด์แคนนอน) Hornet(SMG)
   Mastiff(ไรเฟิลยิงรัว3นัด) Falcon(ไรเฟิลเก็บเสียง) Raptor(ไรเฟิลแรง) Longbow(สไนเปอร์)
   ค่าแนะนำ: Pip 0$/dmg25 | Whisper 500/dmg27 | Warden 800/dmg55 | Hornet 1600/dmg26
   Mastiff 2050/dmg35 | Falcon 2900/dmg39 | Raptor 2900/dmg40(หัว160) | Longbow 4500/dmg150 */
