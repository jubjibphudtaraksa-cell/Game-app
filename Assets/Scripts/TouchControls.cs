using UnityEngine;

public static class TouchInput {
    public static Vector2 Move, Look;
    public static bool Fire, Use, ScoreHeld, JumpDown, ReloadDown, SwapDown, BuyDown;
}

[DefaultExecutionOrder(-100)]
public class TouchControls : MonoBehaviour {
    int moveId = -1, lookId = -1; Vector2 moveOrigin, movePos; float sens = 2f;
    Texture2D circle; GUIStyle st;
    Rect fire, jump, reload, swap, use, score, buy;
    static float S { get { return Screen.height / 720f; } }

    void Awake() {
        sens = PlayerPrefs.GetFloat("sens", 2f);
        circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) {
            float dx = x - 31.5f, dy = y - 31.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = d < 30f ? 1f : (d < 32f ? (32f - d) / 2f : 0f);
            circle.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        circle.Apply();
    }

    void Layout() {
        float s = S, W = Screen.width, H = Screen.height;
        fire = new Rect(W - 210 * s, 60 * s, 150 * s, 150 * s);
        jump = new Rect(W - 110 * s, 230 * s, 90 * s, 90 * s);
        reload = new Rect(W - 230 * s, 230 * s, 90 * s, 90 * s);
        swap = new Rect(W - 350 * s, 230 * s, 90 * s, 90 * s);
        use = new Rect(W - 340 * s, 90 * s, 110 * s, 110 * s);
        score = new Rect(20 * s, H - 80 * s, 100 * s, 60 * s);
        buy = new Rect(130 * s, H - 80 * s, 100 * s, 60 * s);
    }

    void Update() {
        if (!Application.isMobilePlatform) return;
        Layout();
        TouchInput.Look = Vector2.zero;
        TouchInput.JumpDown = false; TouchInput.ReloadDown = false; TouchInput.SwapDown = false; TouchInput.BuyDown = false;
        bool f = false, u = false, sc = false, moveFound = false, lookFound = false;
        for (int i = 0; i < Input.touchCount; i++) {
            Touch t = Input.GetTouch(i); Vector2 p = t.position;
            bool began = t.phase == TouchPhase.Began;
            bool ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            if (t.fingerId == moveId) { if (ended) moveId = -1; else { movePos = p; moveFound = true; continue; } }
            if (t.fingerId == lookId) { if (ended) lookId = -1; else { TouchInput.Look += t.deltaPosition * .075f * sens; lookFound = true; continue; } }
            if (ended) continue;
            if (fire.Contains(p)) { f = true; continue; }
            if (use.Contains(p)) { u = true; continue; }
            if (score.Contains(p)) { sc = true; continue; }
            if (began) {
                if (jump.Contains(p)) { TouchInput.JumpDown = true; continue; }
                if (reload.Contains(p)) { TouchInput.ReloadDown = true; continue; }
                if (swap.Contains(p)) { TouchInput.SwapDown = true; continue; }
                if (buy.Contains(p)) { TouchInput.BuyDown = true; continue; }
                if (p.x < Screen.width * .42f && moveId == -1) { moveId = t.fingerId; moveOrigin = p; movePos = p; moveFound = true; }
                else if (p.x >= Screen.width * .42f && lookId == -1) { lookId = t.fingerId; lookFound = true; }
            }
        }
        if (!moveFound) moveId = -1;
        if (!lookFound) lookId = -1;
        Vector2 d = (movePos - moveOrigin) / (70f * S);
        d = Vector2.ClampMagnitude(d, 1f);
        if (d.magnitude < .12f) d = Vector2.zero;
        TouchInput.Move = moveId >= 0 ? d : Vector2.zero;
        TouchInput.Fire = f; TouchInput.Use = u; TouchInput.ScoreHeld = sc;
    }

    Rect G(Rect r) { return new Rect(r.x, Screen.height - r.yMax, r.width, r.height); }

    void Btn(Rect r, string t, float a) {
        GUI.color = new Color(1, 1, 1, a); GUI.DrawTexture(G(r), circle);
        GUI.color = Color.white; GUI.Label(G(r), t, st);
    }

    void OnGUI() {
        if (!Application.isMobilePlatform) return;
        if (HUD.I != null && HUD.I.buyOpen) return;
        GUI.matrix = Matrix4x4.identity; Layout();
        if (st == null) {
            st = new GUIStyle(GUI.skin.label); st.alignment = TextAnchor.MiddleCenter; st.fontStyle = FontStyle.Bold; st.normal.textColor = Color.white;
        }
        st.fontSize = (int)(18 * S);
        Btn(fire, "FIRE", .35f); Btn(jump, "JUMP", .25f); Btn(reload, "R", .25f); Btn(swap, "SWAP", .25f);
        Btn(use, "PLANT\nDEFUSE", .3f); Btn(score, "TAB", .2f); Btn(buy, "BUY", .2f);
        if (moveId >= 0) {
            float s = S, cx = moveOrigin.x, cy = Screen.height - moveOrigin.y;
            GUI.color = new Color(1, 1, 1, .2f); GUI.DrawTexture(new Rect(cx - 70 * s, cy - 70 * s, 140 * s, 140 * s), circle);
            Vector2 d = Vector2.ClampMagnitude(movePos - moveOrigin, 70f * s);
            GUI.color = new Color(1, 1, 1, .45f); GUI.DrawTexture(new Rect(cx + d.x - 30 * s, cy - d.y - 30 * s, 60 * s, 60 * s), circle);
            GUI.color = Color.white;
        }
    }
}
