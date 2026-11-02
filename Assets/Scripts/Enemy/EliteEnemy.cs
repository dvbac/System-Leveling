using UnityEngine;
using UnityEngine.Rendering;

// Runtime-only variant: prefab art, animation, combat data and drops remain unchanged.
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStats), typeof(SpriteRenderer))]
[DefaultExecutionOrder(100)]
public class EliteEnemy : MonoBehaviour
{
    private SpriteRenderer visual;
    private readonly SpriteRenderer[] outline = new SpriteRenderer[8];
    private Material outlineMaterial;
    private bool configured;
    public float SizeMultiplier { get; private set; } = 1f;

    public void Configure(EliteSpawnSettings settings)
    {
        if (configured) return;
        configured = true;
        settings.Validate();
        EnemyStats stats = GetComponent<EnemyStats>();
        stats.currentHealth *= settings.healthMultiplier;
        stats.currentMoveSpeed *= settings.speedMultiplier;
        stats.currentDamage *= settings.damageMultiplier;
        SizeMultiplier = settings.sizeMultiplier;
        transform.localScale *= SizeMultiplier;
        gameObject.name = "Elite " + gameObject.name;
        visual = GetComponent<SpriteRenderer>();
        // Keep outlines behind the body, but above terrain, as one sorted actor.
        SortingGroup group = gameObject.AddComponent<SortingGroup>();
        group.sortingLayerID = visual.sortingLayerID;
        group.sortingOrder = visual.sortingOrder;
        Shader shader = Resources.Load<Shader>("EliteSilhouette");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogError("Elite silhouette shader is missing or unsupported.", this);
            return;
        }
        outlineMaterial = new Material(shader);
        for (int i = 0; i < outline.Length; i++)
        {
            var child = new GameObject("Elite Red Outline", typeof(SpriteRenderer));
            child.transform.SetParent(transform, false);
            outline[i] = child.GetComponent<SpriteRenderer>();
            outline[i].sharedMaterial = outlineMaterial;
        }
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (visual == null || outlineMaterial == null) return;
        float width = visual.sprite != null ? 1f / visual.sprite.pixelsPerUnit : 0.04f;
        for (int i = 0; i < outline.Length; i++)
        {
            SpriteRenderer edge = outline[i];
            float angle = i * Mathf.PI / 4f;
            edge.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * width;
            edge.sprite = visual.sprite;
            edge.flipX = visual.flipX; edge.flipY = visual.flipY;
            edge.enabled = visual.enabled;
            edge.color = new Color(1f, 0.08f, 0.08f, visual.color.a);
            edge.sortingLayerID = visual.sortingLayerID;
            edge.sortingOrder = visual.sortingOrder - 1;
        }
    }

    private void OnDestroy()
    {
        if (outlineMaterial != null) Destroy(outlineMaterial);
    }
}
