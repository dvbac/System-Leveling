using UnityEngine;
using UnityEngine.UI;

public class SoulBalanceView : MonoBehaviour
{
    public Text caption, amount;
    [SerializeField] private bool compact;
    private int lastTotal = -1;
    private bool lastUnsaved;
    private GameOverController run;

    private void OnEnable() { run = GetComponentInParent<GameOverController>(); lastTotal = -1; LateUpdate(); }
    private void LateUpdate()
    {
        if (caption == null || amount == null) return;
        int total = run != null ? run.SoulEarned : SoulWallet.Total; bool unsaved = SoulWallet.HasUnsavedChanges;
        if (total == lastTotal && unsaved == lastUnsaved) return;
        caption.text = unsaved ? "SOUL / SAVE FAILED" : "SOUL";
        amount.text = total.ToString("N0");
        if (compact)
        {
            Vector2 position = amount.rectTransform.anchoredPosition;
            position.x = caption.preferredWidth + 6;
            amount.rectTransform.anchoredPosition = position;
        }
        lastTotal = total; lastUnsaved = unsaved;
    }

    private void OnDisable() => SoulWallet.Save();
    private void OnApplicationPause(bool paused) { if (paused) SoulWallet.Save(); }
    private void OnApplicationQuit() => SoulWallet.Save();
}
