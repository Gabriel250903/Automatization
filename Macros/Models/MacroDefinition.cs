using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Automatization.Hotkeys;
using Automatization.Macros.Core;

namespace Automatization.Macros.Models
{
    public class MacroDefinition : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _name = string.Empty;
        private string _description = string.Empty;
        private HotKey _triggerHotKey = new(Key.None, ModifierKeys.None);
        private MacroTriggerMode _triggerMode = MacroTriggerMode.Forever;
        private MacroTargetMode _targetMode = MacroTargetMode.ProTanki;
        private double _durationSeconds = 10.0;
        private string _targetProcessName = "ProTanki";
        private bool _isEnabled = true;
        private bool _humanize = true;
        private string _scriptText = string.Empty;
        private List<MacroAction> _visualActions = [];
        private MacroExecutionState _state = MacroExecutionState.Idle;
        private int _runCount = 0;
        private DateTime? _lastRunTime;

        public Guid Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public HotKey TriggerHotKey
        {
            get => _triggerHotKey;
            set => SetField(ref _triggerHotKey, value);
        }

        public MacroTriggerMode TriggerMode
        {
            get => _triggerMode;
            set => SetField(ref _triggerMode, value);
        }

        public double DurationSeconds
        {
            get => _durationSeconds;
            set => SetField(ref _durationSeconds, value);
        }

        public MacroTargetMode TargetMode
        {
            get => _targetMode;
            set => SetField(ref _targetMode, value);
        }

        public string TargetProcessName
        {
            get => _targetProcessName;
            set => SetField(ref _targetProcessName, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        public bool Humanize
        {
            get => _humanize;
            set => SetField(ref _humanize, value);
        }

        public string ScriptText
        {
            get => _scriptText;
            set => SetField(ref _scriptText, value);
        }

        public List<MacroAction> VisualActions
        {
            get => _visualActions;
            set => SetField(ref _visualActions, value);
        }

        [JsonIgnore]
        public MacroExecutionState State
        {
            get => _state;
            set => SetField(ref _state, value);
        }

        [JsonIgnore]
        public int RunCount
        {
            get => _runCount;
            set => SetField(ref _runCount, value);
        }

        [JsonIgnore]
        public DateTime? LastRunTime
        {
            get => _lastRunTime;
            set => SetField(ref _lastRunTime, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null
        )
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
