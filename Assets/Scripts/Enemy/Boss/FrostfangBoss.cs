using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyStats), typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class FrostfangBoss : MonoBehaviour
{
    public enum ActionState { Chase, PounceWindup, Pounce, BreathWindup, Breath, Pillars, Howl, Blizzard, Recover, Dead }
    public enum Motion { Idle, Run, Pounce, Breath, Pillars, Howl, Death }
    [Serializable] public class Sequence
    {
        public Sprite[] frames;
        public float fps = 8;
        public bool loop;
    }
    [SerializeField] private Sequence[] motions;
    [SerializeField] private Sprite warningRing, warningCone;
    [SerializeField] private Sprite[] impactFrames, breathFrames, pillarFrames, blizzardFrames;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float pounceDamage = 25, breathDamage = 10, pillarDamage = 20, blizzardDamage = 6;
    private PlayerStats player;
    private FrostfangEncounter encounter;
    private EnemyStats stats;
    private SpriteRenderer visual;
    private Rigidbody2D body;
    private ActionState state;
    private Motion motion;
    private float stateTime, motionTime, damageAt;
    private int attackIndex;
    private Vector2 aim, landing;
    private bool finished, stopped;
    private readonly List<GameObject> markers = new List<GameObject>();
    private readonly List<FrostfangEffect> effects = new List<FrostfangEffect>();
    private readonly List<FrostfangIcePillar> pillars = new List<FrostfangIcePillar>();
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private const float BodyRadius = 0.65f, PounceRadius = 1.3f, BreathRange = 5, BlizzardRadius = 8;
    public ActionState State => state;
    public EnemyStats Stats => stats;
    public event Action DeathFinished;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>(); visual = GetComponent<SpriteRenderer>(); body = GetComponent<Rigidbody2D>();
    }

    public void Configure(PlayerStats target, FrostfangEncounter owner, LayerMask obstacles)
    {
        player = target; encounter = owner; obstacleLayers = obstacles;
        Enter(ActionState.Chase);
    }

    private void Update() { Tick(Time.deltaTime); Animate(Time.deltaTime); }

    internal void Tick(float delta)
    {
        if (GameOverController.GameplayStopped || player == null || player.IsDead)
        {
            if (!stopped) { stopped = true; Cleanup(); }
            return;
        }
        if (Time.timeScale <= 0 || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        stateTime += delta;
        if (state == ActionState.Dead)
        {
            if (!finished && stateTime >= Duration(Motion.Death))
            {
                finished = true; DeathFinished?.Invoke();
                if (encounter != null) encounter.BossDefeated();
            }
            return;
        }
        if (state == ActionState.Chase)
        {
            Vector2 direction = player.transform.position - transform.position;
            visual.flipX = direction.x < 0; // Frostfang source faces right.
            if (direction.magnitude > 2) Move(direction.normalized, stats.currentMoveSpeed * stats.UpgradeMovementFactor * delta);
            if (stateTime >= 2.5f)
            {
                ActionState[] attacks = { ActionState.PounceWindup, ActionState.BreathWindup, ActionState.Pillars, ActionState.Howl };
                Enter(attacks[attackIndex++ % attacks.Length]);
            }
        }
        else if (state == ActionState.PounceWindup && stateTime >= 0.75f) Enter(ActionState.Pounce);
        else if (state == ActionState.Pounce)
        {
            Vector2 direction = landing - (Vector2)transform.position;
            Move(direction.normalized, Mathf.Min(direction.magnitude, 16 * delta));
            if (stateTime >= 0.5f)
            {
                Effect("Pounce Impact", impactFrames, 16, false, 0.5f, transform.position);
                if (Vector2.Distance(player.transform.position, transform.position) <= PounceRadius) player.TakeDamage(pounceDamage);
                Enter(ActionState.Recover);
            }
        }
        else if (state == ActionState.BreathWindup && stateTime >= 0.75f) Enter(ActionState.Breath);
        else if (state == ActionState.Breath)
        {
            if (stateTime >= 1.5f) { ClearEffects(); Enter(ActionState.Recover); }
            else DamagePulse(0.5f, InBreath(player.transform.position), breathDamage);
        }
        else if (state == ActionState.Pillars && stateTime >= 1.25f) Enter(ActionState.Recover);
        else if (state == ActionState.Howl && stateTime >= 1) Enter(ActionState.Blizzard);
        else if (state == ActionState.Blizzard)
        {
            if (stateTime >= 4) { ClearEffects(); Enter(ActionState.Recover); }
            else DamagePulse(0.75f, Vector2.Distance(player.transform.position, transform.position) <= BlizzardRadius, blizzardDamage);
        }
        else if (state == ActionState.Recover && stateTime >= 1) Enter(ActionState.Chase);
    }

    private void DamagePulse(float interval, bool inside, float damage)
    {
        if (stateTime < damageAt) return;
        damageAt = (Mathf.Floor(stateTime / interval) + 1) * interval;
        if (!inside || !ClearLine(transform.position, player.transform.position)) return;
        player.TakeDamage(damage);
        player.GetComponent<PlayerController>()?.ApplySlow(0.6f, 1.2f);
    }

    internal bool InBreath(Vector2 point)
    {
        Vector2 local = point - Mouth;
        float along = Vector2.Dot(local, aim);
        float across = Mathf.Abs(local.x * aim.y - local.y * aim.x);
        return along >= 0 && along <= BreathRange && across <= along * 0.25f;
    }

    private Vector2 Mouth => (Vector2)transform.position + new Vector2(visual.flipX ? -0.8f : 0.8f, 0.9f);

    private void Enter(ActionState next)
    {
        if (state == ActionState.Dead && next != ActionState.Dead) return;
        ClearMarkers(); state = next; stateTime = 0; damageAt = 0;
        motionTime = 0;
        motion = next == ActionState.Chase ? Motion.Run : next == ActionState.PounceWindup || next == ActionState.Pounce ? Motion.Pounce :
            next == ActionState.BreathWindup || next == ActionState.Breath ? Motion.Breath : next == ActionState.Pillars ? Motion.Pillars :
            next == ActionState.Howl || next == ActionState.Blizzard ? Motion.Howl : next == ActionState.Dead ? Motion.Death : Motion.Idle;
        effects.RemoveAll(e => e == null); pillars.RemoveAll(p => p == null);
        if (next == ActionState.PounceWindup)
        {
            Vector2 offset = Vector2.ClampMagnitude((Vector2)(player.transform.position - transform.position), 6);
            landing = SafeEnd(offset.normalized, offset.magnitude);
            aim = offset.sqrMagnitude > 0 ? offset.normalized : Vector2.right;
            visual.flipX = aim.x < 0;
            Marker(landing, PounceRadius);
        }
        if (next == ActionState.Pounce) { motionTime = 0.75f; Marker(landing, PounceRadius); }
        if (next == ActionState.BreathWindup)
        {
            visual.flipX = player.transform.position.x < transform.position.x;
            aim = ((Vector2)player.transform.position - Mouth).normalized;
            if (aim == Vector2.zero) aim = Vector2.right;
            var marker = Marker(Mouth, 1);
            marker.GetComponent<SpriteRenderer>().sprite = warningCone;
            marker.transform.right = aim;
            marker.transform.localScale = new Vector3(BreathRange, BreathRange, 1);
        }
        if (next == ActionState.Breath)
        {
            motionTime = 0.75f;
            var marker = Marker(Mouth, 1);
            marker.GetComponent<SpriteRenderer>().sprite = warningCone;
            marker.transform.right = aim; marker.transform.localScale = Vector3.one * BreathRange;
            var effect = Effect("Freezing Breath", breathFrames, 16, true, 1.5f, Mouth);
            effect.transform.right = aim;
            effect.transform.localScale = Vector3.one * (BreathRange / (232f / 48));
        }
        if (next == ActionState.Pillars)
        {
            Vector2 target = player.transform.position;
            Vector2 side = new Vector2(-aim.y, aim.x);
            if (side == Vector2.zero) side = Vector2.right;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 point = target + side * i * 1.8f;
                if (!OnGround(point) || (obstacleLayers.value != 0 && Physics2D.OverlapCircle(point, FrostfangIcePillar.Radius, obstacleLayers))) continue;
                var pillar = new GameObject("Frostfang Ice Pillar").AddComponent<FrostfangIcePillar>();
                pillar.transform.position = point;
                pillar.Configure(player, warningRing, pillarFrames, 0.75f + (i + 1) * 0.2f, pillarDamage);
                pillars.Add(pillar);
            }
        }
        if (next == ActionState.Howl) Marker(transform.position, BlizzardRadius);
        if (next == ActionState.Blizzard)
        {
            motionTime = 0.75f;
            Marker(transform.position, BlizzardRadius);
            // Sparse tiles cover the warning disk without tinting the entire screen.
            for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++)
            {
                if (x * x + y * y > 4) continue;
                var effect = Effect("Blizzard Snow", blizzardFrames, 8, true, 4, transform.position + new Vector3(x, y) * 2.65f);
                effect.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.75f);
            }
        }
    }

    private GameObject Marker(Vector2 point, float radius)
    {
        var go = new GameObject("Frostfang Telegraph");
        go.transform.position = point; go.transform.localScale = Vector3.one * radius * 2;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = warningRing;
        renderer.color = new Color(1, 0.4f, 0.15f, 0.9f); renderer.sortingOrder = 8;
        markers.Add(go); return go;
    }

    private FrostfangEffect Effect(string name, Sprite[] frames, float fps, bool loop, float duration, Vector2 position)
    {
        var effect = new GameObject("Frostfang " + name).AddComponent<FrostfangEffect>();
        effect.transform.position = position; effect.Configure(frames, fps, loop, duration);
        effects.Add(effect); return effect;
    }

    private bool OnGround(Vector2 point) => encounter == null || encounter.IsOnGround(point);
    private bool ClearLine(Vector2 start, Vector2 end)
    {
        var filter = new ContactFilter2D(); filter.SetLayerMask(obstacleLayers); filter.useTriggers = false;
        int count = obstacleLayers.value == 0 ? 0 : Physics2D.Linecast(start, end, filter, hits);
        for (int i = 0; i < count; i++) if (hits[i].collider != null && hits[i].collider.transform.root != transform.root) return false;
        return true;
    }

    private Vector2 SafeEnd(Vector2 direction, float distance)
    {
        Vector2 start = transform.position;
        var filter = new ContactFilter2D(); filter.SetLayerMask(obstacleLayers); filter.useTriggers = false;
        int count = obstacleLayers.value == 0 ? 0 : Physics2D.CircleCast(start, BodyRadius, direction, filter, hits, distance);
        for (int i = 0; i < count; i++)
            if (hits[i].collider != null && hits[i].collider.transform.root != transform.root)
                distance = Mathf.Min(distance, Mathf.Max(0, hits[i].distance - 0.03f));
        // Check the swept ground, not only the endpoint, to avoid crossing unloaded gaps.
        for (float step = 0; step <= distance; step += 0.2f)
            if (!OnGround(start + direction * step) || !OnGround(start + direction * (step + BodyRadius))) return start + direction * Mathf.Max(0, step - 0.2f);
        Vector2 end = start + direction * distance;
        return OnGround(end) && OnGround(end + direction * BodyRadius) ? end : start;
    }

    private void Move(Vector2 direction, float distance)
    {
        Vector2 end = SafeEnd(direction, distance);
        body.position = end;
        transform.position = end; // Landing damage/FX need the new pose before the next physics sync.
    }

    private void Animate(float delta)
    {
        if (stopped || GameOverController.GameplayStopped || Time.timeScale <= 0 || delta <= 0) return;
        Sequence clip = motions[(int)motion]; motionTime += delta;
        int frame = Mathf.FloorToInt(motionTime * clip.fps);
        if (state == ActionState.PounceWindup) frame = Mathf.Min(5, frame);
        else if (state == ActionState.BreathWindup || state == ActionState.Howl) frame = Mathf.Min(5, frame);
        else if (state == ActionState.Breath || state == ActionState.Blizzard) frame = 6 + Mathf.FloorToInt(stateTime * 8) % 6;
        else frame = clip.loop ? frame % clip.frames.Length : Mathf.Min(frame, clip.frames.Length - 1);
        visual.sprite = clip.frames[Mathf.Clamp(frame, 0, clip.frames.Length - 1)];
    }

    private float Duration(Motion value) => motions[(int)value].frames.Length / Mathf.Max(1, motions[(int)value].fps);
    public float BeginDeath()
    {
        if (state == ActionState.Dead) return Duration(Motion.Death) + 0.2f;
        Cleanup();
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
        body.simulated = false; Enter(ActionState.Dead);
        return Duration(Motion.Death) + 0.2f;
    }
    private void ClearMarkers() { foreach (var marker in markers) if (marker != null) Destroy(marker); markers.Clear(); }
    private void ClearEffects() { foreach (var effect in effects) if (effect != null) Destroy(effect.gameObject); effects.Clear(); }
    private void Cleanup()
    {
        ClearMarkers(); ClearEffects();
        foreach (var pillar in pillars) if (pillar != null) Destroy(pillar.gameObject);
        pillars.Clear();
    }
    private void OnDestroy() => Cleanup();
}
