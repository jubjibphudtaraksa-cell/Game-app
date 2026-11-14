using System.Collections.Generic;
using UnityEngine;

public class BotAI : MonoBehaviour {
    public Health self; public WeaponData weapon; public Transform eye; public bool enabledAI; public float skill = .55f;
    public bool HasTarget { get { return target != null && target.Alive; } }
    CharacterController cc; AudioSource src; Health target;
    float retarget, nextFire, seenAt, vy, seed, repath; int burst, siteIdx; Vector3 offset, lastGoal;
    List<Vector3> path; int pi;

    void Awake() { cc = GetComponent<CharacterController>(); src = GetComponent<AudioSource>(); seed = Random.value * 10f; }

    public void SetupRound(int site) {
        siteIdx = site; Vector2 o = Random.insideUnitCircle * 3f; offset = new Vector3(o.x, 0, o.y);
        path = null; target = null; vy = 0; burst = 0;
    }
    public void RoundReset(bool losePrimary) { if (losePrimary && GameManager.I != null) weapon = GameManager.I.shop[0]; }

    void Update() {
        if (!enabledAI || !self.Alive) return;
        var g = GameManager.I; if (g == null) return;
        if (Time.time >= retarget) {
            retarget = Time.time + .2f; Health t = FindEnemy(g);
            if (t != null && target == null) seenAt = Time.time;
            target = t;
        }
        Vector3 move = Vector3.zero; float speed = 4.2f;
        if (HasTarget) {
            Vector3 aim = target.transform.position + Vector3.up * 1.2f; Vector3 dir = aim - eye.position;
            Face(dir);
            Vector3 flat = dir; flat.y = 0;
            if (Time.time >= nextFire && Time.time > seenAt + .45f + (1f - skill) * .5f && flat.sqrMagnitude > .01f && Vector3.Angle(transform.forward, flat) < 10f) Shoot(dir);
            move = transform.right * Mathf.Sign(Mathf.Sin(Time.time * .9f + seed)) * .5f;
            if (dir.magnitude > 25f) move += transform.forward * .5f;
            speed = 2.6f;
        } else {
            move = FollowPath(GoalPos(g));
        }
        vy = cc.isGrounded ? -1f : vy - 20f * Time.deltaTime;
        cc.Move((move.normalized * speed + Vector3.up * vy) * Time.deltaTime);
    }

    void Face(Vector3 dir) {
        dir.y = 0; if (dir.sqrMagnitude < .001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), (300f + skill * 250f) * Time.deltaTime);
    }

    void Shoot(Vector3 dir) {
        float rate = 1f / weapon.fireRate;
        nextFire = Time.time + (weapon.automatic ? rate * 1.3f : rate + Random.Range(.15f, .45f));
        if (weapon.automatic && ++burst >= Random.Range(3, 7)) { burst = 0; nextFire += Random.Range(.25f, .6f); }
        float spr = (1.25f - skill) * 1.6f + .2f;
        Vector3 d = Quaternion.Euler(Random.Range(-spr, spr), Random.Range(-spr, spr), 0) * dir.normalized;
        Util.Shoot(self, eye.position, d, eye.position + transform.right * .25f, weapon);
        Sfx.Play(src, weapon.pitch);
    }

    Health FindEnemy(GameManager g) {
        Health best = null; float bd = 60f;
        foreach (var p in g.players) {
            var h = p.health;
            if (h.team == self.team || !h.Alive || !p.go.activeInHierarchy) continue;
            Vector3 to = h.transform.position + Vector3.up * 1.2f - eye.position; float d = to.magnitude;
            if (d > bd) continue;
            bool front = Vector3.Dot(transform.forward, to.normalized) > 0f || d < 12f || Time.time - self.lastDamageTime < 2f;
            if (!front) continue;
            RaycastHit hit;
            if (Util.FirstHit(eye.position, to.normalized, d + .5f, transform, out hit)) {
                var hz = hit.collider.GetComponent<HitZone>();
                if (hz != null && hz.owner == h) { best = h; bd = d; }
            }
        }
        return best;
    }

    Vector3 GoalPos(GameManager g) {
        if (g.spikePlanted && g.plantedSite != null) {
            Vector3 c = g.plantedSite.transform.position;
            return self.team == Team.Defenders ? c : c + offset;
        }
        return g.sites[siteIdx % g.sites.Length].transform.position + offset;
    }

    Vector3 FollowPath(Vector3 goal) {
        Vector3 flat = goal - transform.position; flat.y = 0;
        if (flat.magnitude < 1f) return Vector3.zero;
        if (path == null || (goal - lastGoal).sqrMagnitude > 4f || Time.time > repath) {
            path = MapGrid.FindPath(transform.position, goal); pi = 0; lastGoal = goal; repath = Time.time + 3f;
        }
        Vector3 wp = goal;
        if (path != null && pi < path.Count) {
            wp = path[pi];
            Vector3 w2 = wp - transform.position; w2.y = 0;
            if (w2.magnitude < 1.1f) { pi++; if (pi < path.Count) wp = path[pi]; else wp = goal; }
        }
        Vector3 to = wp - transform.position; to.y = 0;
        Face(to);
        return to;
    }
}
