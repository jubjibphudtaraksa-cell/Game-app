using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// รันด้วย game-ci: buildMethod = ProjectSetup.Generate
public static class ProjectSetup {
    public static void Generate() {
        Directory.CreateDirectory("Assets/Scenes");

        var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.05f, .07f, .1f);
        camGO.AddComponent<AudioListener>();
        new GameObject("Menu").AddComponent<MenuUI>();
        EditorSceneManager.SaveScene(menu, "Assets/Scenes/Menu.unity");

        var match = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Sun"); var l = sun.AddComponent<Light>();
        l.type = LightType.Directional; l.intensity = 1.15f; l.shadows = LightShadows.Soft; l.color = new Color(1f, .96f, .88f);
        sun.transform.rotation = Quaternion.Euler(52, -35, 0);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.45f, .5f, .58f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .006f; RenderSettings.fogColor = new Color(.6f, .72f, .85f);
        new GameObject("Bootstrap").AddComponent<MatchBootstrap>();
        EditorSceneManager.SaveScene(match, "Assets/Scenes/Match.unity");

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Match.unity", true)
        };

        PlayerSettings.companyName = "FIATX2 HUB"; PlayerSettings.productName = "Ironline";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.fiatx2hub.ironline");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.bundleVersion = "0.1.0";
        AssetDatabase.SaveAssets();
        Debug.Log("ProjectSetup: scenes + settings generated");
    }
}
