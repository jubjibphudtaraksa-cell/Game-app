using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour {
    public static int mode = 5;   // 5 = 5v5, 2 = 2v2, 1 = 1v1
    enum Page { Main, Play, Settings }
    Page page = Page.Main; GUIStyle lbl, btn; float sens, vol;

    void Awake() {
        sens = PlayerPrefs.GetFloat("sens", 2f); vol = PlayerPrefs.GetFloat("vol", 1f);
        AudioListener.volume = vol; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void OnGUI() {
        if (lbl == null) {
            lbl = new GUIStyle(GUI.skin.label); lbl.alignment = TextAnchor.MiddleCenter; lbl.fontStyle = FontStyle.Bold; lbl.normal.textColor = Color.white;
            btn = new GUIStyle(GUI.skin.button); btn.fontSize = 24; btn.fontStyle = FontStyle.Bold;
        }
        float s = Screen.height / 720f; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s;
        lbl.fontSize = 56; GUI.Label(new Rect(0, 40, W, 90), "IRONLINE", lbl);
        lbl.fontSize = 18; GUI.Label(new Rect(0, 120, W, 30), "TACTICAL FPS  -  GUNPLAY & TEAMWORK", lbl);
        float x = W / 2 - 160;
        if (page == Page.Main) {
            if (GUI.Button(new Rect(x, 220, 320, 70), "PLAY", btn)) page = Page.Play;
            if (GUI.Button(new Rect(x, 310, 320, 70), "SETTINGS", btn)) page = Page.Settings;
            if (GUI.Button(new Rect(x, 400, 320, 70), "QUIT", btn)) Application.Quit();
        } else if (page == Page.Play) {
            lbl.fontSize = 20; GUI.Label(new Rect(0, 180, W, 30), "CREATE ROOM (vs bots)", lbl);
            if (GUI.Button(new Rect(x, 220, 320, 70), "5 vs 5", btn)) Go(5);
            if (GUI.Button(new Rect(x, 310, 320, 70), "2 vs 2", btn)) Go(2);
            if (GUI.Button(new Rect(x, 400, 320, 70), "1 vs 1", btn)) Go(1);
            if (GUI.Button(new Rect(x, 500, 320, 60), "BACK", btn)) page = Page.Main;
        } else {
            lbl.fontSize = 22;
            GUI.Label(new Rect(x, 200, 320, 30), "SENSITIVITY  " + sens.ToString("0.0"), lbl);
            sens = GUI.HorizontalSlider(new Rect(x, 240, 320, 30), sens, .5f, 6f);
            GUI.Label(new Rect(x, 290, 320, 30), "VOLUME  " + Mathf.RoundToInt(vol * 100) + "%", lbl);
            vol = GUI.HorizontalSlider(new Rect(x, 330, 320, 30), vol, 0f, 1f);
            if (GUI.Button(new Rect(x, 420, 320, 70), "SAVE & BACK", btn)) {
                PlayerPrefs.SetFloat("sens", sens); PlayerPrefs.SetFloat("vol", vol); PlayerPrefs.Save();
                AudioListener.volume = vol; page = Page.Main;
            }
        }
    }

    void Go(int m) { mode = m; SceneManager.LoadScene("Match"); }
}
