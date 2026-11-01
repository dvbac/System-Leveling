#if UNITY_EDITOR
using System;
using UnityEngine;

// Regression kills must never award or overwrite the player's real persistent balance.
public class SoulTestIsolation : MonoBehaviour
{
    private IDisposable save;
    private void Awake() => save = SoulWallet.UseTestSave();
    private void OnDestroy() => save?.Dispose();
}
#endif
