using UnityEngine;
public class WeaponController : MonoBehaviour {
    public Camera cam; public PlayerController player; public Health self;
    public AudioSource audioSrc; public LayerMask hitMask = ~0;
    public WeaponData primary, sidearm; public WeaponData Current { get; private set; }
    int mag, reserve; int shot; float nextFire, lastShot, reloadEnd; bool reloading;
    public int Mag => mag; public int Reserve => reserve;
    void Start(){ Equip(primary ? primary : sidearm); }
    public void Equip(WeaponData w){ if(!w) return; Current = w; mag = w.magSize; reserve = w.reserve; reloading = false; shot = 0; }
    public void Buy(WeaponData w){ if (w.cost > 0 && w.damage < 40 && w.fireRate > 6) primary = w; primary = w.cost >= 800 ? w : primary; Equip(w); }
    void Update() {
        if (!player.controlsEnabled || Current == null || !self.Alive) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) && primary) Equip(primary);
        if (Input.GetKeyDown(KeyCode.Alpha2) && sidearm) Equip(sidearm);
        if (reloading && Time.time >= reloadEnd) { int n = Mathf.Min(Current.magSize - mag, reserve); mag += n; reserve -= n; reloading = false; }
        if (Input.GetKeyDown(KeyCode.R)) StartReload();
        if (Time.time - lastShot > .35f) shot = 0;     // รีเซ็ต recoil
        bool trig = Current.automatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
        if (trig && !reloading && Time.time >= nextFire) { if (mag > 0) Fire(); else StartReload(); }
    }
    void StartReload(){ if (reloading || mag == Current.magSize || reserve <= 0) return; reloading = true; reloadEnd = Time.time + Current.reloadTime; }
    void Fire() {
        mag--; nextFire = Time.time + 1f / Current.fireRate; lastShot = Time.time;
        float spread = Current.baseSpread + (player.Moving ? Current.moveSpread : 0);
        for (int i = 0; i < Current.pellets; i++) {
            Vector3 dir = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0) * cam.transform.forward;
            if (Physics.Raycast(cam.transform.position, dir, out var hit, Current.range, hitMask)) {
                var hz = hit.collider.GetComponent<HitZone>();
                if (hz && hz.owner && hz.owner.team != self.team) {
                    float m = hz.zone == HitZoneType.Head ? Current.headMul : hz.zone == HitZoneType.Leg ? Current.legMul : 1f;
                    hz.owner.TakeDamage(Mathf.RoundToInt(Current.damage * m), self, Current.weaponName);
                    HUD.I?.HitMarker(hz.zone == HitZoneType.Head);
                } else if (Current.impactFx) Destroy(Instantiate(Current.impactFx, hit.point, Quaternion.LookRotation(hit.normal)), 2f);
            }
        }
        if (Current.recoil != null && Current.recoil.Length > 0) player.AddRecoil(Current.recoil[Mathf.Min(shot, Current.recoil.Length - 1)]);
        shot++;
        if (audioSrc && Current.shotSfx) audioSrc.PlayOneShot(Current.shotSfx);
    }
}
