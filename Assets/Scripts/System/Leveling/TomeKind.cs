public enum TomeKind { Elasticity, Electro, Elements, Glass, Might, Power, Rage, Speed, Stillness, Vitality, Wind }

public static class TomeInfo
{
    public static string Title(TomeKind kind) => "TOME OF " + kind.ToString().ToUpperInvariant();

    public static string Description(TomeKind kind)
    {
        switch (kind)
        {
            case TomeKind.Elasticity: return "+2 BULLET BOUNCES\n-25% BULLET DAMAGE\n-1 MAX HP";
            case TomeKind.Electro: return "+25% RELOAD RATE\nLAST AMMO: 22 LIGHTNING DAMAGE\nTO NEARBY ENEMIES\n-16% BULLET DAMAGE";
            case TomeKind.Elements: return "+35% BURN DAMAGE\n+35% LIGHTNING DAMAGE\n+35% FREEZE DURATION\nDoes not grant elemental attacks.";
            case TomeKind.Glass: return "ALL DAMAGE x2\nDODGE CHANCE CAP: 0%\nANY DAMAGE TAKEN IS FATAL";
            case TomeKind.Might: return "+1 MAX HP\n+2 DAMAGE FOR EACH MAX HP\nScales with later HP changes.";
            case TomeKind.Power: return "+50% BULLET DAMAGE\n+100% BULLET SIZE\n+1 PIERCING\n-25% FIRE RATE\n-1 MAX HP";
            case TomeKind.Rage: return "+60% FIRE RATE\n+60 DEG SPREAD\nBASE MAX AMMO x3\n-50% BULLET DAMAGE\n-95% KNOCKBACK";
            case TomeKind.Speed: return "+50% MOVE SPEED\n-1 MAX HP";
            case TomeKind.Stillness: return "+100% BULLET DAMAGE\n+100% BULLET SIZE\n+1 PROJECTILE\n-100% WALK SPEED\n-50% RELOAD SPEED";
            case TomeKind.Vitality: return "+4 MAX HP\n-25% DODGE CHANCE";
            default: return "+10% MOVE SPEED\nMOVE SPEED BONUSES INCREASE\nALL DAMAGE\n-15% DAMAGE DEALT";
        }
    }
}
