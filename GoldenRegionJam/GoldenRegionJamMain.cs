using BepInEx;
using BepInEx.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;
using System.Security;
using Fisobs.Core;
using UnityEngine;
using System.Linq;
using System;

#pragma warning disable CS0618 // ignore false message
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace GoldenRegionJam;

[BepInPlugin("lb-fgf-m4r-ik.golden-region-jam", "GoldenRegionJam", "1.1.0")]
sealed class GoldenRegionJamMain : BaseUnityPlugin
{
    static AssetBundle? _b;
    [AllowNull] internal static ManualLogSource logger;

    public void OnEnable()
    {
        logger = Logger;
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);
            if (!Futile.atlasManager.DoesContainAtlas("grjsprites"))
                Futile.atlasManager.LoadAtlas("atlases/grjsprites");
            try
            {
                _b ??= AssetBundle.LoadFromFile(ModManager.InstalledMods.First(x => x.id == "lb-fgf-m4r-ik.golden-region-jam").path.Replace("\\", "/") + "/assetbundles/grj_shaders");
                if (!self.Shaders.ContainsKey("GRJEelBody"))
                    self.Shaders.Add("GRJEelBody", FShader.CreateShader("GRJEelBody", (Shader)_b.LoadAsset("assets/grjeelbody.shader")));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        };
        On.RainWorld.UnloadResources += (orig, self) =>
        {
            orig(self);
            if (Futile.atlasManager.DoesContainAtlas("grjsprites"))
                Futile.atlasManager.UnloadAtlas("grjsprites");
            _b?.Unload(true);
            _b = null;
        };
        On.RainWorld.OnModsDisabled += (orig, self, newlyDisabledMods) =>
        {
            orig(self, newlyDisabledMods);
            for (var i = 0; i < newlyDisabledMods.Length; i++)
            {
                if (newlyDisabledMods[i].id == "lb-fgf-m4r-ik.golden-region-jam")
                {
                    if (MultiplayerUnlocks.CreatureUnlockList.Contains(SandboxUnlockID.FlyingBigEel))
                        MultiplayerUnlocks.CreatureUnlockList.Remove(SandboxUnlockID.FlyingBigEel);
                    CreatureTemplateType.UnregisterValues();
                    SandboxUnlockID.UnregisterValues();
                    RoomEffectType.UnregisterValues();
                    PlacedObjectType.UnregisterValues();
                    NewSoundID.UnregisterValues();
                    break;
                }
            }
        };
        Content.Register(new FlyingBigEelCritob());
    }

    public void OnDisable()
    {
        logger = null;
        NewSoundID.soundLines = null;
    }
}
