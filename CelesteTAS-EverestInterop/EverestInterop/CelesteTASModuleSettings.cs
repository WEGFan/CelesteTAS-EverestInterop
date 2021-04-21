﻿using System;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TAS.EverestInterop.Hitboxes;
using TAS.EverestInterop.InfoHUD;
using TAS.Utils;

namespace TAS.EverestInterop {
    public class CelesteTasModuleSettings : EverestModuleSettings {
        private bool centerCamera;
        public bool Enabled { get; set; } = true;

        [SettingName("TAS_KEY_START")]
        [DefaultButtonBinding(0, Keys.RightControl)]
        public ButtonBinding KeyStart { get; set; } = new();

        [SettingName("TAS_KEY_RESTART")]
        [DefaultButtonBinding(0, Keys.OemPlus)]
        public ButtonBinding KeyRestart { get; set; } = new();

        [SettingName("TAS_KEY_FAST_FORWARD")]
        [DefaultButtonBinding(0, Keys.RightShift)]
        public ButtonBinding KeyFastForward { get; set; } = new();

        [SettingName("TAS_KEY_FRAME_ADVANCE")]
        [DefaultButtonBinding(0, Keys.OemOpenBrackets)]
        public ButtonBinding KeyFrameAdvance { get; set; } = new();

        [SettingName("TAS_KEY_PAUSE")]
        [DefaultButtonBinding(0, Keys.OemCloseBrackets)]
        public ButtonBinding KeyPause { get; set; } = new();

        [SettingName("TAS_KEY_HITBOXES")]
        [DefaultButtonBinding(0, Keys.B)]
        public ButtonBinding KeyHitboxes { get; set; } = new();

        [SettingName("TAS_KEY_TRIGGER_HITBOXES")]
        [DefaultButtonBinding(0, Keys.LeftAlt)]
        public ButtonBinding KeyTriggerHitboxes { get; set; } = new();

        [SettingName("TAS_KEY_GRAPHICS")]
        [DefaultButtonBinding(0, Keys.N)]
        public ButtonBinding KeyGraphics { get; set; } = new();

        [SettingName("TAS_KEY_CAMERA")]
        [DefaultButtonBinding(0, Keys.M)]
        public ButtonBinding KeyCamera { get; set; } = new();

        // Multiple default keys are not supported, handled by Hotkeys.ModReload()
        [SettingName("TAS_KEY_SAVE_STATE")]
        [DefaultButtonBinding(0, Keys.OemMinus)]
        public ButtonBinding KeySaveState { get; set; } = new();

        // Multiple default keys are not supported, handled by Hotkeys.ModReload()
        [SettingName("TAS_KEY_CLEAR_STATE")]
        [DefaultButtonBinding(0, Keys.Back)]
        public ButtonBinding KeyClearState { get; set; } = new();

        public bool ShowHitboxes {
            get => GameplayRenderer.RenderDebug;
            set => GameplayRenderer.RenderDebug = value;
        }

        public bool CenterCamera {
            get => Enabled && centerCamera;
            set => centerCamera = value;
        }

        public bool DisableAchievements { get; set; } = false;

        [SettingNeedsRelaunch] public bool UnixRtc { get; set; } = false;

        [SettingNeedsRelaunch] public bool LaunchStudioAtBoot { get; set; } = false;

        public bool Mod9DLighting { get; set; } = false;

        [SettingIgnore] public bool FastForwardCallBase { get; set; } = false;
        [SettingIgnore] public int FastForwardThreshold { get; set; } = 10;
        [SettingIgnore] public DateTime StudioLastModifiedTime { get; set; } = new();
        public bool AutoExtractNewStudio { get; set; } = true;
        [SettingIgnore] public Color EntityHitboxColor { get; set; } = HitboxColor.DefaultEntityColor;
        [SettingIgnore] public Color TriggerHitboxColor { get; set; } = HitboxColor.DefaultTriggerColor;
        [SettingIgnore] public Color PlatformHitboxColor { get; set; } = HitboxColor.DefaultPlatformColor;
        public bool HideTriggerHitboxes { get; set; } = false;
        public bool SimplifiedHitboxes { get; set; } = true;
        public ActualCollideHitboxTypes ShowActualCollideHitboxes { get; set; } = ActualCollideHitboxTypes.Off;
        public bool PauseAfterLoadState { get; set; } = true;

