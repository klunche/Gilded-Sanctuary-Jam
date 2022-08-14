using MonoMod.Cil;
using static Mono.Cecil.Cil.OpCodes;
using Random = UnityEngine.Random;
using static System.Reflection.BindingFlags;
using UnityEngine;
using static GoldenRegionJam.GoldenRegionJamMain;
using System;
using MonoMod.RuntimeDetour;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using RWCustom;
using DevInterface;

namespace GoldenRegionJam;

public class Hooks
{
    public static void Apply()
    {
        On.Room.Loaded += (orig, self) =>
        {
            orig(self);
            for (var k = 0; k < self.roomSettings.effects.Count; k++)
            {
                var effect = self.roomSettings.effects[k];
                if (effect.type == EnumExt_GoldenRegionJam.GRJGoldenFlakes)
                    self.AddObject(new GoldFlakesEffect(self));
            }
            for (var i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                var pObj = self.roomSettings.placedObjects[i];
                if (pObj.type == EnumExt_GoldenRegionJam.GRJLeviathanPushBack)
                    self.AddObject(new LeviathanPushbackObject(self, pObj.data as PlacedObject.ResizableObjectData));
            }
        };
        On.PlacedObject.GenerateEmptyData += (orig, self) =>
        {
            orig(self);
            if (self.type == EnumExt_GoldenRegionJam.GRJLeviathanPushBack)
                self.data = new PlacedObject.ResizableObjectData(self);
        };
        On.DevInterface.ObjectsPage.CreateObjRep += (orig, self, tp, pObj) =>
        {
            if (tp == EnumExt_GoldenRegionJam.GRJLeviathanPushBack)
            {
                if (pObj is null)
                    self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
                    {
                        pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + Custom.DegToVec(Random.value * 360f) * .2f
                    });
                var pObjRep = new ResizeableObjectRepresentation(self.owner, $"{tp}_Rep", self, pObj, tp.ToString(), true);
                self.tempNodes.Add(pObjRep);
                self.subNodes.Add(pObjRep);
            }
            else
                orig(self, tp, pObj);
        };
        On.Room.NowViewed += (orig, self) =>
        {
            orig(self);
            for (var j = 0; j < self.roomSettings.effects.Count; j++)
            {
                var effect = self.roomSettings.effects[j];
                if (effect.type == EnumExt_GoldenRegionJam.GRJGoldenFlakes)
                {
                    for (var l = 0; l < self.cameraPositions.Length; l++)
                    {
                        if (effect.amount > 0f) 
                            self.AddObject(new GoldFlakesEffect(self));
                    }
                }
            }
        };
        IL.BigEel.ctor += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(20),
                x => x.MatchNewarr<BodyChunk>(),
                x => x.MatchCallOrCallvirt<PhysicalObject>("set_bodyChunks")))
            {
                c.Index += 2;
                c.Emit(Ldarg_0);
                c.EmitDelegate((int length, BigEel self) => self.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? length / 2 : length);
            }
            else
                logger.LogError("Couldn't ILHook BigEel.ctor! (part 1)");
            c.Index = il.Body.Instructions.Count - 1;
            c.Emit(Ldarg_0);
            c.EmitDelegate((BigEel self) =>
            {
                if (self.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel)
                {
                    var seed = Random.seed;
                    Random.seed = self.abstractCreature.ID.RandomSeed;
                    self.iVars.patternColorB = HSLColor.Lerp(RainWorld.GoldHSL, new HSLColor(RainWorld.GoldHSL.hue, RainWorld.GoldHSL.saturation, RainWorld.GoldHSL.lightness + (Random.value / 12f)), .5f);
                    self.iVars.patternColorA = RainWorld.GoldHSL;
                    self.iVars.patternColorA.hue = .5f;
                    self.iVars.patternColorA = HSLColor.Lerp(self.iVars.patternColorA, new(RainWorld.GoldHSL.hue + (Random.value / 50f), RainWorld.GoldHSL.saturation + (Random.value / 50f), RainWorld.GoldHSL.lightness + (Random.value / 4f)), .9f);
                    self.airFriction = .98f;
                    self.waterFriction = .999f;
                    self.gravity = 0f;
                    self.buoyancy = 1f;
                    self.bounce = 0f;
                    for (var i = 0; i < self.bodyChunks.Length; i++)
                        self.bodyChunks[i].rad *= .75f;
                    Random.seed = seed;
                }
            });
        };
        IL.BigEel.AccessSwimSpace += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(5)))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEel self) => self.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
            }
            else
                logger.LogError("Couldn't ILHook BigEel.AccessSwimSpace!");
        };
        IL.BigEel.Swim += il =>
        {
            ILCursor c = new(il);
            for (var i = 0; i < il.Instrs.Count; i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchCallOrCallvirt<BodyChunk>("get_submersion"))
                {
                    c.Goto(ins, MoveType.After);
                    c.Emit(Ldarg_0);
                    c.EmitDelegate((float submersion, BigEel self) => self.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? 1f : submersion);
                }
            }
        };
        IL.BigEel.JawsSnap += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>("room"),
                x => x.MatchLdcI4(244)))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((SoundID ID, BigEel self) => self.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? EnumExt_GoldenRegionJam.Flying_Leviathan_Bite : ID);
            }
            else
                logger.LogError("Couldn't ILHook BigEel.JawsSnap!");
        };
        On.BigEel.NewRoom += (orig, self, newRoom) =>
        {
            orig(self, newRoom);
            for (var i = 0; i < newRoom.roomSettings.placedObjects.Count; i++)
            {
                if (newRoom.roomSettings.placedObjects[i].type == EnumExt_GoldenRegionJam.GRJLeviathanPushBack)
                    self.antiStrandingZones.Add(newRoom.roomSettings.placedObjects[i]);
            }
        };
        IL.BigEelPather.FollowPath += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<RoomBorderExit>("type"),
                x => x.MatchLdcI4(5)))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEelPather self) => self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
            }
            else
                logger.LogError("Couldn't ILHook BigEelPather.FollowPath (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PathFinder>("world"),
                x => x.MatchLdfld<World>("seaAccessNodes")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((WorldCoordinate[] accessNodes, BigEelPather self) => self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? self.world.skyAccessNodes : accessNodes);
            }
            else
                logger.LogError("Couldn't ILHook BigEelPather.FollowPath (part 2)!");
        };
        IL.BigEelGraphics.ctor += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(60),
                x => x.MatchNewarr<TailSegment>(),
                x => x.MatchStfld<BigEelGraphics>("tail")))
            {
                c.Index += 2;
                c.Emit(Ldarg_0);
                c.EmitDelegate((int length, BigEelGraphics self) => self.eel.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? length / 2 + length / 4 : length);
            }
            else
                logger.LogError("Couldn't ILHook BigEelGraphics.ctor!");
        };
        IL.BigEelGraphics.Update += il =>
        {
            ILCursor c = new(il);
            c.Emit(Ldarg_0);
            c.EmitDelegate((BigEelGraphics self) =>
            {
                if (self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel && self.finSound is not null)
                    self.finSound.volume = 0f;
            });
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BigEelGraphics>("eel"),
                x => x.MatchLdfld<BigEel>("swimSpeed"),
                x => x.MatchCall<Mathf>("Lerp"),
                x => x.MatchDiv(),
                x => x.MatchSub(),
                x => x.MatchStfld<BigEelGraphics>("tailSwim")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((BigEelGraphics self) =>
                {
                    if (self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel)
                        self.tailSwim /= 2f;
                });
            }
            else 
                logger.LogError("Couldn't ILHook BigEelGraphics.Update (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BigEelGraphics>("eel"),
                x => x.MatchLdfld<UpdatableAndDeletable>("room"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BigEelGraphics>("tail"),
                x => x.MatchLdloc(out _),
                x => x.MatchLdelemRef(),
                x => x.MatchLdfld<BodyPart>("pos"),
                x => x.MatchCallOrCallvirt<Room>("PointSubmerged")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((bool flag, BigEelGraphics self) => self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel || flag);
            }
            else
                logger.LogError("Couldn't ILHook BigEelGraphics.Update (part 2)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BigEelGraphics>("eel"),
                x => x.MatchLdfld<UpdatableAndDeletable>("room"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BigEelGraphics>("fins"),
                x => x.MatchLdloc(out _),
                x => x.MatchLdelemRef(),
                x => x.MatchLdloc(out _),
                x => x.MatchLdloc(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdfld<BodyPart>("pos"),
                x => x.MatchCallOrCallvirt<Room>("PointSubmerged")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((bool flag, BigEelGraphics self) => self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel || flag);
            }
            else
                logger.LogError("Couldn't ILHook BigEelGraphics.Update (part 3)!");
        };
        On.BigEelGraphics.Reset += (orig, self) =>
        {
            orig(self);
            if (self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel && self.finSound is not null)
                self.finSound.volume = 0f;
        };
        On.BigEelGraphics.ApplyPalette += (orig, self, sLeaser, rCam, palette) =>
        {
            orig(self, sLeaser, rCam, palette);
            if (self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel)
            {
                var seed = Random.seed;
                Random.seed = be.abstractCreature.ID.RandomSeed;
                for (var k = 0; k < 2; k++)
                {
                    sLeaser.sprites[self.BeakSprite(k, 1)].color = Color.Lerp(sLeaser.sprites[self.BeakSprite(k, 1)].color, RainWorld.GoldRGB, .65f);
                    for (var l = 0; l < self.numberOfScales; l++)
                        sLeaser.sprites[self.ScaleSprite(l, k)].color = Color.Lerp(sLeaser.sprites[self.ScaleSprite(l, k)].color, HSLColor.Lerp(be.iVars.patternColorA, be.iVars.patternColorB, Random.value).rgb, .8f);
                    for (var m = 0; m < self.fins.Length; m++)
                        sLeaser.sprites[self.FinSprite(m, k)].color = HSLColor.Lerp(be.iVars.patternColorA, be.iVars.patternColorB, Random.value).rgb;
                }
                Random.seed = seed;
            }
        };
        On.BigEelGraphics.InitiateSprites += (orig, self, sLeaser, rCam) =>
        {
            orig(self, sLeaser, rCam);
            if (self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel)
            {
                for (var k = 0; k < 2; k++)
                {
                    for (var l = 0; l < self.fins.Length; l++)
                        sLeaser.sprites[self.FinSprite(l, k)].shader = rCam.game.rainWorld.Shaders["TentaclePlant"];
                }
            }
        };
        IL.BigEelAbstractAI.AbstractBehavior += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AbstractCreatureAI>("world"),
                x => x.MatchLdfld<World>("seaAccessNodes"),
                x => x.MatchLdlen(),
                x => x.MatchConvI4()))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((bool flag, BigEelAbstractAI self) => self.RealAI is BigEelAI beAI && beAI.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? self.world.skyAccessNodes.Length != 0 : flag);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AbstractBehavior!");
        };
        IL.BigEelAbstractAI.AddRandomCheckRoom += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AbstractCreatureAI>("world"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AbstractCreatureAI>("world"),
                x => x.MatchCallOrCallvirt<World>("get_firstRoomIndex"),
                x => x.MatchCallOrCallvirt(typeof(World).GetMethod("GetAbstractRoom", Public | NonPublic | Static | Instance, Type.DefaultBinder, new[] { typeof(int) }, null)),
                x => x.MatchLdfld<AbstractRoom>("nodes"),
                x => x.MatchLdloc(out _),
                x => x.MatchLdelema<AbstractRoomNode>(),
                x => x.MatchLdfld<AbstractRoomNode>("type"),
                x => x.MatchLdcI4(5)))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEelAbstractAI self) => self.RealAI is BigEelAI beAI && beAI.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AddRandomCheckRoom (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AbstractCreatureAI>("world"),
                x => x.MatchLdfld<World>("seaAccessNodes"),
                x => x.MatchLdcI4(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AbstractCreatureAI>("world"),
                x => x.MatchLdfld<World>("seaAccessNodes"),
                x => x.MatchLdlen(),
                x => x.MatchConvI4(),
                x => x.MatchCall(typeof(Random).GetMethod("Range", Public | NonPublic | Static | Instance, Type.DefaultBinder, new[] { typeof(int), typeof(int) }, null)),
                x => x.MatchLdelema<WorldCoordinate>(),
                x => x.MatchLdobj<WorldCoordinate>()))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((WorldCoordinate coord, BigEelAbstractAI self) => self.RealAI is BigEelAI beAI && beAI.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? self.world.skyAccessNodes[Random.Range(0, self.world.skyAccessNodes.Length)] : coord);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AddRandomCheckRoom (part 2)!");
        };
        IL.BigEelAbstractAI.AddRoomClusterToCheckList += il =>
        {
            ILCursor c = new(il);
            var loc1 = -1;
            c.TryGotoNext(
                x => x.MatchStloc(out loc1),
                x => x.MatchLdloc(loc1),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AbstractCreatureAI>("parent"),
                x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
                x => x.MatchLdfld<CreatureTemplate>("type"),
                x => x.MatchCallOrCallvirt<AbstractRoom>("AttractionForCreature"),
                x => x.MatchLdcI4(1));
            var loc2 = -1;
            if (c.TryGotoNext(
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(out _),
                x => x.MatchBr(out _),
                x => x.MatchLdloc(out _),
                x => x.MatchLdfld<AbstractRoom>("creatures"),
                x => x.MatchLdloc(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
                x => x.MatchLdfld<CreatureTemplate>("type"),
                x => x.MatchLdcI4(23),
                x => x.MatchBneUn(out _),
                x => x.MatchLdcI4(1),
                x => x.MatchStloc(out loc2))
                && loc1 != -1 && loc2 != -1)
            {
                c.Emit(Ldarg_0);
                c.Emit(Ldloc_S, il.Body.Variables[loc1]);
                c.EmitDelegate((BigEelAbstractAI self, AbstractRoom abstractRoom) =>
                {
                    if (self.RealAI is BigEelAI beAI && beAI.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel)
                    {
                        for (var i = 0; i < abstractRoom.creatures.Count; i++)
                        {
                            if (abstractRoom.creatures[i].creatureTemplate.type == EnumExt_GoldenRegionJam.FlyingBigEel)
                                return true;
                        }
                    }
                    return false;
                });
                c.Emit(Stloc_S, il.Body.Variables[loc2]);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AddRoomClusterToCheckList (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out _),
                x => x.MatchLdfld<AbstractRoom>("nodes"),
                x => x.MatchLdloc(out _),
                x => x.MatchLdelema<AbstractRoomNode>(),
                x => x.MatchLdfld<AbstractRoomNode>("type"),
                x => x.MatchLdcI4(5)))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEelAbstractAI self) => self.RealAI is BigEelAI beAI && beAI.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AddRoomClusterToCheckList (part 2)!");
        };
        new ILHook(typeof(BigEelAI).GetMethod("IUseARelationshipTracker.UpdateDynamicRelationship", Public | NonPublic | Static | Instance), il =>
        {
            ILCursor c = new(il);
            for (var i = 0; i < il.Instrs.Count; i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchLdfld<Room>("defaultWaterLevel"))
                {
                    c.Goto(ins, MoveType.After);
                    c.Emit(Ldarg_0);
                    c.EmitDelegate((int defaultLevel, BigEelAI self) => self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel && be.room is Room r ? r.TileHeight : defaultLevel);
                }
            }
            c.Index = il.Body.Instructions.Count - 1;
            c.Emit(Ldarg_0);
            c.Emit(Ldarg_1);
            c.EmitDelegate((CreatureTemplate.Relationship rel, BigEelAI self, RelationshipTracker.DynamicRelationship dRelation) =>
            {
                if (rel.type is CreatureTemplate.Relationship.Type.Eats && self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel && be.antiStrandingZones.Count > 0 && dRelation.trackerRep?.representedCreature?.realizedCreature?.mainBodyChunk is BodyChunk b)
                {
                    for (var j = 0; j < be.antiStrandingZones.Count; j++)
                    {
                        if (Custom.DistLess(b.pos, be.antiStrandingZones[j].pos, 100f))
                        {
                            rel.type = CreatureTemplate.Relationship.Type.Ignores;
                            rel.intensity = 0f;
                            break;
                        }
                    }
                }
                return rel;
            });
        });
        IL.BigEelAI.Update += il =>
        {
            ILCursor c = new(il);
            for (var i = 0; i < il.Instrs.Count; i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchLdfld<Room>("defaultWaterLevel"))
                {
                    c.Goto(ins, MoveType.After);
                    c.Emit(Ldarg_0);
                    c.EmitDelegate((int defaultLevel, BigEelAI self) => self.eel is BigEel be && be.Template.type == EnumExt_GoldenRegionJam.FlyingBigEel && be.room is Room r ? r.TileHeight : defaultLevel);
                }
            }
        };
        IL.SoundLoader.LoadSounds += il =>
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchCall(typeof(File), "ReadAllLines")))
            {
                c.EmitDelegate((string[] lines) =>
                {
                    var index = lines.Length;
                    Array.Resize(ref lines, lines.Length + EnumExt_GoldenRegionJam.soundLines.Length);
                    EnumExt_GoldenRegionJam.soundLines.CopyTo(lines, index);
                    return lines;
                });
            }
            else
                logger.LogError("Couldn't ILHook SoundLoader.LoadSounds!");
        };
        On.OverseerAbstractAI.HowInterestingIsCreature += (orig, self, testCrit) =>
        {
            if (testCrit is not null && testCrit.creatureTemplate.type == EnumExt_GoldenRegionJam.FlyingBigEel)
            {
                var num = .55f;
                if (testCrit.state.dead)
                    num /= 10f;
                num *= testCrit.Room.AttractionValueForCreature(self.parent.creatureTemplate.type);
                return num * Mathf.Lerp(.5f, 1.5f, self.world.game.SeededRandom(self.parent.ID.RandomSeed + testCrit.ID.RandomSeed));
            }
            return orig(self, testCrit);
        };
        On.RoomRealizer.RoomPerformanceEstimation += (orig, self, testRoom) =>
        {
            var res = orig(self, testRoom);
            for (var j = 0; j < testRoom.creatures.Count; j++)
            {
                if (testRoom.creatures[j].state.alive && testRoom.creatures[j].creatureTemplate.type == EnumExt_GoldenRegionJam.FlyingBigEel)
                    res += 290f;
            }
            return res;
        };
        On.DevInterface.MapPage.CreatureVis.CritString += (orig, crit) => crit.creatureTemplate.type == EnumExt_GoldenRegionJam.FlyingBigEel ? "FlEel" : orig(crit);
        On.DevInterface.MapPage.CreatureVis.CritCol += (orig, crit) => crit.creatureTemplate.type == EnumExt_GoldenRegionJam.FlyingBigEel ? RainWorld.GoldRGB + new Color(.2f, .2f, .2f) : orig(crit);
        On.AImap.TileAccessibleToCreature_IntVector2_CreatureTemplate += (orig, self, pos, crit) =>
        {
            if (crit.type == EnumExt_GoldenRegionJam.FlyingBigEel)
            {
                if (!crit.MovementLegalInRelationToWater(self.getAItile(pos).DeepWater, self.getAItile(pos).WaterSurface))
                    return false;
                if (crit.PreBakedPathingIndex == -1)
                    return false;
                for (var i = 0; i < self.room.accessModifiers.Count; i++)
                {
                    if (!self.room.accessModifiers[i].IsTileAccessible(pos, crit))
                        return false;
                }
                if (self.getAItile(pos).terrainProximity < 4)
                    return false;
                return crit.AccessibilityResistance(self.getAItile(pos).acc).Allowed;
            }
            return orig(self, pos, crit);
        };
        On.MultiplayerUnlocks.UnlockedCritters += (orig, ID) =>
        {
            var res = orig(ID);
            if (ID is MultiplayerUnlocks.LevelUnlockID.Hidden)
                res.Add(EnumExt_GoldenRegionJam.FlyingBigEel);
            return res;
        };
        On.WorldLoader.CreatureTypeFromString += (orig, s) => Regex.IsMatch(s, "/flyinglev(iathan)?/gi") ? EnumExt_GoldenRegionJam.FlyingBigEel : orig(s);
        On.ArenaBehaviors.SandboxEditor.StayOutOfTerrainIcon.AllowedTile += (orig, self, tst) =>
        {
            if (self.room is not null && self.room.readyForAI && (self.room.GetTile(tst).Terrain == 0 || self.room.GetTile(tst).Terrain is Room.Tile.TerrainType.Floor) && self.iconData.critType == EnumExt_GoldenRegionJam.FlyingBigEel)
                return self.room.aimap.getAItile(tst).terrainProximity > 4;
            return orig(self, tst);
        };
        On.ArenaBehaviors.SandboxEditor.CreaturePerfEstimate += delegate (On.ArenaBehaviors.SandboxEditor.orig_CreaturePerfEstimate orig, CreatureTemplate.Type critType, ref float linear, ref float exponential)
        {
            if (critType == EnumExt_GoldenRegionJam.FlyingBigEel)
            {
                linear += 4f;
                exponential += 1.2f;
            }
            else 
                orig(critType, ref linear, ref exponential);
        };
        IL.GarbageWormAI.Update += il =>
        {
            ILCursor c = new(il);
            var loc1 = -1;
            var loc2 = -1;
            if (c.TryGotoNext(
                x => x.MatchLdloc(out loc1),
                x => x.MatchLdfld<GarbageWormAI.CreatureInterest>("crit"),
                x => x.MatchLdfld<Tracker.CreatureRepresentation>("representedCreature"),
                x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
                x => x.MatchCallOrCallvirt<CreatureTemplate>("get_IsVulture"),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdcR4(1000f),
                x => x.MatchStloc(out loc2))
                && loc1 != -1 && loc2 != -1)
            {
                c.Emit(Ldloc, il.Body.Variables[loc1]);
                c.Emit(Ldloc, il.Body.Variables[loc2]);
                c.EmitDelegate((GarbageWormAI.CreatureInterest interest, float num) =>
                {
                    if (interest.crit.representedCreature.creatureTemplate.type == EnumExt_GoldenRegionJam.FlyingBigEel)
                        return 1000f;
                    return num;
                });
                c.Emit(Stloc, il.Body.Variables[loc2]);
            }
            else
                logger.LogError("Couldn't ILHook GarbageWormAI.Update!");
        };
        On.PathFinder.CoordinateReachableAndGetbackable += (orig, self, coord) =>
        {
            var res = orig(self, coord);
            if (coord.TileDefined && self.creature?.creatureTemplate.type == EnumExt_GoldenRegionJam.FlyingBigEel && self.creature.realizedCreature is BigEel be && be.antiStrandingZones.Count > 0 && be.room is not null)
            {
                for (var j = 0; j < be.antiStrandingZones.Count; j++)
                {
                    if (Custom.DistLess(be.room.MiddleOfTile(coord), be.antiStrandingZones[j].pos, 100f))
                    {
                        res = false;
                        break;
                    }
                }
            }
            return res;
        };
        new Hook(typeof(Fisobs.Core.Ext).GetMethod("LoadAtlasFromEmbRes", Public | NonPublic | Static | Instance), (Func<Assembly, string, FAtlas> orig, Assembly assembly, string resource) =>
        {
            if (assembly.FullName.Contains("GoldenRegionJam"))
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream is null) 
                    return null;
                var array = new byte[stream.Length];
                stream.Read(array, 0, array.Length);
                Texture2D texture2D = new(0, 0, TextureFormat.ARGB32, false) { anisoLevel = 1, filterMode = 0 };
                texture2D.LoadImage(array);
                return Futile.atlasManager.LoadAtlasFromTexture(resource, texture2D);
            }
            else 
                return orig(assembly, resource);
        });
    }
}