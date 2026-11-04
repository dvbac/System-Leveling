using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(EnemyStats), typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class RotwoodBoss : MonoBehaviour
{
    public enum ActionState { Chase, Roots, Windup, Charge, Stagger, Exposed, Summon, Dead }
    public enum Motion { Walk, RootCast, Windup, Charge, Stagger, Exposed, Death }
    [Serializable] public class Sequence
    {
        public Sprite[] frames;
        public float fps = 8;
        public bool loop;
        public AudioClip sound;
    }

    [SerializeField] private Sequence[] motions;
    [SerializeField] private Sprite warningRing;
    [SerializeField] private Sprite rootSprite;
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private float rootDamage = 6f;
    [SerializeField] private float chargeDamage = 8f;
    [SerializeField] private float chargeSpeed = 9f;
    [SerializeField] private float chargeRange = 8f;
    [SerializeField] private float chaseDuration = 3f;
    [SerializeField] private float bodyRadius = 0.65f;
    [SerializeField] private LayerMask obstacleLayers;
    private PlayerStats player;
    private EnemyStats stats;
    private SpriteRenderer visual;
    private Rigidbody2D body;
    private AudioSource audioSource;
    private RotwoodEncounter encounter;
    private Tilemap ground;
    private readonly List<RotwoodRoot> roots = new List<RotwoodRoot>();
    private readonly List<EnemyStats> wolves = new List<EnemyStats>();
    private readonly List<GameObject> markers = new List<GameObject>();
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private ActionState state;
    private float stateTime;
    private float motionTime;
    private Motion motion;
    private Vector2 chargeDirection;
    private float chargeRemaining;
    private bool chargeHit;
    private int attackIndex;
    private Vector3[] summonPositions;
    private bool stopped;
    private readonly Vector3[] playerTrail = new Vector3[3];
    private float trailTimer;
    public ActionState State => state;
    public EnemyStats Stats => stats;
    public float DamageTakenMultiplier => state == ActionState.Exposed ? 1.5f : 1f;
    public int LivingWolves { get { wolves.RemoveAll(w => w == null || w.IsDead); return wolves.Count; } }
    public event System.Action DeathFinished;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
        visual = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
    }

    public void Configure(PlayerStats target, RotwoodEncounter owner, Tilemap allowedGround, LayerMask obstacles)
    {
        player = target;
        encounter = owner;
        ground = allowedGround;
        obstacleLayers = obstacles;
        for (int i = 0; i < playerTrail.Length; i++) playerTrail[i] = target.transform.position;
        Enter(ActionState.Chase);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
        Animate(Time.deltaTime);
    }

    internal void Tick(float delta)
    {
        if (GameOverController.GameplayStopped || (player != null && player.IsDead))
        {
            if (!stopped) { stopped = true; Cleanup(); if (audioSource != null) audioSource.Stop(); }
            return;
        }
        if (Time.timeScale <= 0 || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        if (state == ActionState.Dead)
        {
            stateTime += delta;
            if (stateTime >= Duration(Motion.Death))
            {
                DeathFinished?.Invoke();
                if (encounter != null) encounter.BossDefeated();
                enabled = false;
            }
            return;
        }
        if (player == null || !player.gameObject.activeInHierarchy) return;
        trailTimer += delta;
        if (trailTimer >= 0.35f)
        {
            trailTimer = 0;
            playerTrail[2] = playerTrail[1];
            playerTrail[1] = playerTrail[0];
            playerTrail[0] = player.transform.position;
        }
        stateTime += delta;
        if (state == ActionState.Chase)
        {
            Vector2 direction = player.transform.position - transform.position;
            visual.flipX = direction.x > 0; // Source art faces left.
            if (direction.magnitude > 1.8f) Move(direction.normalized, stats.currentMoveSpeed * stats.UpgradeMovementFactor * delta, false);
            if (stateTime >= chaseDuration)
            {
                ActionState[] attacks = { ActionState.Roots, ActionState.Windup, ActionState.Summon };
                Enter(attacks[attackIndex++ % attacks.Length]);
            }
        }
        else if (state == ActionState.Roots && stateTime >= 1.8f) Enter(ActionState.Chase);
        else if (state == ActionState.Windup && stateTime >= 1f) Enter(ActionState.Charge);
        else if (state == ActionState.Charge)
        {
            float distance = Mathf.Min(chargeRemaining, chargeSpeed * delta);
            float moved = Move(chargeDirection, distance, true);
            chargeRemaining -= moved;
            if (moved < distance - 0.001f || chargeRemaining <= 0.001f) Enter(ActionState.Stagger);
        }
        else if (state == ActionState.Stagger && stateTime >= 1.5f)
            Enter(stats.currentHealth <= stats.enemyData.MaxHealth * 0.5f ? ActionState.Exposed : ActionState.Chase);
        else if (state == ActionState.Exposed && stateTime >= 2f) Enter(ActionState.Chase);
        else if (state == ActionState.Summon && stateTime >= 1f)
        { SummonWolves(); Enter(ActionState.Chase); }
    }

    private void Enter(ActionState next)
    {
        ClearMarkers();
        state = next;
        stateTime = 0;
        Motion animation = next == ActionState.Roots || next == ActionState.Summon ? Motion.RootCast :
            next == ActionState.Windup ? Motion.Windup : next == ActionState.Charge ? Motion.Charge :
            next == ActionState.Stagger ? Motion.Stagger : next == ActionState.Exposed ? Motion.Exposed :
            next == ActionState.Dead ? Motion.Death : Motion.Walk;
        motion = animation;
        motionTime = 0;
        if (audioSource != null)
        {
            audioSource.Stop();
            Sequence sequence = motions[(int)motion];
            audioSource.clip = sequence.sound;
            audioSource.loop = motion == Motion.Walk || motion == Motion.Charge;
            if (sequence.sound != null) audioSource.Play();
        }
        if (next == ActionState.Roots)
        {
            roots.RemoveAll(r => r == null);
            // Snapshot all three positions now: warning rings never follow the player.
            for (int i = 0; i < playerTrail.Length; i++)
            {
                Vector3 position = playerTrail[i];
                if (!OnGround(position)) continue;
                var root = new GameObject("Rotwood Root Warning").AddComponent<RotwoodRoot>();
                root.transform.position = position;
                root.Configure(player, warningRing, rootSprite, 0.8f + i * 0.25f, 0.85f, rootDamage);
                roots.Add(root);
            }
        }
        if (next == ActionState.Windup)
        {
            chargeDirection = ((Vector2)(player.transform.position - transform.position)).normalized;
            if (chargeDirection == Vector2.zero) chargeDirection = Vector2.left;
            visual.flipX = chargeDirection.x > 0;
            for (int i = 1; i <= Mathf.CeilToInt(chargeRange); i++)
                Marker(transform.position + (Vector3)chargeDirection * i, bodyRadius + 0.3f);
        }
        if (next == ActionState.Charge) { chargeRemaining = chargeRange; chargeHit = false; }
        if (next == ActionState.Summon)
        {
            summonPositions = new Vector3[2];
            Vector3 away = ((Vector2)(transform.position - player.transform.position)).normalized;
            if (away == Vector3.zero) away = Vector3.up;
            Vector3 side = new Vector3(-away.y, away.x);
            for (int i = 0; i < 2; i++)
            {
                summonPositions[i] = transform.position + away * 2 + side * (i == 0 ? -2 : 2);
                if (CanSummonAt(summonPositions[i])) Marker(summonPositions[i], 0.65f);
            }
        }
    }

    private void SummonWolves()
    {
        if (wolfPrefab == null) return;
        foreach (Vector3 position in summonPositions)
        {
            if (LivingWolves >= 4 || !CanSummonAt(position)) continue;
            var wolf = Instantiate(wolfPrefab, position, Quaternion.identity);
            wolves.Add(wolf.GetComponent<EnemyStats>());
        }
    }

    private bool CanSummonAt(Vector3 position) => OnGround(position) &&
        Vector2.Distance(position, player.transform.position) >= 2f &&
        (obstacleLayers.value == 0 || Physics2D.OverlapCircle(position, 0.6f, obstacleLayers) == null);

    private bool OnGround(Vector3 p) => encounter != null ? encounter.IsOnGround(p) :
        ground == null || ground.HasTile(ground.WorldToCell(p));

    private float Move(Vector2 direction, float distance, bool charging)
    {
        Vector2 start = transform.position;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayers);
        filter.useTriggers = false;
        int count = obstacleLayers.value == 0 ? 0 : Physics2D.CircleCast(start, bodyRadius, direction, filter, hits, distance);
        for (int i = 0; i < count; i++)
            if (hits[i].collider != null && hits[i].collider.transform.root != transform.root)
                distance = Mathf.Min(distance, Mathf.Max(0f, hits[i].distance - 0.03f));
        Vector2 end = start + direction * distance;
        if (!OnGround(end) || !OnGround(end + direction * bodyRadius)) return 0;
        if (charging && !chargeHit && DistanceToSegment(player.transform.position, start, end) <= bodyRadius + 0.3f)
        {
            player.TakeDamage(chargeDamage);
            chargeHit = true;
        }
        body.position = end;
        return distance;
    }

    internal static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude) : 0;
        return Vector2.Distance(point, start + t * segment);
    }

    private void Marker(Vector3 position, float radius)
    {
        var marker = new GameObject("Rotwood Telegraph");
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * radius * 2;
        var renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = warningRing;
        renderer.color = new Color(1, 0.45f, 0.15f, 0.8f);
        renderer.sortingOrder = 8;
        markers.Add(marker);
    }

    private void ClearMarkers()
    {
        foreach (var marker in markers) if (marker != null) Destroy(marker);
        markers.Clear();
    }

    private void Animate(float delta)
    {
        if (GameOverController.GameplayStopped || stopped || delta <= 0) return;
        Sequence sequence = motions[(int)motion];
        if (sequence.frames == null || sequence.frames.Length == 0) return;
        motionTime += delta;
        int frame = Mathf.FloorToInt(motionTime * sequence.fps);
        frame = sequence.loop ? frame % sequence.frames.Length : Mathf.Min(frame, sequence.frames.Length - 1);
        visual.sprite = sequence.frames[frame];
    }

    public float BeginDeath()
    {
        if (state == ActionState.Dead) return Duration(Motion.Death);
        Cleanup();
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
        body.simulated = false;
        Enter(ActionState.Dead);
        // EnemyStats schedules Destroy; leave one update beyond the final frame for victory.
        return Duration(Motion.Death) + 0.2f;
    }

    private float Duration(Motion value) => motions[(int)value].frames.Length / Mathf.Max(1f, motions[(int)value].fps);

    private void Cleanup()
    {
        ClearMarkers();
        foreach (var root in roots) if (root != null) Destroy(root.gameObject);
        roots.Clear();
        foreach (var wolf in wolves) if (wolf != null) Destroy(wolf.gameObject);
        wolves.Clear();
    }

    private void OnDestroy() => Cleanup();
}