        public bool RestoreSettings { get; set; } = false;
        [SettingIgnore] public bool FirstLaunch { get; set; } = true;

        // for hot reloading
        // ReSharper disable once UnusedMember.Local
        [Load]
        private static void RestoreHitboxSetting() {
            GameplayRenderer.RenderDebug =
                Engine.Instance.GetDynDataInstance().Get<bool?>(nameof(CelesteTasModule.Settings.ShowHitboxes)) ?? false;
        }

        // for hot reloading
        // ReSharper disable once UnusedMember.Local
        [Unload]
        private static void SaveHitboxSetting() {
            Engine.Instance.GetDynDataInstance().Set(nameof(CelesteTasModule.Settings.ShowHitboxes), CelesteTasModule.Settings.ShowHitboxes);
        }

        #region SimplifiedGraphics

        private bool simplifiedGraphics = false;

        public bool SimplifiedGraphics {
            get => Enabled && simplifiedGraphics;
            set => simplifiedGraphics = value;
        }

        private bool hideGamePlay;

        public bool HideGameplay {
            get => hideGamePlay;
            set => hideGamePlay = ShowHitboxes = value;
        }

        public int? SimplifiedLighting { get; set; } = 10;
        public int? SimplifiedBloomBase { get; set; } = 0;
        public int? SimplifiedBloomStrength { get; set; } = 1;
        public SimplifiedGraphicsFeature.SpinnerColor SimplifiedSpinnerColor { get; set; } = SimplifiedGraphicsFeature.SpinnerColor.All[1];
        public bool SimplifiedDustSpriteEdge { get; set; } = true;
        public bool SimplifiedSpotlightWipe { get; set; } = true;
        public bool SimplifiedColorGrade { get; set; } = true;
        public bool SimplifiedBackdrop { get; set; } = true;
        public bool SimplifiedDecal { get; set; } = true;
        public bool SimplifiedParticle { get; set; } = true;
        public bool SimplifiedDistort { get; set; } = true;
        public bool SimplifiedMiniTextbox { get; set; } = true;
        public bool SimplifiedDreamBlock { get; set; } = true;
        public bool SimplifiedLava { get; set; } = true;
        public bool SimplifiedLightning { get; set; } = true;

        #endregion

        #region Info HUD

        public bool InfoHud { get; set; } = false;
        public bool InfoGame { get; set; } = true;
        public bool InfoTasInput { get; set; } = true;
        public bool InfoSubPixelIndicator { get; set; } = true;
        public bool InfoCustom { get; set; } = false;
        public InspectEntityTypes InfoInspectEntityType { get; set; } = InspectEntityTypes.Position;
        public bool InfoIgnoreTriggerWhenClickEntity { get; set; } = true;

        [SettingIgnore]
        public string InfoCustomTemplate { get; set; } =
            "Wind: {Level.Wind}\n" +
            "AutoJump: {Player.AutoJump} ({Player.AutoJumpTimer.toFrame()})\n" +
            "ForceMoveX: {Player.forceMoveX} ({Player.forceMoveXTimer.toFrame()})\n" +
            "Theo: {TheoCrystal.ExactPosition}\n" +
            "TheoCantGrab: {TheoCrystal.Hold.cannotHoldTimer.toFrame()}";

        [SettingIgnore] public Vector2 InfoPosition { get; set; } = Vector2.Zero;
        [SettingIgnore] public int InfoTextSize { get; set; } = 10;
        [SettingIgnore] public int InfoSubPixelIndicatorSize { get; set; } = 10;
        [SettingIgnore] public int InfoOpacity { get; set; } = 6;
        [SettingIgnore] public int InfoMaskedOpacity { get; set; } = 3;

        #endregion

        #region Round Values

        private bool roundPosition = true;
        private bool roundSpeed = true;
        private bool roundVelocity = true;
        private bool roundCustomInfo = true;

        public bool RoundPosition {
            get => roundPosition;
            set {
                roundPosition = value;
                GameInfo.Update();
            }
        }

        public bool RoundSpeed {
            get => roundSpeed;
            set {
                roundSpeed = value;
                GameInfo.Update();
            }
        }

        public bool RoundVelocity {
            get => roundVelocity;
            set {
                roundVelocity = value;
                GameInfo.Update();
            }
        }

        public bool RoundCustomInfo {
            get => roundCustomInfo;
            set {
                roundCustomInfo = value;
                GameInfo.Update();
            }
        }

        #endregion
    }
}