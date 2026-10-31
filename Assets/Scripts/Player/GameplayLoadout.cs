using System;
using UnityEngine;

// Keep the scene's Player identity: camera, HUD, spawner and input already point to it.
[DefaultExecutionOrder(-200)]
public class GameplayLoadout : MonoBehaviour
{
    private void Awake()
    {
        if (LoadoutLauncher.Character == null) return;
        PlayerStats stats = GetComponent<PlayerStats>();
        WeaponHolder holder = GetComponentInChildren<WeaponHolder>(true);
        if (stats == null || holder == null || LoadoutLauncher.Weapon == null)
            throw new InvalidOperationException("Gameplay Player is missing its stats, hand anchor or selected weapon.");

        GameObject source = LoadoutLauncher.CharacterPrefab;
        if (source != null)
        {
            SpriteRenderer appearance = source.GetComponent<SpriteRenderer>();
            Animator animation = source.GetComponent<Animator>();
            CapsuleCollider2D body = source.GetComponent<CapsuleCollider2D>();
            WeaponHolder hand = source.GetComponentInChildren<WeaponHolder>(true);
            if (appearance == null || animation == null || body == null || hand == null)
                throw new InvalidOperationException("Selected character prefab is missing its appearance or calibration.");
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = appearance.sprite;
            renderer.color = appearance.color;
            renderer.flipX = appearance.flipX; renderer.flipY = appearance.flipY;
            GetComponent<Animator>().runtimeAnimatorController = animation.runtimeAnimatorController;
            CapsuleCollider2D collider = GetComponent<CapsuleCollider2D>();
            collider.size = body.size; collider.offset = body.offset; collider.direction = body.direction;
            transform.localScale = source.transform.localScale;
            holder.transform.SetLocalPositionAndRotation(hand.transform.localPosition, hand.transform.localRotation);
            holder.transform.localScale = hand.transform.localScale;
        }
        // This runs before PlayerStats.Awake and WeaponHolder.Awake; the default must not replace the selection.
        stats.characterData = LoadoutLauncher.Character;
        if (!holder.Equip(LoadoutLauncher.Weapon))
            throw new InvalidOperationException("Could not equip the selected weapon.");
    }
}
