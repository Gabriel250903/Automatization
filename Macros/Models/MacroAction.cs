using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Automatization.Macros.Core;

namespace Automatization.Macros.Models
{
    public enum MacroActionType
    {
        KeyPress = 0,
        KeyDown = 1,
        KeyUp = 2,
        Delay = 3,
        DelayRange = 4,
        MouseClick = 5,
        MouseDown = 6,
        MouseUp = 7,
        MouseMove = 8,
        CheckPixel = 9,
        Repeat = 10,
        Stop = 11,
    }

    public class MacroAction : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? prop = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplaySummary)));
            }
        }

        private MacroActionType _type = MacroActionType.KeyPress;
        private Key _key = Key.D1;
        private double _holdMs = 25;
        private double _delayMs = 100;
        private double _minDelayMs = 100;
        private double _maxDelayMs = 150;
        private MacroMouseButton _mouseButton = MacroMouseButton.Left;
        private int _x = 0;
        private int _y = 0;
        private string _colorHex = "#FFFFFF";
        private int _repeatCount = 5;

        public MacroActionType Type
        {
            get => _type;
            set
            {
                SetField(ref _type, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsKeyAction)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHoldVisible)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDelayAction)));
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(IsDelayRangeAction))
                );
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMouseAction)));
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(IsCoordinatesVisible))
                );
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsColorVisible)));
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(IsRepeatVisible))
                );
            }
        }

        public Key Key
        {
            get => _key;
            set => SetField(ref _key, value);
        }

        public double HoldMs
        {
            get => _holdMs;
            set => SetField(ref _holdMs, value);
        }

        public double DelayMs
        {
            get => _delayMs;
            set => SetField(ref _delayMs, value);
        }

        public double MinDelayMs
        {
            get => _minDelayMs;
            set => SetField(ref _minDelayMs, value);
        }

        public double MaxDelayMs
        {
            get => _maxDelayMs;
            set => SetField(ref _maxDelayMs, value);
        }

        public MacroMouseButton MouseButton
        {
            get => _mouseButton;
            set => SetField(ref _mouseButton, value);
        }

        public int X
        {
            get => _x;
            set => SetField(ref _x, value);
        }

        public int Y
        {
            get => _y;
            set => SetField(ref _y, value);
        }

        public string ColorHex
        {
            get => _colorHex;
            set => SetField(ref _colorHex, value);
        }

        public int RepeatCount
        {
            get => _repeatCount;
            set => SetField(ref _repeatCount, value);
        }

        public List<MacroAction> Children { get; set; } = [];

        public MacroAction Clone()
        {
            return new MacroAction
            {
                Type = Type,
                Key = Key,
                HoldMs = HoldMs,
                DelayMs = DelayMs,
                MinDelayMs = MinDelayMs,
                MaxDelayMs = MaxDelayMs,
                MouseButton = MouseButton,
                X = X,
                Y = Y,
                ColorHex = ColorHex,
                RepeatCount = RepeatCount,
                Children = [.. Children.Select(c => c.Clone())],
            };
        }

        [JsonIgnore]
        public bool IsKeyAction =>
            Type is MacroActionType.KeyPress or MacroActionType.KeyDown or MacroActionType.KeyUp;

        [JsonIgnore]
        public bool IsHoldVisible => Type is MacroActionType.KeyPress or MacroActionType.MouseClick;

        [JsonIgnore]
        public bool IsDelayAction => Type == MacroActionType.Delay;

        [JsonIgnore]
        public bool IsDelayRangeAction => Type == MacroActionType.DelayRange;

        [JsonIgnore]
        public bool IsMouseAction =>
            Type
                is MacroActionType.MouseClick
                    or MacroActionType.MouseDown
                    or MacroActionType.MouseUp;

        [JsonIgnore]
        public bool IsCoordinatesVisible =>
            Type
                is MacroActionType.MouseClick
                    or MacroActionType.MouseDown
                    or MacroActionType.MouseUp
                    or MacroActionType.MouseMove
                    or MacroActionType.CheckPixel;

        [JsonIgnore]
        public bool IsColorVisible => Type == MacroActionType.CheckPixel;

        [JsonIgnore]
        public bool IsRepeatVisible => Type == MacroActionType.Repeat;

        [JsonIgnore]
        public string DisplaySummary =>
            Type switch
            {
                MacroActionType.KeyPress => $"Press '{FormatKey(Key)}' (hold {HoldMs:F0}ms)",
                MacroActionType.KeyDown => $"Hold down '{FormatKey(Key)}'",
                MacroActionType.KeyUp => $"Release '{FormatKey(Key)}'",
                MacroActionType.Delay => $"Wait {DelayMs:F0}ms",
                MacroActionType.DelayRange => $"Wait {MinDelayMs:F0}ms ~ {MaxDelayMs:F0}ms",
                MacroActionType.MouseClick => $"Click {MouseButton} at ({X}, {Y})",
                MacroActionType.MouseDown => $"Press {MouseButton} at ({X}, {Y})",
                MacroActionType.MouseUp => $"Release {MouseButton} at ({X}, {Y})",
                MacroActionType.MouseMove => $"Move cursor to ({X}, {Y})",
                MacroActionType.CheckPixel => $"If pixel ({X}, {Y}) == {ColorHex}",
                MacroActionType.Repeat => $"Repeat {RepeatCount} times",
                MacroActionType.Stop => "Stop macro",
                _ => Type.ToString(),
            };

        private static string FormatKey(Key key)
        {
            return key is >= Key.D0 and <= Key.D9
                ? ((int)key - (int)Key.D0).ToString()
                : key.ToString();
        }
    }
}
