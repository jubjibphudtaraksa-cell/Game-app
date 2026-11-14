using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HUD : MonoBehaviour {
    public static HUD I;
    public GameManager.PlayerState local; public bool buyOpen;
    float prog, progUntil, hitUntil; bool hitHead; string result = "";
    readonly List<KeyValuePair<float, string>> feed = new List<KeyValuePair<float, string>>();
    GUIStyle lbl, btn;
    static readonly Color Cyan = new Color(.4f, .8f, 1f), Red = new Color(1f, .45f, .4f);

    void Awake() { I = this; }
    public void ShowBuyMenu(bool on) { buyOpen = on; }
    public void HitMarker(bool head) { hitUntil = Time.time + .18f; hitHead = head; }
    public void SetProgress(float v) { prog = Mathf.Clamp01(v); progUntil = Time.time + .15f; }
    public void AddKillFeed(string t) { feed.Add(new KeyValuePair<float, string>(Time.time + 6f, t)); if (feed.Count > 6) feed.RemoveAt(0); }
    public void ShowRoundResult(string t) { result = t; }
    public void ClearResult() { result = ""; }

    void Update() {
        var g = GameManager.I; if (g == null) return;
        feed.RemoveAll(f => f.Key < Time.time);
        if (g.phase != Phase.Buy) buyOpen = false;
        else if (Input.GetKeyDown(KeyCode.B) || TouchInput.BuyDown) buyOpen = !buyOpen;
        if (!Application.isMobilePlatform) {
            bool need = buyOpen || g.phase == Phase.MatchEnd;
            Cursor.lockState = need ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = need;
        }
    }

    void Fill(float x, float y, float w, float h, Color c) {
        Color old = GUI.color; GUI.color = c; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = old;
    }
    void Txt(float x, float y, float w, float h, string s, int size, TextAnchor a, Color c) {
        lbl.fontSize = size; lbl.alignment = a; lbl.normal.textColor = c; GUI.Label(new Rect(x, y, w, h), s, lbl);
    }

    void OnGUI() {
        var g = GameManager.I; if (g == null || local == null) return;
        if (lbl == null) {
            lbl = new GUIStyle(GUI.skin.label); lbl.wordWrap = false; lbl.fontStyle = FontStyle.Bold;
            btn = new GUIStyle(GUI.skin.button); btn.fontStyle = FontStyle.Bold; btn.wordWrap = true;
        }
        float s = Screen.height / 720f; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = 720f; var hp = local.health;
        int my = local.teamIndex, en = 1 - my;
        TextAnchor mc = TextAnchor.MiddleCenter;

        // Round timer + score
        Fill(W / 2 - 150, 6, 300, 52, new Color(0, 0, 0, .5f));
        Txt(W / 2 - 150, 6, 100, 52, g.score[my].ToString(), 36, mc, Cyan);
        Txt(W / 2 + 50, 6, 100, 52, g.score[en].ToString(), 36, mc, Red);
        Txt(W / 2 - 50, 6, 100, 52, Mathf.Max(0, Mathf.CeilToInt(g.timer)).ToString(), 32, mc, g.spikePlanted ? new Color(1f, .3f, .3f) : Color.white);
        string ph = g.phase == Phase.Buy ? "BUY PHASE" : (g.spikePlanted ? "SPIKE PLANTED" : "ROUND " + g.round);
        Txt(W / 2 - 150, 58, 300, 22, ph + "  -  " + (hp.team == Team.Attackers ? "ATTACK" : "DEFEND"), 14, mc, Color.white);

        // Health / Armor
        Fill(20, H - 100, 200, 80, new Color(0, 0, 0, .5f));
        Txt(30, H - 98, 90, 44, "HP", 16, TextAnchor.MiddleLeft, Color.white);
        Txt(100, H - 98, 110, 44, hp.hp.ToString(), 34, TextAnchor.MiddleRight, hp.hp > 40 ? Color.white : Red);
        Txt(30, H - 54, 90, 30, "ARMOR", 14, TextAnchor.MiddleLeft, Color.white);
        Txt(100, H - 54, 110, 30, hp.armor.ToString(), 24, TextAnchor.MiddleRight, Cyan);

        // Weapon / ammo / money
        var wc = local.weapons; bool hasW = wc != null && wc.Current != null;
        Fill(W / 2 - 130, H - 100, 260, 80, new Color(0, 0, 0, .5f));
        Txt(W / 2 - 130, H - 100, 260, 24, "$" + local.money, 16, mc, new Color(.6f, 1f, .6f));
        Txt(W / 2 - 130, H - 78, 260, 20, hasW ? wc.Current.weaponName.ToUpper() : "", 14, mc, Color.white);
        Txt(W / 2 - 130, H - 58, 260, 36, hasW ? (wc.Reloading ? "RELOADING" : wc.Mag + " / " + wc.Reserve) : "", 28, mc, Color.white);

        // Kill feed
        for (int i = 0; i < feed.Count; i++) Txt(W - 460, 12 + i * 24, 450, 24, feed[i].Value, 16, TextAnchor.MiddleRight, Color.white);

        // Crosshair
        float cx = W / 2, cy = H / 2, gap = 5, len = 9, th = 2;
        Color cc2 = Time.time < hitUntil ? (hitHead ? Color.red : Color.white) : new Color(.3f, 1f, .6f);
        Fill(cx - gap - len, cy - th / 2, len, th, cc2); Fill(cx + gap, cy - th / 2, len, th, cc2);
        Fill(cx - th / 2, cy - gap - len, th, len, cc2); Fill(cx - th / 2, cy + gap, th, len, cc2);

        if (Time.time < progUntil) { Fill(cx - 100, cy + 70, 200, 10, new Color(0, 0, 0, .6f)); Fill(cx - 100, cy + 70, 200 * prog, 10, new Color(1f, .8f, .2f)); }
        if (!hp.Alive && g.phase != Phase.MatchEnd) Txt(0, H / 2 - 150, W, 60, "ELIMINATED", 44, mc, Red);
        if (result != "" && g.phase != Phase.MatchEnd) Txt(0, 110, W, 50, result, 28, mc, Color.white);

        // Buy menu (Weapon Selection)
        if (buyOpen && g.phase == Phase.Buy) {
            Fill(W / 2 - 390, 70, 780, 540, new Color(0, 0, 0, .88f));
            Txt(W / 2 - 380, 78, 760, 40, "BUY PHASE   $" + local.money + "   (" + Mathf.CeilToInt(g.timer) + "s)", 26, mc, Color.white);
            btn.fontSize = 15;
            for (int i = 0; i < g.shop.Length; i++) {
                var w = g.shop[i]; int col = i % 4, row = i / 4;
                Rect r = new Rect(W / 2 - 380 + col * 192, 130 + row * 130, 182, 120);
                bool owned = local.weapons.Owns(w);
                GUI.enabled = !owned && local.money >= w.cost;
                string label = w.weaponName.ToUpper() + "\n$" + w.cost + "\nDMG " + w.damage + " / HS " + Mathf.RoundToInt(w.damage * w.headMul) + "\n" + (w.isSidearm ? "SIDEARM" : "PRIMARY") + (owned ? "  (OWNED)" : "");
                if (GUI.Button(r, label, btn)) g.TryBuy(local, w);
                GUI.enabled = true;
            }
            GUI.enabled = hp.armor < 50 && local.money >= 650;
            if (GUI.Button(new Rect(W / 2 - 380, 400, 182, 70), "ARMOR\n$650", btn)) g.TryBuyArmor(local);
            GUI.enabled = true;
            if (GUI.Button(new Rect(W / 2 + 8, 520, 182, 70), "READY", btn)) g.ReadyUp();
            if (GUI.Button(new Rect(W / 2 + 200, 520, 182, 70), "CLOSE", btn)) buyOpen = false;
        }

        // Scoreboard
        if (Input.GetKey(KeyCode.Tab) || TouchInput.ScoreHeld) {
            Fill(W / 2 - 300, 90, 600, 480, new Color(0, 0, 0, .82f)); float y = 100;
            for (int ti = 0; ti < 2; ti++) {
                Txt(W / 2 - 290, y, 580, 28, (ti == my ? "YOUR TEAM   " : "ENEMY TEAM   ") + g.score[ti], 20, TextAnchor.MiddleLeft, ti == my ? Cyan : Red); y += 30;
                foreach (var p in g.players) {
                    if (p.teamIndex != ti) continue;
                    Color c = p.health.Alive ? Color.white : new Color(1, 1, 1, .4f);
                    Txt(W / 2 - 290, y, 260, 24, p.name, 16, TextAnchor.MiddleLeft, c);
                    Txt(W / 2, y, 80, 24, "K " + p.kills, 16, TextAnchor.MiddleLeft, c);
                    Txt(W / 2 + 90, y, 80, 24, "D " + p.deaths, 16, TextAnchor.MiddleLeft, c);
                    if (ti == my) Txt(W / 2 + 180, y, 100, 24, "$" + p.money, 16, TextAnchor.MiddleLeft, c);
                    y += 24;
                }
                y += 14;
            }
        }

        // Match end
        if (g.phase == Phase.MatchEnd) {
            Fill(0, 0, W, H, new Color(0, 0, 0, .6f));
            Txt(0, 190, W, 100, result, 64, mc, Color.white);
            btn.fontSize = 22;
            if (GUI.Button(new Rect(W / 2 - 210, 340, 200, 70), "REMATCH", btn)) SceneManager.LoadScene("Match");
            if (GUI.Button(new Rect(W / 2 + 10, 340, 200, 70), "MENU", btn)) SceneManager.LoadScene("Menu");
        }
    }
}
