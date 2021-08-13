using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Monocle;
using MonoMod.Utils;
using TAS.Communication;
using TAS.EverestInterop;
using TAS.Utils;

namespace TAS.Input {
    public class InputController {
        public static string StudioTasFilePath = string.Empty;
        public readonly SortedDictionary<int, List<Command>> Commands = new();
        public readonly SortedDictionary<int, FastForward> FastForwards = new();
        public readonly List<InputFrame> Inputs = new();
        public readonly Dictionary<string, DateTime> UsedFiles = new();

        private string checksum;
        private int initializationFrameCount;

        public static string TasFilePath {
            get {
                string defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "Celeste.tas");
                string path = string.IsNullOrEmpty(StudioTasFilePath) ? defaultPath : StudioTasFilePath;
                try {
                    if (!File.Exists(path)) {
                        File.WriteAllText(path, string.Empty);
                    }
                } catch {
                    return defaultPath;
                }

                return path;
            }
        }

        public int InputCurrentFrame { get; private set; }
        public int CurrentFrame { get; private set; }

        public InputFrame Previous => Inputs.GetValueOrDefault(CurrentFrame - 1);
        public InputFrame Current => Inputs.GetValueOrDefault(CurrentFrame);
        public InputFrame Next => Inputs.GetValueOrDefault(CurrentFrame + 1);
        public FastForward CurrentFastForward => FastForwards.GetValueOrDefault(CurrentFrame);
        public List<Command> CurrentCommands => Commands.GetValueOrDefault(CurrentFrame);
        private bool NeedsReload => UsedFiles.IsNotEmpty() && UsedFiles.Any(file => File.GetLastWriteTime(file.Key) != file.Value);
        public bool CanPlayback => CurrentFrame < Inputs.Count;
        public bool NeedsToWait => Manager.IsLoading();

        public bool HasFastForward => (FastForwards.LastValueOrDefault()?.Frame ?? -1) > CurrentFrame;
        public int FastForwardSpeed => FastForwards.LastValueOrDefault()?.Speed ?? 1;
        public bool Break => FastForwards.LastValueOrDefault()?.Frame == CurrentFrame;

        public string SavedChecksum {
            get => string.IsNullOrEmpty(checksum) ? Checksum() : checksum;
            private set => checksum = value;
        }

        public void RefreshInputs(bool enableRun) {
            if (enableRun) {
                InputCurrentFrame = 0;
                CurrentFrame = 0;
            }

            bool needsReload = NeedsReload;
            if (needsReload || enableRun) {
                int tryCount = 5;
                while (tryCount > 0) {
                    initializationFrameCount = 0;
                    checksum = string.Empty;
                    Inputs.Clear();
                    FastForwards.Clear();
                    Commands.Clear();
                    UsedFiles.Clear();
                    AnalogHelper.AnalogModeChange(AnalogueMode.Ignore);
                    if (ReadFile(TasFilePath)) {
                        LibTasHelper.FinishExport();
                        if (needsReload) {
                            InputCommands.UpdateRecordCount(this);
                        }

                        break;
                    }

                    // read file failed, rewrite the libtas inputs file.
                    LibTasHelper.RestartExport();

                    System.Threading.Thread.Sleep(50);
                    tryCount--;
                }

                CurrentFrame = Math.Min(Inputs.Count, CurrentFrame);
            }
        }

        public void Stop() {
            InputCurrentFrame = 0;
            CurrentFrame = 0;
        }

        public void AdvanceFrame() {
            RefreshInputs(false);

            if (NeedsToWait) {
                return;
            }

            CurrentCommands?.ForEach(command => command.Invoke());

            if (!CanPlayback) {
                return;
            }

            if (GameInfo.ExportSyncData) {
                GameInfo.ExportPlayerInfo();
            }

            Manager.SetInputs(Current);

            if (InputCurrentFrame == 0 || Current.Line == Previous.Line) {
                InputCurrentFrame++;
            } else {
                InputCurrentFrame = 1;
            }

            CurrentFrame++;
        }

        public void InitializeRecording() { }

        public bool ReadFile(string filePath, int startLine = 0, int endLine = int.MaxValue, int studioLine = 0) {
            try {
                if (filePath == TasFilePath && startLine == 0 && !File.Exists(filePath)) {
                    return false;
                }

                UsedFiles[filePath] = File.GetLastWriteTime(filePath);

                int subLine = 0;
                using StreamReader sr = new(filePath);
                while (!sr.EndOfStream) {
                    string lineText = sr.ReadLine().Trim();

                    subLine++;
                    if (subLine < startLine) {
                        continue;
                    }

                    if (subLine > endLine) {
                        break;
                    }

                    if (InputCommands.TryExecuteCommand(this, filePath, lineText, initializationFrameCount, studioLine))
                        //workaround for the play command
                    {
                        return true;
                    }

                    if (lineText.StartsWith("***")) {
                        FastForwards[initializationFrameCount] = new FastForward(initializationFrameCount, lineText.Substring(3), studioLine);
                    } else {
                        AddFrames(lineText, studioLine);
                    }

                    if (filePath == TasFilePath) {
                        studioLine++;
                    }
                }

                return true;
            } catch (Exception e) {
                e.Log();
                return false;
            }
        }

