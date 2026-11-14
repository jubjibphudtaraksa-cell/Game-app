using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour {
    public Camera cam; public PlayerController player; public Health self; public AudioSource audioSrc; public Light muzzle; public Transform viewmodel;
    public WeaponData primary, sidearm;
    public WeaponData Current { get; private set; }
    Dictionary<WeaponData, int> mags = new Dictionary<WeaponData, int>(), res = new Dictionary<WeaponData, int>();
    int shot; float nextFire, lastShot, reloadEnd, flashUntil; bool reloading, prevTouchFire; Vector3 vmBase;
    public bool Reloading { get { return reloading; } }
    public int Mag { get { return Current != null && mags.ContainsKey(Current) ? mags[Current] : 0; } }
    public int Reserve { get { return Current != null && res.ContainsKey(Current) ? res[Current] : 0; } }

    void Start() {
        if (viewmodel != null) vmBase = viewmodel.localPosition;
        if (Current == null) Equip(primary != null ? primary : sidearm, false);
    }

    public bool Owns(WeaponData w) { return primary == w || sidearm == w; }

    public void Equip(WeaponData w, bool refill) {
        if (w == null) return;
        Current = w;
        if (refill || !mags.ContainsKey(w)) { mags[w] = w.magSize; res[w] = w.reserve; }
        reloading = false; shot = 0;
    }

    public void Buy(WeaponData w) { if (w.isSidearm) sidearm = w; else primary = w; Equip(w, true); }

    public void RoundReset(bool losePrimary) {
        if (losePrimary) primary = null;
        if (sidearm == null) sidearm = GameManager.I.shop[0];
        mags.Clear(); res.Clear();
        Equip(primary != null ? primary : sidearm, true);
    }

    void Update() {
        if (muzzle != null) muzzle.enabled = Time.time < flashUntil;
        if (viewmodel != null) viewmodel.localPosition = Vector3.Lerp(viewmodel.localPosition, vmBase, Time.deltaTime * 14f);
        if (Current == null || !player.controlsEnabled || !self.Alive) return;
        bool mob = Application.isMobilePlatform;
        if (!mob) {
            if (Input.GetKeyDown(KeyCode.Alpha1) && primary != null) Equip(primary, false);
            if (Input.GetKeyDown(KeyCode.Alpha2) && sidearm != null) Equip(sidearm, false);
        }
        if (TouchInput.SwapDown || Input.GetKeyDown(KeyCode.Q)) {
            if (Current == primary && sidearm != null) Equip(sidearm, false); else if (primary != null) Equip(primary, false);
        }
        if (reloading && Time.time >= reloadEnd) {
            int n = Mathf.Min(Current.magSize - mags[Current], res[Current]);
            mags[Current] += n; res[Current] -= n; reloading = false;
        }
        if (Input.GetKeyDown(KeyCode.R) || TouchInput.ReloadDown) StartReload();
        if (Time.time - lastShot > .35f) shot = 0;
        bool down = mob ? (TouchInput.Fire && !prevTouchFire) : Input.GetButtonDown("Fire1");
        bool held = mob ? TouchInput.Fire : Input.GetButton("Fire1");
        prevTouchFire = TouchInput.Fire;
        bool trig = Current.automatic ? held : down;
        if (trig && !reloading && Time.time >= nextFire) { if (mags[Current] > 0) Fire(); else StartReload(); }
    }

    void StartReload() {
        if (reloading || mags[Current] == Current.magSize || res[Current] <= 0) return;
        reloading = true; reloadEnd = Time.time + Current.reloadTime;
    }

    void Fire() {
        mags[Current]--; nextFire = Time.time + 1f / Current.fireRate; lastShot = Time.time;
        float spread = Current.baseSpread + (player.Moving ? Current.moveSpread : 0f);
        Vector3 dir = cam.transform.rotation * Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0) * Vector3.forward;
        Transform c = cam.transform;
        Vector3 tracerStart = c.position + c.right * .2f - c.up * .12f + c.forward * .6f;
        int r = Util.Shoot(self, c.position, dir, tracerStart, Current);
        if (r > 0 && HUD.I != null) HUD.I.HitMarker(r == 2);
        player.AddRecoil(Current.Recoil(shot)); shot++;
        flashUntil = Time.time + .04f;
        if (muzzle != null) muzzle.enabled = true;
        if (viewmodel != null) viewmodel.localPosition = vmBase + new Vector3(0, .01f, -.06f);
        Sfx.Play(audioSrc, Current.pitch);
    }
}
