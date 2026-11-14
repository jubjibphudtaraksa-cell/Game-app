using System.Collections.Generic;
using UnityEngine;

public enum Phase { Buy, Live, PostRound, MatchEnd }

public class GameManager : MonoBehaviour {
    public static GameManager I;
    public int roundsToWin = 13, halftimeAfter = 12;
    public float buyTime = 25f, roundTime = 100f, spikeTime = 45f, postTime = 5f;
    public int startMoney = 800, maxMoney = 9000, winReward = 3000, lossReward = 1900, lossBonus = 500, plantReward = 300, killReward = 200;
    public List<PlayerState> players = new List<PlayerState>();
    public PlayerState local; public WeaponData[] shop; public SpikeSite[] sites;
    public List<Vector3> attackSpawns, defendSpawns;
    public Phase phase; public float timer; public int round = 1;
    public int[] score = new int[2]; int[] lossStreak = new int[2];
    public bool teamZeroAttacks = true, spikePlanted; public SpikeSite plantedSite;
    int attackSiteIdx; bool halfFlag;
    public bool IsLive { get { return phase == Phase.Live; } }

    [System.Serializable]
    public class PlayerState {
        public string name; public GameObject go; public Health health; public WeaponController weapons; public PlayerController ctrl; public BotAI bot; public CharacterController cc;
        public int money, teamIndex, kills, deaths;
    }

    void Awake() { I = this; }

    void Start() {
        int per = MenuUI.mode; var keep = new List<PlayerState>(); int[] cnt = new int[2];
        foreach (var p in players) {
            if (cnt[p.teamIndex] < per) { cnt[p.teamIndex]++; keep.Add(p); } else p.go.SetActive(false);
        }
        players = keep;
        foreach (var p in players) { p.money = startMoney; var ps = p; p.health.OnDied += (v, k, w) => OnKill(ps, v, k, w); }
        StartRound();
    }

    Team TeamOf(PlayerState p) { return (p.teamIndex == 0) == teamZeroAttacks ? Team.Attackers : Team.Defenders; }

    void StartRound() {
        phase = Phase.Buy; timer = buyTime; spikePlanted = false; plantedSite = null;
        foreach (var s in sites) s.ResetSite();
        attackSiteIdx = Random.Range(0, sites.Length);
        int a = 0, d = 0, di = 0;
        foreach (var p in players) {
            bool wasAlive = p.health.Alive; Team t = TeamOf(p);
            int keepArmor = (wasAlive && !halfFlag) ? p.health.armor : 0;
            bool lose = !wasAlive || halfFlag;
            p.health.team = t; p.go.SetActive(true);
            Vector3 sp = t == Team.Attackers ? attackSpawns[a++ % attackSpawns.Count] : defendSpawns[d++ % defendSpawns.Count];
            Quaternion rot = t == Team.Attackers ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            p.cc.enabled = false; p.go.transform.SetPositionAndRotation(sp, rot); p.cc.enabled = true;
            p.health.ResetFor(100, keepArmor);
            if (p.weapons != null) p.weapons.RoundReset(lose);
            if (p.ctrl != null) { p.ctrl.controlsEnabled = false; p.ctrl.ResetLook(); }
            if (p.bot != null) {
                p.bot.enabledAI = false; p.bot.RoundReset(lose); BotBuy(p);
                p.bot.SetupRound(t == Team.Attackers ? attackSiteIdx : (di++ % sites.Length));
            }
        }
        halfFlag = false;
        if (HUD.I != null) { HUD.I.ClearResult(); HUD.I.ShowBuyMenu(true); }
    }

    void BotBuy(PlayerState p) {
        int m = p.money;
        if (p.health.armor < 50 && m >= 1900) { m -= 650; p.health.armor = 50; }
        if (p.bot.weapon.isSidearm) {
            var opts = new List<WeaponData>();
            foreach (var w in shop) { if (w.isSidearm || w.cost > m) continue; if (w.damage > 100 && Random.value < .7f) continue; opts.Add(w); }
            if (opts.Count > 0 && Random.value < .85f) {
                opts.Sort((x, y) => y.cost.CompareTo(x.cost));
                var best = opts[Mathf.Min(Random.Range(0, 2), opts.Count - 1)];
                m -= best.cost; p.bot.weapon = best;
            }
        }
        p.money = m;
    }

