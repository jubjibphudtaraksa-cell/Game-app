using UnityEngine;

public class SpikeSite : MonoBehaviour {
    public string siteName = "A"; public float radius = 5.5f, plantTime = 4f, defuseTime = 7f;
    public bool planted; float hold; GameObject vis;

    void Update() {
        var g = GameManager.I; if (g == null || !g.IsLive) return;
        GameManager.PlayerState actor = null; bool isPlant = false;
        foreach (var p in g.players) {
            if (!p.health.Alive || !p.go.activeInHierarchy) continue;
            Vector3 d = p.go.transform.position - transform.position; d.y = 0;
            if (d.magnitude > radius) continue;
            bool plant = p.health.team == Team.Attackers && !g.spikePlanted;
            bool defuse = p.health.team == Team.Defenders && planted;
            if (!plant && !defuse) continue;
            bool wants = p.bot != null ? !p.bot.HasTarget : (Input.GetKey(KeyCode.E) || TouchInput.Use);
            if (!wants) continue;
            actor = p; isPlant = plant; break;
        }
        if (actor == null) { hold = 0; return; }
        hold += Time.deltaTime;
        float need = isPlant ? plantTime : defuseTime;
        if (actor == g.local && HUD.I != null) HUD.I.SetProgress(hold / need);
        if (hold >= need) {
            hold = 0;
            if (isPlant) {
                planted = true;
                vis = Util.Prim(PrimitiveType.Cube, transform, new Vector3(0, .15f, 0), new Vector3(.6f, .3f, .4f), new Color(1f, .15f, .1f), false, true);
                g.OnPlanted(this, actor);
            } else { planted = false; g.OnDefused(actor); }
        }
    }

    public void ResetSite() { planted = false; hold = 0; if (vis != null) Destroy(vis); }
}
