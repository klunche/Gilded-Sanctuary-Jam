using MonoMod.Cil;
using static Mono.Cecil.Cil.OpCodes;
using Random = UnityEngine.Random;
using static System.Reflection.BindingFlags;
using UnityEngine;
using static GoldenRegionJam.GoldenRegionJamMain;
using System;
using MonoMod.RuntimeDetour;
using System.IO;
using RWCustom;
using DevInterface;
using System.Runtime.InteropServices;

namespace GoldenRegionJam;

static class Hooks
{
    public static void Apply()
    {
        On.Room.Loaded += (orig, self) =>
        {
            orig(self);
            for (var k = 0; k < self.roomSettings.effects.Count; k++)
            {
                var effect = self.roomSettings.effects[k];
                if (effect.type == RoomEffectType.GRJGoldenFlakes)
                    self.AddObject(new GoldFlakesEffect(self));
            }
            for (var i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                var pObj = self.roomSettings.placedObjects[i];
                if (pObj.type == PlacedObjectType.GRJLeviathanPushBack)
                    self.AddObject(new LeviathanPushbackObject(self, (pObj.data as PlacedObject.ResizableObjectData)!));
            }
        };
        On.PlacedObject.GenerateEmptyData += (orig, self) =>
        {
            orig(self);
            if (self.type == PlacedObjectType.GRJLeviathanPushBack)
                self.data = new PlacedObject.ResizableObjectData(self);
        };
        On.DevInterface.ObjectsPage.CreateObjRep += (orig, self, tp, pObj) =>
        {
            if (tp == PlacedObjectType.GRJLeviathanPushBack)
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
        On.DevInterface.ObjectsPage.DevObjectGetCategoryFromPlacedType += (orig, self, type) => type == PlacedObjectType.GRJLeviathanPushBack ? ObjectsPage.DevObjectCategories.Gameplay : orig(self, type);
        On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += (orig, self, type) => type == RoomEffectType.GRJGoldenFlakes ? RoomSettingsPage.DevEffectsCategories.Decorations : orig(self, type);
        On.Room.NowViewed += (orig, self) =>
        {
            orig(self);
            for (var j = 0; j < self.roomSettings.effects.Count; j++)
            {
                var effect = self.roomSettings.effects[j];
                if (effect.type == RoomEffectType.GRJGoldenFlakes)
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
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(20),
                x => x.MatchNewarr<BodyChunk>()))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((BodyChunk[] ar, BigEel self) => self.Template.type == CreatureTemplateType.FlyingBigEel ? new BodyChunk[10] : ar);
            }
            else
                logger.LogError("Couldn't ILHook BigEel.ctor!");
            c.Index = il.Body.Instructions.Count - 1;
            c.Emit(Ldarg_0);
            c.EmitDelegate((BigEel self) =>
            {
                if (self.Template.type == CreatureTemplateType.FlyingBigEel)
                {
                    var state = Random.state;
                    Random.InitState(self.abstractCreature.ID.RandomSeed);
                    self.iVars.patternColorB = HSLColor.Lerp(RainWorld.GoldHSL, new HSLColor(RainWorld.GoldHSL.hue, RainWorld.GoldHSL.saturation, RainWorld.GoldHSL.lightness + (Random.value / 12f)), .5f);
                    self.iVars.patternColorA = RainWorld.GoldHSL;
                    self.iVars.patternColorA.hue = .5f;
                    self.iVars.patternColorA = HSLColor.Lerp(self.iVars.patternColorA, new(RainWorld.GoldHSL.hue + (Random.value / 50f), RainWorld.GoldHSL.saturation + (Random.value / 50f), RainWorld.GoldHSL.lightness + (Random.value / 4f)), .9f);
                    self.airFriction = .98f;
                    self.waterFriction = .999f;
                    self.gravity = 0f;
                    self.buoyancy = 1f;
                    self.bounce = 0f;
                    self.albino = false;
                    for (var i = 0; i < self.bodyChunks.Length; i++)
                        self.bodyChunks[i].rad *= .75f;
                    Random.state = state;
                }
            });
        };
        IL.BigEel.AccessSwimSpace += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdsfld<AbstractRoomNode.Type>("SeaExit")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEel self) => self.Template.type == CreatureTemplateType.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
            }
            else
                logger.LogError("Couldn't ILHook BigEel.AccessSwimSpace!");
        };
        IL.BigEel.Swim += il =>
        {
            var c = new ILCursor(il);
            for (var i = 0; i < il.Instrs.Count; i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchCallOrCallvirt<BodyChunk>("get_submersion"))
                {
                    c.Goto(ins, MoveType.After);
                    c.Emit(Ldarg_0);
                    c.EmitDelegate((float submersion, BigEel self) => self.Template.type == CreatureTemplateType.FlyingBigEel ? 1f : submersion);
                }
            }
        };
        IL.BigEel.JawsSnap += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>("room"),
                x => x.MatchLdsfld<SoundID>("Leviathan_Bite")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((SoundID ID, BigEel self) => self.Template.type == CreatureTemplateType.FlyingBigEel ? NewSoundID.Flying_Leviathan_Bite : ID);
            }
            else
                logger.LogError("Couldn't ILHook BigEel.JawsSnap!");
        };
        On.BigEel.NewRoom += (orig, self, newRoom) =>
        {
            orig(self, newRoom);
            for (var i = 0; i < newRoom.roomSettings.placedObjects.Count; i++)
            {
                if (newRoom.roomSettings.placedObjects[i].type == PlacedObjectType.GRJLeviathanPushBack)
                    self.antiStrandingZones.Add(newRoom.roomSettings.placedObjects[i]);
            }
        };
        IL.BigEelPather.FollowPath += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<RoomBorderExit>("type"),
                x => x.MatchLdsfld<AbstractRoomNode.Type>("SeaExit")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEelPather self) => self.creature?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
            }
            else
                logger.LogError("Couldn't ILHook BigEelPather.FollowPath (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PathFinder>("world"),
                x => x.MatchLdfld<World>("seaAccessNodes")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((WorldCoordinate[] accessNodes, BigEelPather self) => self.creature?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel ? self.world.skyAccessNodes : accessNodes);
            }
            else
                logger.LogError("Couldn't ILHook BigEelPather.FollowPath (part 2)!");
        };
        IL.BigEelGraphics.ctor += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(60),
                x => x.MatchNewarr<TailSegment>()))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((TailSegment[] ar, BigEelGraphics self) => self.eel?.Template.type == CreatureTemplateType.FlyingBigEel ? new TailSegment[45] : ar);
            }
            else
                logger.LogError("Couldn't ILHook BigEelGraphics.ctor!");
            c.Index = il.Body.Instructions.Count - 1;
            c.Emit(Ldarg_0);
            c.EmitDelegate((BigEelGraphics self) =>
            {
                if (self.eel?.Template.type == CreatureTemplateType.FlyingBigEel)
                {
                    for (var n = 0; n < self.eyesData.Length; n++)
                        self.eyesData[n] *= .75f;
                }
            });
        };
        IL.BigEelGraphics.Update += il =>
        {
            var c = new ILCursor(il);
            c.Emit(Ldarg_0);
            c.EmitDelegate((BigEelGraphics self) =>
            {
                if (self.eel?.Template.type == CreatureTemplateType.FlyingBigEel && self.finSound is not null)
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
                    if (self.eel?.Template.type == CreatureTemplateType.FlyingBigEel)
                        self.tailSwim /= 2f;
                });
            }
            else 
                logger.LogError("Couldn't ILHook BigEelGraphics.Update!");
            for (var i = 0; i < il.Instrs.Count; i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchCallOrCallvirt<Room>("PointSubmerged"))
                {
                    c.Goto(ins, MoveType.After);
                    if (i != 0)
                    {
                        c.Emit(Ldarg_0);
                        c.EmitDelegate((bool sub, BigEelGraphics self) => self.eel?.Template.type == CreatureTemplateType.FlyingBigEel || sub);
                    }
                }
            }
        };
        On.BigEelGraphics.Reset += (orig, self) =>
        {
            orig(self);
            if (self.eel?.Template.type == CreatureTemplateType.FlyingBigEel && self.finSound != null)
                self.finSound.volume = 0f;
        };
        IL.BigEelGraphics.ApplyPalette += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdstr("_LeviathanColorA")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((string s, BigEelGraphics g) => g.eel?.Template.type == CreatureTemplateType.FlyingBigEel ? "_GRJLeviathanColorA" : s);
            }
            else
                logger.LogError("Couldn't ILHook BigEelGraphics.ApplyPalette (part 1)!");
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdstr("_LeviathanColorB")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((string s, BigEelGraphics g) => g.eel?.Template.type == CreatureTemplateType.FlyingBigEel ? "_GRJLeviathanColorB" : s);
            }
            else
                logger.LogError("Couldn't ILHook BigEelGraphics.ApplyPalette (part 2)!");
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdstr("_LeviathanColorHead")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((string s, BigEelGraphics g) => g.eel?.Template.type == CreatureTemplateType.FlyingBigEel ? "_GRJLeviathanColorHead" : s);
            }
            else
                logger.LogError("Couldn't ILHook BigEelGraphics.ApplyPalette (part 3)!");
        };
        On.BigEelGraphics.ApplyPalette += (orig, self, sLeaser, rCam, palette) =>
        {
            orig(self, sLeaser, rCam, palette);
            if (self.eel is BigEel be && be.Template.type == CreatureTemplateType.FlyingBigEel)
            {
                var state = Random.state;
                Random.InitState(be.abstractCreature.ID.RandomSeed);
                for (var k = 0; k < 2; k++)
                {
                    var beak = sLeaser.sprites[self.BeakSprite(k, 1)];
                    beak.color = Color.Lerp(beak.color, RainWorld.GoldRGB, .65f);
                    for (var l = 0; l < self.numberOfScales; l++)
                    {
                        var scl = sLeaser.sprites[self.ScaleSprite(l, k)];
                        scl.color = Color.Lerp(scl.color, HSLColor.Lerp(be.iVars.patternColorA, be.iVars.patternColorB, Random.value).rgb, .8f);
                    }
                    for (var m = 0; m < self.fins.Length; m++)
                        sLeaser.sprites[self.FinSprite(m, k)].color = HSLColor.Lerp(be.iVars.patternColorA, be.iVars.patternColorB, Random.value).rgb;
                }
                Random.state = state;
            }
        };
        On.BigEelGraphics.InitiateSprites += (orig, self, sLeaser, rCam) =>
        {
            orig(self, sLeaser, rCam);
            if (self.eel?.Template.type == CreatureTemplateType.FlyingBigEel)
            {
                if (_b is null)
                sLeaser.sprites[self.MeshSprite].shader = rCam.game.rainWorld.Shaders["GRJEelBody"];
                for (var k = 0; k < 2; k++)
                {
                    for (var l = 0; l < self.fins.Length; l++)
                        sLeaser.sprites[self.FinSprite(l, k)].shader = rCam.game.rainWorld.Shaders["TentaclePlant"];
                    for (var num = 0; num < 2; num++)
                    {
                        for (var n = 0; n < 4; n++)
                        {
                            if (n % 2 == 0)
                                sLeaser.sprites[self.BeakArmSprite(n, num, k)].scaleX *= .75f;
                            else
                                sLeaser.sprites[self.BeakArmSprite(n, num, k)].scale *= .75f;
                        }
                        sLeaser.sprites[self.BeakSprite(k, num)].element = Futile.atlasManager.GetElementWithName("FEelJaw" + (2 - k) + (num is 0 ? "A" : "B"));
                    }
                }
            }
        };
        On.BigEelGraphics.DrawSprites += (orig, self, sLeaser, rCam, timeStacker, camPos) =>
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.eel is BigEel be && be.Template.type == CreatureTemplateType.FlyingBigEel)
            {
                var vector4 = Vector2.Lerp(be.bodyChunks[0].lastPos, be.bodyChunks[0].pos, timeStacker);
                var vec = Vector2.Lerp(be.bodyChunks[1].lastPos, be.bodyChunks[1].pos, timeStacker);
                var vec2 = Vector2.Lerp(vector4, vec, .5f) - camPos;
                for (var k = 0; k < 2; k++)
                {
                    for (var num12 = 0; num12 < 2; num12++)
                    {
                        for (var num14 = 0; num14 < 4; num14++)
                        {
                            var s = sLeaser.sprites[self.BeakArmSprite(num14, num12, k)];
                            s.x = Mathf.Lerp(s.x, vec2.x, .2f);
                            s.y = Mathf.Lerp(s.y, vec2.y, .2f);
                        }
                    }
                }
            }
        };
        IL.BigEelAbstractAI.AbstractBehavior += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AbstractCreatureAI>("world"),
                x => x.MatchLdfld<World>("seaAccessNodes"),
                x => x.MatchLdlen()))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((int length, BigEelAbstractAI self) => self.parent?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel ? self.world.skyAccessNodes.Length : length);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AbstractBehavior!");
        };
        IL.BigEelAbstractAI.AddRandomCheckRoom += il =>
        {
            var c = new ILCursor(il);
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
                x => x.MatchLdsfld<AbstractRoomNode.Type>("SeaExit")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEelAbstractAI self) => self.parent?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
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
                x => x.MatchLdelemAny<WorldCoordinate>()))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((WorldCoordinate coord, BigEelAbstractAI self) => self.parent?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel ? self.world.skyAccessNodes[Random.Range(0, self.world.skyAccessNodes.Length)] : coord);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AddRandomCheckRoom (part 2)!");
        };
        IL.BigEelAbstractAI.AddRoomClusterToCheckList += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
                x => x.MatchLdfld<CreatureTemplate>("type"),
                x => x.MatchLdsfld<CreatureTemplate.Type>("BigEel")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((CreatureTemplate.Type type, BigEelAbstractAI self) => self.parent?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel ? CreatureTemplateType.FlyingBigEel : type);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AddRoomClusterToCheckList (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out _),
                x => x.MatchLdfld<AbstractRoom>("nodes"),
                x => x.MatchLdloc(out _),
                x => x.MatchLdelema<AbstractRoomNode>(),
                x => x.MatchLdfld<AbstractRoomNode>("type"),
                x => x.MatchLdsfld<AbstractRoomNode.Type>("SeaExit")))
            {
                c.Emit(Ldarg_0);
                c.EmitDelegate((AbstractRoomNode.Type nodeType, BigEelAbstractAI self) => self.parent?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel ? AbstractRoomNode.Type.SkyExit : nodeType);
            }
            else
                logger.LogError("Couldn't ILHook BigEelAbstractAI.AddRoomClusterToCheckList (part 2)!");
        };
        new ILHook(typeof(BigEelAI).GetMethod("IUseARelationshipTracker.UpdateDynamicRelationship", Public | NonPublic | Static | Instance), il =>
        {
            var c = new ILCursor(il);
            for (var i = 0; i < il.Instrs.Count; i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchLdfld<Room>("defaultWaterLevel"))
                {
                    c.Goto(ins, MoveType.After);
                    c.Emit(Ldarg_0);
                    c.EmitDelegate((int defaultLevel, BigEelAI self) => self.creature?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel && self.eel?.room is Room r ? r.TileHeight : defaultLevel);
                }
            }
            c.Index = il.Body.Instructions.Count - 1;
            c.Emit(Ldarg_0);
            c.Emit(Ldarg_1);
            c.EmitDelegate((CreatureTemplate.Relationship rel, BigEelAI self, RelationshipTracker.DynamicRelationship dRelation) =>
            {
                if (rel.type == CreatureTemplate.Relationship.Type.Eats && self.creature?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel && self.eel is BigEel be && be.antiStrandingZones.Count > 0 && dRelation.trackerRep?.representedCreature?.realizedCreature?.mainBodyChunk is BodyChunk b)
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
            var c = new ILCursor(il);
            for (var i = 0; i < il.Instrs.Count; i++)
            {
                var ins = il.Instrs[i];
                if (ins.MatchLdfld<Room>("defaultWaterLevel"))
                {
                    c.Goto(ins, MoveType.After);
                    c.Emit(Ldarg_0);
                    c.EmitDelegate((int defaultLevel, BigEelAI self) => self.creature?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel && self.eel?.room is Room r ? r.TileHeight : defaultLevel);
                }
            }
        };
        IL.SoundLoader.LoadSounds += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchCall(typeof(File), "ReadAllLines")))
            {
                c.EmitDelegate((string[] lines) =>
                {
                    var index = lines.Length;
                    Array.Resize(ref lines, lines.Length + NewSoundID.soundLines.Length);
                    NewSoundID.soundLines.CopyTo(lines, index);
                    return lines;
                });
            }
            else
                logger.LogError("Couldn't ILHook SoundLoader.LoadSounds!");
        };
        On.OverseerAbstractAI.HowInterestingIsCreature += (orig, self, testCrit) =>
        {
            if (testCrit?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel)
            {
                var num = .55f;
                if (testCrit.state.dead)
                    num /= 10f;
                num *= testCrit.Room.AttractionValueForCreature(self.parent.creatureTemplate.type);
                return num * Mathf.Lerp(.5f, 1.5f, self.world.game.SeededRandom(self.parent.ID.RandomSeed + testCrit.ID.RandomSeed));
            }
            return orig(self, testCrit);
        };
        IL.GarbageWormAI.Update += il =>
        {
            var c = new ILCursor(il);
            int loc1 = 0, loc2 = 0;
            if (c.TryGotoNext(
                x => x.MatchLdloc(out loc1),
                x => x.MatchLdfld<GarbageWormAI.CreatureInterest>("crit"),
                x => x.MatchLdfld<Tracker.CreatureRepresentation>("representedCreature"),
                x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
                x => x.MatchCallOrCallvirt<CreatureTemplate>("get_IsVulture"),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdcR4(1000f),
                x => x.MatchStloc(out loc2)))
            {
                var l2 = il.Body.Variables[loc2];
                c.Emit(Ldloc, il.Body.Variables[loc1]);
                c.Emit(Ldloc, l2);
                c.EmitDelegate((GarbageWormAI.CreatureInterest interest, float num) =>
                {
                    if (interest.crit.representedCreature.creatureTemplate.type == CreatureTemplateType.FlyingBigEel)
                        return 1000f;
                    return num;
                });
                c.Emit(Stloc, l2);
            }
            else
                logger.LogError("Couldn't ILHook GarbageWormAI.Update!");
        };
        On.PathFinder.CoordinateReachableAndGetbackable += (orig, self, coord) =>
        {
            var res = orig(self, coord);
            if (coord.TileDefined && self.creature?.creatureTemplate.type == CreatureTemplateType.FlyingBigEel && self.creature.realizedCreature is BigEel be && be.antiStrandingZones.Count > 0 && be.room is Room rm)
            {
                for (var j = 0; j < be.antiStrandingZones.Count; j++)
                {
                    if (Custom.DistLess(rm.MiddleOfTile(coord), be.antiStrandingZones[j].pos, 100f))
                    {
                        res = false;
                        break;
                    }
                }
            }
            return res;
        };
        On.MoreSlugcats.BigJellyFish.ValidGrabCreature += (orig, self, abs) => orig(self, abs) && abs.creatureTemplate.type != CreatureTemplateType.FlyingBigEel;
        On.MoreSlugcats.StowawayBugAI.WantToEat += (orig, self, input) => orig(self, input) && input != CreatureTemplateType.FlyingBigEel;
        On.ArenaCreatureSpawner.IsMajorCreature += (orig, type) => orig(type) || type == CreatureTemplateType.FlyingBigEel;
        On.SSOracleBehavior.CreatureJokeDialog += (orig, self) =>
        {
            orig(self);
            var type = self.CheckStrayCreatureInRoom();
            if (type == CreatureTemplateType.FlyingBigEel)
                self.dialogBox.NewMessage(self.Translate("How did you fit them inside here anyhow?"), 10);
        };
        On.SLOracleBehaviorHasMark.CreatureJokeDialog += (orig, self) =>
        {
            orig(self);
            var type = self.CheckStrayCreatureInRoom();
            if (type == CreatureTemplateType.FlyingBigEel)
                self.dialogBox.NewMessage(self.Translate("Your friend is very large, how did you fit them in here?"), 10);
        };
    }
}