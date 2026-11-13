using System.Collections.Generic;
using UnityEngine;
public enum Phase { Buy, Live, PostRound, MatchEnd }

public class GameManager : MonoBehaviour {
    public static GameManager I;
    public int roundsToWin = 13, halftimeAfter = 12;
    public float buyTime = 30, roundTime = 100, spikeTime = 45, postTime = 5;
    public int startMoney = 800, maxMoney = 9000, winReward = 3000, lossReward = 1900, lossBonus = 500, plantReward = 300, killReward = 200;
    public List<PlayerState> players = new();
    public Transform[] attackSpawns, defendSpawns;
    public Phase phase; public float timer; public int round = 1;
    public int[] score = new int[2]; int[] lossStreak = new int[2];
    public Team attackerSide = Team.Attackers; // ฝั่งที่ "ทีม 0" เล่นอยู่
    public bool attackerHasSpike = true, spikePlanted; SpikeSite plantedSite;
    public bool IsLive => phase == Phase.Live;

    [System.Serializable] public class PlayerState { public Health health; public WeaponController weapons; public PlayerController ctrl; public int money, teamIndex; }
    void Awake(){ I = this; }
    void Start(){ foreach (var p in players) { p.money = startMoney; p.health.OnDied += OnKill; } StartRound(); }

    void StartRound() {
        phase = Phase.Buy; timer = buyTime; spikePlanted = false; attackerHasSpike = true;
        foreach (var s in FindObjectsOfType<SpikeSite>()) s.ResetSite();
        int a = 0, d = 0;
        foreach (var p in players) {
            p.health.team = p.teamIndex == 0 ? attackerSide : Opp(attackerSide);
            var sp = p.health.team == Team.Attackers ? attackSpawns[a++ % attackSpawns.Length] : defendSpawns[d++ % defendSpawns.Length];
            var cc = p.ctrl.GetComponent<CharacterController>(); cc.enabled = false; p.ctrl.transform.SetPositionAndRotation(sp.position, sp.rotation); cc.enabled = true;
            p.health.ResetFor(100, p.health.armor); p.ctrl.controlsEnabled = false; // ล็อกตอน Buy
        }
        HUD.I?.ShowBuyMenu(true);
    }
    void Update() {
        timer -= Time.deltaTime;
        if (phase == Phase.Buy && timer <= 0) { phase = Phase.Live; timer = roundTime; HUD.I?.ShowBuyMenu(false); foreach (var p in players) p.ctrl.controlsEnabled = true; }
        else if (phase == Phase.Live) {
            if (spikePlanted && timer <= 0) EndRound(attackerSide, "Spike exploded");
            else if (!spikePlanted && timer <= 0) EndRound(Opp(attackerSide), "Time out");
            else CheckElimination();
        } else if (phase == Phase.PostRound && timer <= 0) NextRound();
    }
    void CheckElimination() {
        int atk = 0, def = 0;
        foreach (var p in players) if (p.health.Alive) { if (p.health.team == Team.Attackers) atk++; else def++; }
        if (def == 0) EndRound(Team.Attackers, "Defenders eliminated");
        else if (atk == 0 && !spikePlanted) EndRound(Team.Defenders, "Attackers eliminated");
    }
    public void OnPlanted(SpikeSite s){ spikePlanted = true; plantedSite = s; timer = spikeTime; foreach (var p in players) if (p.health.team == Team.Attackers) p.money += plantReward; }
    public void OnDefused(){ EndRound(Team.Defenders, "Spike defused"); }
    void OnKill(Health v, Health k, string w) { HUD.I?.AddKillFeed(k ? k.name : "World", v.name, w); var ks = players.Find(x => x.health == k); if (ks != null && k.team != v.team) ks.money = Mathf.Min(maxMoney, ks.money + killReward); }

    void EndRound(Team winner, string reason) {
        if (phase != Phase.Live) return; phase = Phase.PostRound; timer = postTime;
        int wi = winner == attackerSide ? 0 : 1; score[wi]++; lossStreak[wi] = 0; lossStreak[1 - wi] = Mathf.Min(lossStreak[1 - wi] + 1, 4);
        foreach (var p in players) {
            bool won = p.teamIndex == wi;
            p.money = Mathf.Min(maxMoney, p.money + (won ? winReward : lossReward + lossBonus * lossStreak[p.teamIndex]));
        }
        HUD.I?.ShowRoundResult(winner, reason, score);
        if (score[wi] >= roundsToWin) { phase = Phase.MatchEnd; HUD.I?.ShowMatchEnd(wi); }
    }
    void NextRound() {
        round++;
        if (round - 1 == halftimeAfter) { attackerSide = Opp(attackerSide); foreach (var p in players) { p.money = startMoney; p.weapons.primary = null; p.health.armor = 0; } }
        StartRound();
    }
    // เรียกจาก Buy Menu
    public bool TryBuy(PlayerState p, WeaponData w) { if (phase != Phase.Buy || p.money < w.cost) return false; p.money -= w.cost; p.weapons.Buy(w); return true; }
    public bool TryBuyArmor(PlayerState p, int cost = 650){ if (phase != Phase.Buy || p.money < cost) return false; p.money -= cost; p.health.armor = 50; return true; }
    static Team Opp(Team t) => t == Team.Attackers ? Team.Defenders : Team.Attackers;
}
