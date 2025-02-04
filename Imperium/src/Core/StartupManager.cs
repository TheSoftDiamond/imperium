using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Imperium.Core;

public class StartupManager
{
    private bool HasLaunched;

    internal StartupManager()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (Imperium.Settings.Preferences.QuickloadSkipSplash.Value) RunSplashRemover();
    }

    internal void ExecuteAutoLaunch()
    {
        Imperium.IO.LogInfo("quickload auto launch 1");
        if (HasLaunched) return;
        HasLaunched = true;

        Imperium.IO.LogInfo("quickload auto launch 2");

        switch (Imperium.Settings.Preferences.QuickloadLaunchMode.Value)
        {
            case LaunchMode.LAN:
                Imperium.IO.LogInfo("quickload loadng lan");
                SceneManager.LoadScene("InitSceneLANMode");
                break;
            case LaunchMode.Online:
            default:
                Imperium.IO.LogInfo("quickload loadng online");
                SceneManager.LoadScene("InitScene");
                break;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        switch (scene.name) {
            case "InitScene":
            case "InitSceneLANMode":
                SkipBootAnimation();
                break;

            case "MainMenu":
                SkipMenuAnimation();
                SkipLanPopup();
                break;
        }
    }

    private static void SkipBootAnimation()
    {
        if (!Imperium.Settings.Preferences.QuickloadSkipSplash.Value) return;

        var game = Object.FindObjectOfType<InitializeGame>();
        if (game == null) return;

        game.runBootUpScreen = false;
        game.bootUpAnimation = null;
        game.bootUpAudio = null;
    }

    private static void SkipLanPopup()
    {
        if (!Imperium.Settings.Preferences.QuickloadSkipSplash.Value) return;

        var obj = Object.FindObjectOfType<MenuManager>();
        if (obj == null) return;

        Object.Destroy(obj.lanWarningContainer);
    }

    private static void SkipMenuAnimation()
    {
        if (!Imperium.Settings.Preferences.QuickloadSkipSplash.Value) return;

        GameNetworkManager.Instance.firstTimeInMenu = false;
    }

    private void RunSplashRemover()
    {
        Task.Factory.StartNew(() => {
            while (!HasLaunched) {
                SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
                if (Time.realtimeSinceStartup < 15) continue;
                break;
            }
        });
    }
}

internal enum LaunchMode
{
    Online,
    LAN
}