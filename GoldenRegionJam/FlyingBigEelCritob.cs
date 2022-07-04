using Fisobs.Creatures;
using Fisobs.Core;
using System.Collections.Generic;
using Fisobs.Sandbox;
using static PathCost.Legality;
using UnityEngine;
using System;

namespace GoldenRegionJam;

sealed class FlyingBigEelCritob : Critob
{
    internal FlyingBigEelCritob() : base(EnumExt_GoldenRegionJam.FlyingBigEel)
    {
        Icon = new SimpleIcon("Kill_BigEel", RainWorld.GoldRGB + new Color(.2f, .2f, .2f));
        RegisterUnlock(KillScore.Constant(100), EnumExt_GoldenRegionJam.FlyingBigEelUnlock);
        Hooks.Apply();
    }

    public override IEnumerable<CreatureTemplate> GetTemplates()
    {
        var t = new CreatureFormula(this, "FlyingBigEel") {
            TileResistances = new() {
                Air = new(1f, Allowed)
            },
            ConnectionResistances = new() {
                Standard = new(1f, Allowed),
                OutsideRoom = new(1f, Allowed),
                SkyHighway = new(100000f, Allowed),
                OffScreenMovement = new(1f, Allowed),
                BetweenRooms = new(10f, Allowed)
            },
            DefaultRelationship = new(CreatureTemplate.Relationship.Type.Eats, 1f),
            DamageResistances = new() { Base = 1000f },
            StunResistances = new() { Base = 1000f },
            HasAI = true,
            Pathing = PreBakedPathing.Ancestral(CreatureTemplate.Type.BigEel)
        }.IntoTemplate();
        t.abstractedLaziness = 100;
        t.requireAImap = true;
        t.offScreenSpeed = 1f;
        t.bodySize = 100f;
        t.grasps = 1;
        t.stowFoodInDen = true;
        t.visualRadius = 1450f;
        t.waterVision = 0f;
        t.throughSurfaceVision = 0f;
        t.movementBasedVision = 0f;
        t.hibernateOffScreen = true;
        t.dangerousToPlayer = .8f;
        t.communityID = CreatureCommunities.CommunityID.None;
        t.waterRelationship = CreatureTemplate.WaterRelationship.AirOnly;
        t.canFly = true;
        yield return t;
    }

    public override void EstablishRelationships()
    {
        Relationships b = new(EnumExt_GoldenRegionJam.FlyingBigEel);
        for (var i = 0; i < Enum.GetValues(typeof(CreatureTemplate.Type)).Length; i++) b.FearedBy((CreatureTemplate.Type)i, 1f);
        b.IgnoredBy(CreatureTemplate.Type.TempleGuard);
        b.IgnoredBy(CreatureTemplate.Type.GarbageWorm);
        b.IgnoredBy(CreatureTemplate.Type.Leech);
        b.IgnoredBy(CreatureTemplate.Type.SeaLeech);
        b.IgnoredBy(CreatureTemplate.Type.BigEel);
        b.IgnoredBy(CreatureTemplate.Type.JetFish);
        b.IgnoredBy(CreatureTemplate.Type.Hazer);
        b.IgnoredBy(CreatureTemplate.Type.Snail);
        b.IgnoredBy(CreatureTemplate.Type.Salamander);
        b.IgnoredBy(CreatureTemplate.Type.TentaclePlant);
        b.IgnoredBy(CreatureTemplate.Type.PoleMimic);
        b.Ignores(CreatureTemplate.Type.TempleGuard);
        b.Ignores(CreatureTemplate.Type.GarbageWorm);
        b.Ignores(CreatureTemplate.Type.Leech);
        b.Ignores(CreatureTemplate.Type.SeaLeech);
        b.Ignores(CreatureTemplate.Type.BigEel);
        b.Ignores(CreatureTemplate.Type.JetFish);
        b.Ignores(CreatureTemplate.Type.Hazer);
        b.Ignores(CreatureTemplate.Type.Snail);
        b.Ignores(CreatureTemplate.Type.Salamander);
        b.Ignores(CreatureTemplate.Type.TentaclePlant);
        b.Ignores(CreatureTemplate.Type.PoleMimic);
        b.Ignores(CreatureTemplate.Type.Overseer);
        b.Ignores(EnumExt_GoldenRegionJam.FlyingBigEel);
    }

    public override ArtificialIntelligence GetRealizedAI(AbstractCreature acrit) => new BigEelAI(acrit, acrit.world);

    public override Creature GetRealizedCreature(AbstractCreature acrit) => new BigEel(acrit, acrit.world);

    public override AbstractCreatureAI GetAbstractAI(AbstractCreature acrit) => new BigEelAbstractAI(acrit.world, acrit);

    public override void LoadResources(RainWorld rainWorld)
    {
        string[] sprAr = { };
        foreach (var spr in sprAr) 
            Ext.LoadAtlasFromEmbRes(GetType().Assembly, spr);
    }

    public override CreatureTemplate.Type? ArenaFallback(CreatureTemplate.Type type) => CreatureTemplate.Type.BigEel;
}