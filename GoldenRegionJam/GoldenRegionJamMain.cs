using BepInEx;
using Fisobs.Core;

namespace GoldenRegionJam;

[BepInPlugin("lb-fgf-m4r-ik.golden-region-jam", "GoldenRegionJam", "0.1.0"), BepInDependency("github.notfood.BepInExPartialityWrapper", BepInDependency.DependencyFlags.SoftDependency)]
public class GoldenRegionJamMain : BaseUnityPlugin
{
    internal static BepInEx.Logging.ManualLogSource logger;

    public void OnEnable()
    {
        logger = Logger;
        if (EnumExt_GoldenRegionJam.GRJGoldenFlakes == 0) 
            Logger.LogWarning("EnumExtender is missing!");
        Content.Register(new FlyingBigEelCritob());
    }
}