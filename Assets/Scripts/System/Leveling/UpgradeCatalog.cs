using UnityEngine;

public static class UpgradeCatalog
{
    public sealed class Tree
    {
        public readonly string Name, Symbol;
        public readonly Color Color;
        public readonly UpgradeKind Root, A, B, Final;
        public Tree(string name, string symbol, Color color, UpgradeKind root, UpgradeKind a, UpgradeKind b, UpgradeKind final)
        { Name = name; Symbol = symbol; Color = color; Root = root; A = a; B = b; Final = final; }
        public bool Contains(UpgradeKind kind) => kind == Root || kind == A || kind == B || kind == Final;
    }

    public static readonly Tree[] Trees =
    {
        new Tree("POWER", "DMG", new Color(1f, .65f, .25f), UpgradeKind.PowerBullet, UpgradeKind.BigShot, UpgradeKind.Splinter, UpgradeKind.ReaperRounds),
        new Tree("RAPID FIRE", "FAST", new Color(1f, .88f, .35f), UpgradeKind.RapidFire, UpgradeKind.LightBullets, UpgradeKind.RubberBullets, UpgradeKind.Siege),
        new Tree("MULTISHOT", "MULTI", new Color(.85f, .55f, 1f), UpgradeKind.DoubleShot, UpgradeKind.FanFire, UpgradeKind.SplitFire, UpgradeKind.Fusillade),
        new Tree("RELOAD", "RLD", new Color(.4f, .85f, 1f), UpgradeKind.QuickHands, UpgradeKind.ArmedReady, UpgradeKind.FreshClip, UpgradeKind.KillClip),
        new Tree("FROST", "ICE", new Color(.35f, .9f, 1f), UpgradeKind.FrostMage, UpgradeKind.Frostbite, UpgradeKind.IceShard, UpgradeKind.Shatter),
        new Tree("FLAME", "FIRE", new Color(1f, .35f, .12f), UpgradeKind.PyroMage, UpgradeKind.FireStarter, UpgradeKind.IntenseBurn, UpgradeKind.SoothingWarmth),
        new Tree("VITALITY", "HP", new Color(.5f, 1f, .55f), UpgradeKind.Vitality, UpgradeKind.AngerPoint, UpgradeKind.Giant, UpgradeKind.Regeneration),
        new Tree("HASTE", "SPD", new Color(.35f, 1f, .8f), UpgradeKind.MoveSpeed, UpgradeKind.BlazingSpeed, UpgradeKind.RunAndGun, UpgradeKind.InTheWind)
    };

    public static Tree Find(UpgradeKind kind)
    {
        foreach (var tree in Trees) if (tree.Contains(kind)) return tree;
        return null;
    }

    public static bool IsNew(UpgradeKind kind) => kind >= UpgradeKind.RapidFire && kind <= UpgradeKind.InTheWind;
    public static string Title(UpgradeKind kind)
    {
        string name = kind.ToString();
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2").ToUpperInvariant();
    }

    public static string Description(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.RapidFire: return "+25% ATTACK RATE";
            case UpgradeKind.FrostMage: return "35% ON HIT: FREEZE FOR 1.5s\nBosses: 30% slow for 0.3s.";
            case UpgradeKind.PyroMage: return "50% ON HIT: BURN\n3 damage/sec for 3 seconds.";
            case UpgradeKind.BigShot: return "+35% DAMAGE\n+40% PROJECTILE SIZE";
            case UpgradeKind.Splinter: return "Kills scatter 3 fragments\nfor 10% weapon damage.\nFragments cannot split again.";
            case UpgradeKind.ReaperRounds: return "+20% DAMAGE, +1 PIERCE\nKilled targets cost no pierce.";
            case UpgradeKind.LightBullets: return "+15% FIRE RATE, +1 AMMO\n+15% PROJECTILE SPEED";
            case UpgradeKind.RubberBullets: return "+1 BULLET BOUNCE\n+10% FIRE RATE";
            case UpgradeKind.Siege: return "While stationary: 40% chance\nto refund the round just fired.";
            case UpgradeKind.FanFire: return "Last round fires 10 fragments\nin a ring for 15% damage.";
            case UpgradeKind.SplitFire: return "Fire one extra projectile\nbehind you each attack.";
            case UpgradeKind.Fusillade: return "+1 PROJECTILE, +15 SPREAD\n-20% PROJECTILE DAMAGE";
            case UpgradeKind.ArmedReady: return "+10% RELOAD RATE\n+2 AMMO / CROSSBOW CHARGE LEVELS";
            case UpgradeKind.FreshClip: return "+5% RELOAD RATE\n+50% DAMAGE for 1s\nafter recovering ammo.";
            case UpgradeKind.KillClip: return "+5% RELOAD RATE per kill\n(up to +100%).\nResets on ammo recovery.";
            case UpgradeKind.Frostbite: return "Freeze removes 15% MAX HP.\nBosses: 1%. Once per\nfreeze, not per frozen hit.";
            case UpgradeKind.IceShard: return "Last round fires 3 ice shards\nthat always freeze enemies.";
            case UpgradeKind.Shatter: return "Frozen enemies burst on death:\n7% MAX HP damage in radius 2.\nBursts cannot chain themselves.";
            case UpgradeKind.FireStarter: return "Every 5th attack launches\na fireball: 20 area damage\nand guaranteed burn.";
            case UpgradeKind.IntenseBurn: return "+35% BURN DAMAGE";
            case UpgradeKind.SoothingWarmth: return "Burn application has a 0.5%\nchance to heal 1 HP.";
            case UpgradeKind.AngerPoint: return "Taking damage grants +50%\ndamage and attack rate\nfor 10 seconds.";
            case UpgradeKind.Giant: return "+6 MAX HP, HEAL 6\n+15% CHARACTER SIZE";
            case UpgradeKind.Regeneration: return "Heal 1 HP every 30 seconds.";
            case UpgradeKind.BlazingSpeed: return "+10% BASE MOVE SPEED\nMoving leaves burning sparks\nthat ignite nearby enemies.";
            case UpgradeKind.RunAndGun: return "Attack movement: 45% -> 90%.\nRoots and Tome movement\nrestrictions still apply.";
            case UpgradeKind.InTheWind: return "+10% DAMAGE AND MOVE SPEED\nevery 10s unhurt (max +40%).\nTaking damage resets the bonus.";
            default: return "";
        }
    }
}
