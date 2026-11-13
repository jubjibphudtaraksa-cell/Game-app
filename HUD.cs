using UnityEngine; using UnityEngine.UI; using TMPro;
public class HUD : MonoBehaviour {
    public static HUD I; void Awake(){ I = this; }
    public TMP_Text timerText, scoreText, hpText, armorText, ammoText, moneyText, killFeedText, resultText;
    public Image progressBar, hitMarker; public GameObject buyMenu, scoreboard;
    public Health localHealth; public WeaponController localWeapon; public GameManager.PlayerState local;
    void Update() {
        var g = GameManager.I; if (g == null) return;
        timerText.text = $"{g.phase}  {Mathf.Max(0, Mathf.CeilToInt(g.timer))}s";
        scoreText.text = $"{g.score[0]} : {g.score[1]}";
        hpText.text = localHealth.hp.ToString(); armorText.text = localHealth.armor.ToString();
        ammoText.text = $"{localWeapon.Mag} / {localWeapon.Reserve}"; moneyText.text = "$" + local.money;
        if (scoreboard) scoreboard.SetActive(Input.GetKey(KeyCode.Tab));
        if (hitMarker && hitMarker.color.a > 0) { var c = hitMarker.color; c.a -= Time.deltaTime * 4; hitMarker.color = c; }
        if (Input.GetKeyDown(KeyCode.B) && g.phase == Phase.Buy) ShowBuyMenu(!buyMenu.activeSelf);
    }
    public void ShowBuyMenu(bool on){ buyMenu.SetActive(on); Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked; }
    public void HitMarker(bool head){ hitMarker.color = head ? Color.red : Color.white; }
    public void SetProgress(float v){ if (progressBar) progressBar.fillAmount = v; }
    public void AddKillFeed(string k, string v, string w){ killFeedText.text = $"{k} [{w}] {v}\n" + killFeedText.text; Invoke(nameof(TrimFeed), 5f); }
    void TrimFeed(){ int i = killFeedText.text.LastIndexOf('\n', killFeedText.text.Length - 2); killFeedText.text = i > 0 ? killFeedText.text.Substring(0, i + 1) : ""; }
    public void ShowRoundResult(Team w, string reason, int[] s){ resultText.text = $"{w} win — {reason}"; }
    public void ShowMatchEnd(int winnerIdx){ resultText.text = winnerIdx == 0 ? "VICTORY" : "DEFEAT"; }
}
