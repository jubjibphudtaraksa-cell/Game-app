using System.Collections.Generic;
using UnityEngine;

public enum Team { Attackers, Defenders }
public enum HitZoneType { Head, Body, Leg }

public class HitZone : MonoBehaviour {
    public HitZoneType zone;
    public Health owner;
}

public class Health : MonoBehaviour {
    public Team team;
    public int hp = 100, armor;
    public string displayName = "Player";
    public Health lastAttacker;
    public float lastDamageTime = -99f;
    public bool Alive { get { return hp > 0; } }
    public event System.Action<Health, Health, string> OnDied;

    public void TakeDamage(int dmg, Health attacker, string weapon) {
        if (!Alive) return;
        if (armor > 0) { int ab = Mathf.Min(armor, Mathf.RoundToInt(dmg * .5f)); armor -= ab; dmg -= ab; }
        hp = Mathf.Max(0, hp - dmg);
        lastDamageTime = Time.time; lastAttacker = attacker;
        if (hp == 0 && OnDied != null) OnDied(this, attacker, weapon);
    }
    public void ResetFor(int newHp, int newArmor) { hp = newHp; armor = newArmor; lastAttacker = null; lastDamageTime = -99f; }
}

public static class Util {
    public const int HitLayer = 8;
    static Material baseMat;
    static Dictionary<int, Material> mats = new Dictionary<int, Material>();

    static Material GetMat(Color c, Material src) {
        Color32 k = c;
        int key = (k.r << 24) | (k.g << 16) | (k.b << 8) | k.a;
        Material m;
        if (!mats.TryGetValue(key, out m) || m == null) {
            if (baseMat == null) baseMat = src;
            m = new Material(baseMat);
            m.color = c; m.enableInstancing = true;
            mats[key] = m;
        }
        return m;
    }

    public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 lp, Vector3 scale, Color c, bool collider, bool shadows) {
        var go = GameObject.CreatePrimitive(t);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lp; go.transform.localScale = scale;
        if (!collider) { var col = go.GetComponent<Collider>(); if (col != null) UnityEngine.Object.DestroyImmediate(col); }
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = GetMat(c, r.sharedMaterial);
        if (!shadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static bool FirstHit(Vector3 o, Vector3 d, float range, Transform ignoreRoot, out RaycastHit best) {
        var hits = Physics.RaycastAll(o, d, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bd = float.MaxValue; bool found = false; best = default(RaycastHit);
        for (int i = 0; i < hits.Length; i++) {
            if (hits[i].collider.transform.IsChildOf(ignoreRoot)) continue;
            if (hits[i].distance < bd) { bd = hits[i].distance; best = hits[i]; found = true; }
        }
        return found;
    }

    // return: 0 miss/wall, 1 body hit, 2 headshot
    public static int Shoot(Health shooter, Vector3 origin, Vector3 dir, Vector3 tracerStart, WeaponData w) {
        int result = 0; Vector3 end = origin + dir * w.range;
        RaycastHit h;
        if (FirstHit(origin, dir, w.range, shooter.transform, out h)) {
            end = h.point;
            var hz = h.collider.GetComponent<HitZone>();
            if (hz != null && hz.owner != null) {
                var o = hz.owner;
                if (o != shooter && o.team != shooter.team && o.Alive) {
                    float m = hz.zone == HitZoneType.Head ? w.headMul : (hz.zone == HitZoneType.Leg ? w.legMul : 1f);
                    o.TakeDamage(Mathf.RoundToInt(w.damage * m), shooter, w.weaponName);
                    result = hz.zone == HitZoneType.Head ? 2 : 1;
                }
            } else Spark(h.point);
        }
        Tracer(tracerStart, end);
        return result;
    }

    static void Tracer(Vector3 a, Vector3 b) {
        Vector3 d = b - a; float len = d.magnitude; if (len < 1f) return;
        var go = Prim(PrimitiveType.Cube, null, a + d * .5f, new Vector3(.02f, .02f, len), new Color(1f, .9f, .5f), false, false);
        go.transform.rotation = Quaternion.LookRotation(d);
        UnityEngine.Object.Destroy(go, .05f);
    }

    static void Spark(Vector3 p) {
        var go = Prim(PrimitiveType.Sphere, null, p, Vector3.one * .12f, new Color(1f, .8f, .3f), false, false);
        UnityEngine.Object.Destroy(go, .12f);
    }
}

public static class Sfx {
    static AudioClip shot;
    public static AudioClip Shot() {
        if (shot == null) {
            int sr = 22050, n = sr / 5; var d = new float[n]; var rnd = new System.Random(7);
            for (int i = 0; i < n; i++) {
                float t = i / (float)n; float env = Mathf.Pow(1f - t, 3f);
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                d[i] = (noise * .7f + Mathf.Sin(i * .045f) * .5f) * env * .8f;
            }
            shot = AudioClip.Create("shot", n, 1, sr, false); shot.SetData(d, 0);
        }
        return shot;
    }
    public static void Play(AudioSource s, float pitch) { if (s == null) return; s.pitch = pitch; s.PlayOneShot(Shot(), .9f); }
}