    void Update() {
        if (phase == Phase.MatchEnd) return;
        timer -= Time.deltaTime;
        if (phase == Phase.Buy && timer <= 0) BeginLive();
        else if (phase == Phase.Live) {
            if (timer <= 0) { if (spikePlanted) EndRound(Team.Attackers, "Spike detonated"); else EndRound(Team.Defenders, "Time expired"); }
            else CheckElim();
        } else if (phase == Phase.PostRound && timer <= 0) NextRound();
    }

    void BeginLive() {
        phase = Phase.Live; timer = roundTime;
        if (HUD.I != null) HUD.I.ShowBuyMenu(false);
        foreach (var p in players) { if (p.ctrl != null) p.ctrl.controlsEnabled = true; if (p.bot != null) p.bot.enabledAI = true; }
    }

    public void ReadyUp() { if (phase == Phase.Buy) timer = 0f; }

    void CheckElim() {
        int atk = 0, def = 0;
        foreach (var p in players) if (p.health.Alive) { if (p.health.team == Team.Attackers) atk++; else def++; }
        if (def == 0) EndRound(Team.Attackers, "Defenders eliminated");
        else if (atk == 0 && !spikePlanted) EndRound(Team.Defenders, "Attackers eliminated");
    }

    public void OnPlanted(SpikeSite s, PlayerState actor) {
        spikePlanted = true; plantedSite = s; timer = spikeTime;
        foreach (var p in players) if (p.health.team == Team.Attackers) p.money = Mathf.Min(maxMoney, p.money + plantReward);
        if (HUD.I != null) HUD.I.AddKillFeed("Spike planted at site " + s.siteName);
    }
    public void OnDefused(PlayerState actor) { EndRound(Team.Defenders, "Spike defused"); }

    void OnKill(PlayerState victim, Health v, Health k, string w) {
        victim.deaths++;
        PlayerState killer = players.Find(x => x.health == k);
        if (killer != null && killer != victim) { killer.kills++; killer.money = Mathf.Min(maxMoney, killer.money + killReward); }
        if (HUD.I != null) HUD.I.AddKillFeed((k != null ? k.displayName : "World") + "  [" + w + "]  " + v.displayName);
        if (victim.bot != null) victim.go.SetActive(false);
    }

    void EndRound(Team winner, string reason) {
        if (phase != Phase.Live) return;
        phase = Phase.PostRound; timer = postTime;
        int wi = (winner == Team.Attackers) == teamZeroAttacks ? 0 : 1;
        score[wi]++; lossStreak[wi] = 0; lossStreak[1 - wi] = Mathf.Min(lossStreak[1 - wi] + 1, 4);
        foreach (var p in players) {
            int add = p.teamIndex == wi ? winReward : lossReward + lossBonus * (lossStreak[p.teamIndex] - 1);
            p.money = Mathf.Min(maxMoney, p.money + add);
            if (p.bot != null) p.bot.enabledAI = false;
        }
        if (HUD.I != null) HUD.I.ShowRoundResult((winner == Team.Attackers ? "ATTACKERS" : "DEFENDERS") + " WIN  -  " + reason);
        if (score[wi] >= roundsToWin) {
            phase = Phase.MatchEnd;
            if (HUD.I != null) HUD.I.ShowRoundResult(wi == local.teamIndex ? "VICTORY" : "DEFEAT");
        }
    }

    void NextRound() {
        round++;
        if (round - 1 == halftimeAfter) {
            teamZeroAttacks = !teamZeroAttacks; halfFlag = true; lossStreak[0] = 0; lossStreak[1] = 0;
            foreach (var p in players) p.money = startMoney;
        }
        StartRound();
    }

    public bool TryBuy(PlayerState p, WeaponData w) {
        if (phase != Phase.Buy || p.money < w.cost || p.weapons.Owns(w)) return false;
        p.money -= w.cost; p.weapons.Buy(w); return true;
    }
    public bool TryBuyArmor(PlayerState p) {
        if (phase != Phase.Buy || p.money < 650 || p.health.armor >= 50) return false;
        p.money -= 650; p.health.armor = 50; return true;
    }
}