        public void AddFrames(string line, int studioLine) {
            if (!InputFrame.TryParse(line, studioLine, out InputFrame inputFrame)) {
                return;
            }

            for (int i = 0; i < inputFrame.Frames; i++) {
                Inputs.Add(inputFrame);
            }

            initializationFrameCount += inputFrame.Frames;
        }

        public InputController Clone() {
            InputController clone = new();

            clone.Inputs.AddRange(Inputs);
            clone.FastForwards.AddRange((IDictionary) FastForwards);
            foreach (int frame in Commands.Keys) {
                clone.Commands[frame] = new List<Command>(Commands[frame]);
            }

            clone.UsedFiles.AddRange(UsedFiles);
            clone.CurrentFrame = CurrentFrame;
            clone.InputCurrentFrame = InputCurrentFrame;
            clone.SavedChecksum = clone.Checksum();

            return clone;
        }

        public void CopyFrom(InputController controller) {
            InputCurrentFrame = controller.InputCurrentFrame;
            CurrentFrame = controller.CurrentFrame;
        }

        private string Checksum(int? toInputFrame = null) {
            toInputFrame ??= CurrentFrame;

            StringBuilder result = new(TasFilePath);
            result.AppendLine();

            try {
                int checkInputFrame = 0;

                while (checkInputFrame < toInputFrame) {
                    InputFrame currentInput = Inputs[checkInputFrame];
                    result.AppendLine(currentInput.ToActionsString());

                    if (Commands.GetValueOrDefault(checkInputFrame) is { } commands) {
                        foreach (Command command in commands.Where(command => command.Attribute.SavestateChecksum)) {
                            result.Append(command.LineText);
                        }
                    }

                    checkInputFrame++;
                }

                return SavedChecksum = HashHelper.ComputeHash(result.ToString());
            } catch {
                return SavedChecksum = HashHelper.ComputeHash(result.ToString());
            }
        }

        public string Checksum(InputController controller) => Checksum(controller.CurrentFrame);

        // for hot loading
        // ReSharper disable once UnusedMember.Local
        [Unload]
        private static void SaveStudioTasFilePath() {
            Engine.Instance.GetDynDataInstance().Set(nameof(StudioTasFilePath), StudioTasFilePath);
        }

        // ReSharper disable once UnusedMember.Local
        [Load]
        private static void RestoreStudioTasFilePath() {
            StudioTasFilePath = Engine.Instance.GetDynDataInstance().Get<string>(nameof(StudioTasFilePath));
        }

        #region ignore

        /*
        public void RecordPlayer() {
            InputRecord input = new InputRecord() { Line = inputIndex + 1, Frames = currentFrame };
            GetCurrentInputs(input);

            if (currentFrame == 0 && input == Current) {
                return;
            } else if (input != Current && !Manager.IsLoading()) {
                Current.Frames = currentFrame - Current.Frames;
                inputIndex++;
                if (Current.Frames != 0) {
                    inputs.Add(Current);
                }
                Current = input;
            }
            currentFrame++;
        }


        private static void GetCurrentInputs(InputRecord record) {
            if (Input.Jump.Check || Input.MenuConfirm.Check) {
                record.Actions |= Actions.Jump;
            }

            if (Input.Dash.Check || Input.MenuCancel.Check || Input.Talk.Check) {
                record.Actions |= Actions.Dash;
            }

            if (Input.Grab.Check) {
                record.Actions |= Actions.Grab;
            }

            if (Input.MenuJournal.Check) {
                record.Actions |= Actions.Journal;
            }

            if (Input.Pause.Check) {
                record.Actions |= Actions.Start;
            }

            if (Input.QuickRestart.Check) {
                record.Actions |= Actions.Restart;
            }

            if (Input.MenuLeft.Check || Input.MoveX.Value < 0) {
                record.Actions |= Actions.Left;
            }

            if (Input.MenuRight.Check || Input.MoveX.Value > 0) {
                record.Actions |= Actions.Right;
            }

            if (Input.MenuUp.Check || Input.MoveY.Value < 0) {
                record.Actions |= Actions.Up;
            }

            if (Input.MenuDown.Check || Input.MoveY.Value > 0) {
                record.Actions |= Actions.Down;
            }
        }

        /*
        public void WriteInputs() {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
                for (int i = 0; i < inputs.Count; i++) {
                    InputRecord record = inputs[i];
                    byte[] data = Encoding.ASCII.GetBytes(record.ToString() + "\r\n");
                    fs.Write(data, 0, data.Length);
                }
                fs.Close();
            }
        }
        */

        #endregion
    }
}