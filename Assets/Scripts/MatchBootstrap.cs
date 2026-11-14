using System.Collections.Generic;
using UnityEngine;

// สร้างแผนที่ ผู้เล่น บอท และ UI ทั้งหมดตอนเริ่ม Scene "Match"
public class MatchBootstrap : MonoBehaviour {
    WeaponData[] shop;

    void Awake() {
        Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep; Input.simulateMouseWithTouches = false;
        Physics.IgnoreLayerCollision(Util.HitLayer, 2, true); Physics.IgnoreLayerCollision(Util.HitLayer, Util.HitLayer, true); Physics.IgnoreLayerCollision(Util.HitLayer, 0, true);
        shop = WeaponDB.Create();
        var gm = gameObject.AddComponent<GameManager>(); gm.shop = shop;

        List<Vector3> atk, def; Vector3[] siteC;
        MapGrid.BuildWorld(transform, out atk, out def, out siteC);
        gm.attackSpawns = atk; gm.defendSpawns = def;
        gm.sites = new SpikeSite[2];
        for (int i = 0; i < 2; i++) {
            var go = new GameObject("Site " + (i == 0 ? "A" : "B")); go.transform.SetParent(transform, false); go.transform.position = siteC[i];
            var s = go.AddComponent<SpikeSite>(); s.siteName = i == 0 ? "A" : "B"; gm.sites[i] = s;
        }

        gm.local = MakeActor("You", 0, true); gm.players.Add(gm.local);
        for (int i = 1; i < 5; i++) gm.players.Add(MakeActor("Ally " + i, 0, false));
        for (int i = 1; i <= 5; i++) gm.players.Add(MakeActor("Foe " + i, 1, false));

        var hud = gameObject.AddComponent<HUD>(); hud.local = gm.local;
        gameObject.AddComponent<TouchControls>();
    }

    void Zone(Transform root, Health hp, string n, HitZoneType z, Vector3 pos, Vector3 size, bool sphere) {
        var c = new GameObject(n); c.layer = Util.HitLayer; c.transform.SetParent(root, false); c.transform.localPosition = pos;
        if (sphere) { var sc = c.AddComponent<SphereCollider>(); sc.radius = size.x; }
        else { var bc = c.AddComponent<BoxCollider>(); bc.size = size; }
        var hz = c.AddComponent<HitZone>(); hz.zone = z; hz.owner = hp;
    }

    GameManager.PlayerState MakeActor(string name, int teamIdx, bool isPlayer) {
        var go = new GameObject(name); go.layer = 2; go.transform.SetParent(transform, false);
        var cc = go.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .35f; cc.center = new Vector3(0, .9f, 0); cc.stepOffset = .3f; cc.skinWidth = .04f;
        var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var hp = go.AddComponent<Health>(); hp.displayName = name;
        Zone(go.transform, hp, "Head", HitZoneType.Head, new Vector3(0, 1.68f, 0), new Vector3(.22f, 0, 0), true);
        Zone(go.transform, hp, "Body", HitZoneType.Body, new Vector3(0, 1.15f, 0), new Vector3(.62f, .75f, .38f), false);
        Zone(go.transform, hp, "Legs", HitZoneType.Leg, new Vector3(0, .39f, 0), new Vector3(.5f, .78f, .34f), false);
        var ps = new GameManager.PlayerState { name = name, go = go, health = hp, cc = cc, teamIndex = teamIdx };

        if (isPlayer) {
            var camGO = new GameObject("Cam"); camGO.tag = "MainCamera"; camGO.transform.SetParent(go.transform, false); camGO.transform.localPosition = new Vector3(0, 1.6f, 0);
            var cam = camGO.AddComponent<Camera>(); cam.fieldOfView = 85f; cam.nearClipPlane = .05f; cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.6f, .72f, .85f);
            camGO.AddComponent<AudioListener>();
            var lightGO = new GameObject("Muzzle"); lightGO.transform.SetParent(camGO.transform, false); lightGO.transform.localPosition = new Vector3(.2f, -.1f, .7f);
            var light = lightGO.AddComponent<Light>(); light.type = LightType.Point; light.range = 8f; light.intensity = 2.5f; light.color = new Color(1f, .8f, .5f); light.enabled = false;
            var vm = new GameObject("ViewModel").transform; vm.SetParent(camGO.transform, false); vm.localPosition = new Vector3(.22f, -.2f, .5f);
            Util.Prim(PrimitiveType.Cube, vm, Vector3.zero, new Vector3(.07f, .1f, .55f), new Color(.12f, .12f, .14f), false, false);
            Util.Prim(PrimitiveType.Cube, vm, new Vector3(0, -.09f, -.12f), new Vector3(.05f, .12f, .08f), new Color(.2f, .2f, .22f), false, false);
            var pc = go.AddComponent<PlayerController>(); pc.cam = camGO.transform;
            var wc = go.AddComponent<WeaponController>();
            wc.cam = cam; wc.player = pc; wc.self = hp; wc.muzzle = light; wc.viewmodel = vm; wc.sidearm = shop[0];
            var src = go.AddComponent<AudioSource>(); src.spatialBlend = 0f; wc.audioSrc = src;
            ps.ctrl = pc; ps.weapons = wc;
        } else {
            Color tc = teamIdx == 0 ? new Color(.2f, .45f, .9f) : new Color(.85f, .25f, .22f);
            Util.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, 1.1f, 0), new Vector3(.6f, .4f, .4f), tc, false, true);
            Util.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, .4f, 0), new Vector3(.5f, .4f, .3f), new Color(.15f, .15f, .18f), false, true);
            Util.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.68f, 0), Vector3.one * .3f, new Color(.9f, .75f, .6f), false, true);
            Util.Prim(PrimitiveType.Cube, go.transform, new Vector3(.25f, 1.2f, .35f), new Vector3(.07f, .1f, .5f), new Color(.1f, .1f, .1f), false, true);
            var eye = new GameObject("Eye").transform; eye.SetParent(go.transform, false); eye.localPosition = new Vector3(0, 1.55f, 0);
            var src = go.AddComponent<AudioSource>(); src.spatialBlend = 1f; src.maxDistance = 60f; src.rolloffMode = AudioRolloffMode.Linear;
            var bot = go.AddComponent<BotAI>(); bot.self = hp; bot.eye = eye; bot.weapon = shop[0]; bot.skill = Random.Range(.35f, .7f);
            ps.bot = bot;
        }
        return ps;
    }
}
