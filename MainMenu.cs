using UnityEngine; using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour {
    public static int mode = 5; // 5v5, 2v2, 1v1
    public void Play(int m){ mode = m; SceneManager.LoadScene("Match"); }   // ปุ่ม 5v5 / 2v2 / 1v1
    public void Quit(){ Application.Quit(); }
    public void SetSens(float v){ PlayerPrefs.SetFloat("sens", v); }
    public void SetVolume(float v){ AudioListener.volume = v; }
}
