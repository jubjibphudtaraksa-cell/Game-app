using UnityEngine;
// วางบนพื้นที่ Trigger ของจุดวางระเบิด (Site A / Site B)
public class SpikeSite : MonoBehaviour {
    public string siteName = "A"; public float plantTime = 4f, defuseTime = 7f;
    float hold; public bool planted;
    void OnTriggerStay(Collider c) {
        var h = c.GetComponent<Health>(); if (!h || !h.Alive || !GameManager.I.IsLive) return;
        bool plant = h.team == Team.Attackers && !planted && GameManager.I.attackerHasSpike;
        bool defuse = h.team == Team.Defenders && planted;
        if ((plant || defuse) && Input.GetKey(KeyCode.E)) {
            hold += Time.deltaTime;
            HUD.I?.SetProgress(hold / (plant ? plantTime : defuseTime));
            if (plant && hold >= plantTime) { planted = true; hold = 0; GameManager.I.OnPlanted(this); }
            if (defuse && hold >= defuseTime) { planted = false; hold = 0; GameManager.I.OnDefused(); }
        } else { hold = 0; HUD.I?.SetProgress(0); }
    }
    public void ResetSite(){ planted = false; hold = 0; }
}
