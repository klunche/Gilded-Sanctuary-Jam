using System.Diagnostics.CodeAnalysis;

namespace GoldenRegionJam;

public static class CreatureTemplateType
{
    [AllowNull] public static CreatureTemplate.Type FlyingBigEel = new(nameof(FlyingBigEel), true);

    public static void UnregisterValues()
    {
        if (FlyingBigEel != null)
        {
            FlyingBigEel.Unregister();
            FlyingBigEel = null;
        }
    }
}

public static class SandboxUnlockID
{
    [AllowNull] public static MultiplayerUnlocks.SandboxUnlockID FlyingBigEel = new(nameof(FlyingBigEel), true);

    public static void UnregisterValues()
    {
        if (FlyingBigEel != null)
        {
            FlyingBigEel.Unregister();
            FlyingBigEel = null;
        }
    }
}

public static class RoomEffectType
{
    [AllowNull] public static RoomSettings.RoomEffect.Type GRJGoldenFlakes = new(nameof(GRJGoldenFlakes), true);

    public static void UnregisterValues()
    {
        if (GRJGoldenFlakes != null)
        {
            GRJGoldenFlakes.Unregister();
            GRJGoldenFlakes = null;
        }
    }
}

public static class PlacedObjectType
{
    [AllowNull] public static PlacedObject.Type GRJLeviathanPushBack = new(nameof(GRJLeviathanPushBack), true);

    public static void UnregisterValues()
    {
        if (GRJLeviathanPushBack != null)
        {
            GRJLeviathanPushBack.Unregister();
            GRJLeviathanPushBack = null;
        }
    }
}

public static class NewSoundID
{
    [AllowNull] public static SoundID Flying_Leviathan_Bite = new(nameof(Flying_Leviathan_Bite), true);
    [AllowNull] internal static string[] soundLines = new[] { "Flying_Leviathan_Bite : bigClankA/vol=0.4" };

    public static void UnregisterValues()
    {
        if (Flying_Leviathan_Bite != null)
        {
            Flying_Leviathan_Bite.Unregister();
            Flying_Leviathan_Bite = null;
        }
    }
}