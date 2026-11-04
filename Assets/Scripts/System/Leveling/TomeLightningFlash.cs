using UnityEngine;

public static class TomeLightningFlash
{
    public static void Create(EnemyStats target)
    {
        var sprite = target.GetComponent<SpriteRenderer>();
        if (sprite == null) return;
        var go = new GameObject("Tome Lightning");
        go.transform.position = target.transform.position;
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = sprite.sharedMaterial;
        line.startColor = line.endColor = new Color(0.6f, 0.9f, 1f);
        line.startWidth = line.endWidth = 0.07f; line.sortingOrder = sprite.sortingOrder + 5;
        line.useWorldSpace = false; line.positionCount = 5;
        line.SetPositions(new[] { new Vector3(0, 2.4f), new Vector3(-0.2f, 1.7f), new Vector3(0.16f, 1.2f), new Vector3(-0.12f, 0.6f), Vector3.zero });
        Object.Destroy(go, 0.18f);
    }
}
