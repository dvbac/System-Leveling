#if UNITY_EDITOR
using UnityEngine;

public static class GameplayRegressionBaseline
{
    public static void Prepare()
    {
        if (!Application.isPlaying) return;
        PlayerController main = GameObject.Find("Player")?.GetComponent<PlayerController>();
        if (main == null) return;
        // Preview characters may coexist in Edit Mode; single-player tests isolate the authored main Player.
        foreach (PlayerController other in Object.FindObjectsByType<PlayerController>())
            if (other != main) other.gameObject.SetActive(false);
        main.SendMessage("Awake"); // Re-select the singleton after all scene actors have run Awake.
    }
}
#endif
