using System;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Celeste.Mod;
using Celeste.Pico8;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace TAS.EverestInterop.Hitboxes {
    public static class HitboxLastFrame {
        private static ILHook IlHookPlayerOrigUpdate;
        private static CelesteTASModuleSettings Settings => CelesteTASModule.Settings;

        public static void Load() {
            IlHookPlayerOrigUpdate = new ILHook(typeof(Player).GetMethod("orig_Update"), ModPlayerOrigUpdate);
            On.Monocle.Hitbox.Render += HitboxOnRender;
            On.Monocle.Circle.Render += CircleOnRender;
        }

        public static void Unload() {
            IlHookPlayerOrigUpdate?.Dispose();
            On.Monocle.Hitbox.Render -= HitboxOnRender;
            On.Monocle.Circle.Render -= CircleOnRender;
        }

        private static void ModPlayerOrigUpdate(ILContext il) {
            ILCursor ilCursor = new ILCursor(il);
            if (ilCursor.TryGotoNext(MoveType.After, ins => ins.MatchCastclass<PlayerCollider>())) {
                ilCursor.Emit(OpCodes.Ldarg_0).EmitDelegate<Action<Player>>(player => player.SaveActualCollidePosition());
                ilCursor.Emit(OpCodes.Dup).EmitDelegate<Action<PlayerCollider>>(playerCollider => {
                    Entity entity = playerCollider.Entity;

                    if (entity == null ||  !Settings.ShowHitboxes || Settings.ShowActualEntityCollideHitbox == LastFrameHitboxesTypes.OFF) {
                        return;
                    }

                    entity.SaveActualCollidePosition();
                    entity.SaveActualCollidable();
                });
            }
        }

        private static void CircleOnRender(On.Monocle.Circle.orig_Render orig, Circle self, Camera camera, Color color) {
            DrawLastFrameHitbox(self, color, hitboxColor => orig(self, camera, hitboxColor));
        }

        private static void HitboxOnRender(On.Monocle.Hitbox.orig_Render orig, Hitbox self, Camera camera, Color color) {
            DrawLastFrameHitbox(self, color, hitboxColor => orig(self, camera, hitboxColor));
        }

        private static void DrawLastFrameHitbox(Collider self, Color color, Action<Color> invokeOrig) {
            Entity entity = self.Entity;

            if (!Settings.ShowHitboxes
                || Settings.ShowActualEntityCollideHitbox == LastFrameHitboxesTypes.OFF
                || entity.Get<PlayerCollider>() == null
                || entity.Scene?.Tracker.GetEntity<Player>() == null
                || entity.LoadActualCollidePosition() == null
                || entity.LoadActualCollidePosition() == entity.Position && Settings.ShowActualEntityCollideHitbox == LastFrameHitboxesTypes.Append
            ) {
                invokeOrig(color);
                return;
            }

            Color lastFrameColor = color;

            if (Settings.ShowActualEntityCollideHitbox == LastFrameHitboxesTypes.Append) {
                lastFrameColor = color.Invert();
            }

            if (entity.Collidable && !entity.LoadActualCollidable()) {
                lastFrameColor *= 0.5f;
            } else if (!entity.Collidable && entity.LoadActualCollidable()) {
                lastFrameColor *= 2f;
            }

            if (Settings.ShowActualEntityCollideHitbox == LastFrameHitboxesTypes.Append) {
                invokeOrig(color);
            }

            Vector2 lastPosition = entity.LoadActualCollidePosition().Value;
            Vector2 currentPosition = entity.Position;

            IEnumerable<PlayerCollider> playerColliders = entity.Components.GetAll<PlayerCollider>().ToArray();
            if (playerColliders.All(playerCollider => playerCollider.Collider != null)) {
                if (playerColliders.Any(playerCollider => playerCollider.Collider == self)) {
                    entity.Position = lastPosition;
                    invokeOrig(lastFrameColor);
                    entity.Position = currentPosition;
                } else {
                    invokeOrig(color);
                }
            } else {
                entity.Position = lastPosition;
                invokeOrig(lastFrameColor);
                entity.Position = currentPosition;
            }
        }
    }

    public enum LastFrameHitboxesTypes {
        OFF,
        Override,
        Append
    }
}