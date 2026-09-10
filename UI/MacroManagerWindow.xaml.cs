using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Automatization.Hotkeys;
using Automatization.Macros.Compiler;
using Automatization.Macros.Core;
using Automatization.Macros.Models;
using Automatization.Services;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using MediaColor = System.Windows.Media.Color;
using WpfMessageBox = Wpf.Ui.Controls.MessageBox;
using WpfMessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace Automatization.UI
{
    public partial class MacroManagerWindow : FluentWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public static List<Key> CommonKeys { get; } =
        [
            Key.D0,
            Key.D1,
            Key.D2,
            Key.D3,
            Key.D4,
            Key.D5,
            Key.D6,
            Key.D7,
            Key.D8,
            Key.D9,
            Key.A,
            Key.B,
            Key.C,
            Key.D,
            Key.E,
            Key.F,
            Key.G,
            Key.H,
            Key.I,
            Key.J,
            Key.K,
            Key.L,
            Key.M,
            Key.N,
            Key.O,
            Key.P,
            Key.Q,
            Key.R,
            Key.S,
            Key.T,
            Key.U,
            Key.V,
            Key.W,
            Key.X,
            Key.Y,
            Key.Z,
            Key.F1,
            Key.F2,
            Key.F3,
            Key.F4,
            Key.F5,
            Key.F6,
            Key.F7,
            Key.F8,
            Key.F9,
            Key.F10,
            Key.F11,
            Key.F12,
            Key.Space,
            Key.Return,
            Key.Tab,
            Key.Escape,
            Key.Back,
            Key.Delete,
            Key.Insert,
            Key.Up,
            Key.Down,
            Key.Left,
            Key.Right,
            Key.Home,
            Key.End,
            Key.PageUp,
            Key.PageDown,
        ];

        public static List<MacroMouseButton> MouseButtons { get; } =
        [MacroMouseButton.Left, MacroMouseButton.Right, MacroMouseButton.Middle];

        private readonly MacroService _macroService;
        private readonly ICollectionView _macrosView;
        private MacroDefinition? _selectedMacro;
        private readonly ObservableCollection<MacroAction> _currentVisualActions = [];
        private bool _isLoadingMacro = false;
        private bool _isUpdatingScriptFromUI = false;

        public MacroDefinition? SelectedMacro
        {
            get => _selectedMacro;
            set
            {
                if (_selectedMacro == value)
                {
                    return;
                }

                _isLoadingMacro = true;
                try
                {
                    if (_selectedMacro != null)
                    {
                        _selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;
                        SaveCurrentState();
                    }

                    _selectedMacro = value;

                    if (_selectedMacro != null)
                    {
                        _selectedMacro.PropertyChanged += SelectedMacro_PropertyChanged;
                    }

                    OnPropertyChanged();
                    LoadSelectedMacroInternal();
                }
                finally
                {
                    _isLoadingMacro = false;
                }
            }
        }

        private void SelectedMacro_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MacroDefinition.RunCount))
            {
                Dispatcher.Invoke(() =>
                {
                    if (_selectedMacro != null)
                    {
                        TxtRunCount.Text = _selectedMacro.RunCount.ToString();
                    }
                });
            }
        }

        public MacroManagerWindow(MacroService macroService)
        {
            _macroService = macroService;
            DataContext = this;
            InitializeComponent();

            CmbTriggerMode.ItemsSource = new[]
            {
                new { Mode = MacroTriggerMode.Forever, Display = "Run Forever (Toggle)" },
                new { Mode = MacroTriggerMode.Timed, Display = "Run for Duration" },
                new { Mode = MacroTriggerMode.Once, Display = "Run Once" },
            };
            CmbTriggerMode.SelectedValuePath = "Mode";
            CmbTriggerMode.DisplayMemberPath = "Display";

            CmbTargetMode.ItemsSource = new[]
            {
                new { Target = MacroTargetMode.ProTanki, Display = "ProTanki (Game Only)" },
                new { Target = MacroTargetMode.Global, Display = "Global (Any Window)" },
            };
            CmbTargetMode.SelectedValuePath = "Target";
            CmbTargetMode.DisplayMemberPath = "Display";

            _macrosView = CollectionViewSource.GetDefaultView(_macroService.Macros);
            _macrosView.Filter = FilterMacroItem;
            MacroListView.ItemsSource = _macrosView;
            VisualActionsItemsControl.ItemsSource = _currentVisualActions;

            _macroService.MacroStateChanged += OnMacroStateChanged;

            TxtMacroName.TextChanged += TxtMacroName_TextChanged;
            TxtMacroDescription.TextChanged += TxtMacroDescription_TextChanged;
            CmbTriggerMode.SelectionChanged += CmbTriggerMode_SelectionChanged;
            CmbTargetMode.SelectionChanged += CmbTargetMode_SelectionChanged;
            TxtDurationSeconds.TextChanged += TxtDurationSeconds_TextChanged;
            ChkHumanize.Click += ChkHumanize_Click;
            TxtScriptCode.TextChanged += TxtScriptCode_TextChanged;
            TxtScriptCode.LostFocus += TxtScriptCode_LostFocus;
            TxtScriptCode.AddHandler(
                ScrollViewer.ScrollChangedEvent,
                new ScrollChangedEventHandler(TxtScriptCode_ScrollChanged)
            );
            HotkeyBoxTrigger.HotKeyChanged += HotkeyBoxTrigger_HotKeyChanged;

            if (_macroService.Macros.Count > 0)
            {
                MacroListView.SelectedIndex = 0;
            }
            else
            {
                AddNewMacro();
            }

            Closed += MacroManagerWindow_Closed;
        }

        private void MacroManagerWindow_Closed(object? sender, EventArgs e)
        {
            _macroService.MacroStateChanged -= OnMacroStateChanged;
        }

        private void TxtMacroName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingMacro || _isUpdatingScriptFromUI)
            {
                return;
            }

            if (_selectedMacro == null)
            {
                if (string.IsNullOrWhiteSpace(TxtMacroName.Text))
                {
                    return;
                }
                AddNewMacro();
                if (_selectedMacro == null)
                {
                    return;
                }
            }

            _selectedMacro.Name = TxtMacroName.Text.Trim();
            string name = string.IsNullOrWhiteSpace(_selectedMacro.Name)
                ? "New Macro"
                : _selectedMacro.Name;
            UpdateSingleDirectiveInScript("macro", $"macro \"{name.Replace("\"", "\\\"")}\"");
        }

        private void TxtMacroDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingMacro || _selectedMacro == null)
            {
                return;
            }

            _selectedMacro.Description = TxtMacroDescription.Text.Trim();
        }

        private void CmbTriggerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingMacro || _isUpdatingScriptFromUI || _selectedMacro == null)
            {
                return;
            }

            if (CmbTriggerMode.SelectedValue is MacroTriggerMode tm)
            {
                _selectedMacro.TriggerMode = tm;
                PnlDuration.Visibility =
                    tm == MacroTriggerMode.Timed ? Visibility.Visible : Visibility.Collapsed;
                UpdateSingleDirectiveInScript("mode", $"mode: {tm}");
            }
        }

        private void CmbTargetMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingMacro || _isUpdatingScriptFromUI || _selectedMacro == null)
            {
                return;
            }

            if (CmbTargetMode.SelectedValue is MacroTargetMode targetM)
            {
                _selectedMacro.TargetMode = targetM;
                UpdateSingleDirectiveInScript("target", $"target: {targetM}");
            }
        }

        private void TxtDurationSeconds_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingMacro || _selectedMacro == null)
            {
                return;
            }

            if (
                double.TryParse(
                    TxtDurationSeconds.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double secs
                )
                && secs > 0
            )
            {
                _selectedMacro.DurationSeconds = secs;
            }
        }

        private void ChkHumanize_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingMacro || _isUpdatingScriptFromUI || _selectedMacro == null)
            {
                return;
            }

            _selectedMacro.Humanize = ChkHumanize.IsChecked ?? true;
            UpdateSingleDirectiveInScript(
                "humanize",
                $"humanize: {_selectedMacro.Humanize.ToString().ToLowerInvariant()}"
            );
        }

        private void HotkeyBoxTrigger_HotKeyChanged(HotKey newHotKey)
        {
            if (_isLoadingMacro || _isUpdatingScriptFromUI || _selectedMacro == null)
            {
                return;
            }

            _selectedMacro.TriggerHotKey = newHotKey;
            UpdateHotkeyConflictWarning();
            _macroService.ReRegisterMacroHotkeys();
            _macroService.SaveMacros();
            string hotkey = newHotKey.IsEmpty ? "None" : newHotKey.ToString();
            UpdateSingleDirectiveInScript("hotkey", $"hotkey: \"{hotkey}\"");
        }

        private void UpdateSingleDirectiveInScript(string directive, string line)
        {
            if (
                _isLoadingMacro
                || _isUpdatingScriptFromUI
                || _isSyncingTabs
                || _selectedMacro == null
            )
            {
                return;
            }

            if (TxtScriptCode != null && TxtScriptCode.IsKeyboardFocused)
            {
                return;
            }

            _isUpdatingScriptFromUI = true;
            try
            {
                string currentScript =
                    TxtScriptCode?.Text ?? _selectedMacro.ScriptText ?? string.Empty;
                string updatedScript = VisualActionConverter.UpdateScriptDirective(
                    currentScript,
                    directive,
                    line
                );
                if (!string.Equals(updatedScript, currentScript, StringComparison.Ordinal))
                {
                    _selectedMacro.ScriptText = updatedScript;
                    if (TxtScriptCode != null)
                    {
                        int caret = TxtScriptCode.SelectionStart;
                        int len = TxtScriptCode.SelectionLength;
                        TxtScriptCode.Text = updatedScript;
                        if (caret <= updatedScript.Length)
                        {
                            TxtScriptCode.Select(
                                caret,
                                Math.Min(len, updatedScript.Length - caret)
                            );
                        }
                    }
                    UpdateLineNumbers();
                    ValidateSyntaxSilently();
                }
            }
            finally
            {
                _isUpdatingScriptFromUI = false;
            }
        }

        private void TxtScriptCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingMacro || _isSyncingTabs || _isUpdatingScriptFromUI)
            {
                return;
            }

            if (_selectedMacro == null)
            {
                if (string.IsNullOrWhiteSpace(TxtScriptCode.Text))
                {
                    return;
                }
                AddNewMacro();
                if (_selectedMacro == null)
                {
                    return;
                }
            }

            _selectedMacro.ScriptText = TxtScriptCode.Text;
            UpdateLineNumbers();
            ValidateSyntaxSilently();
        }

        private void TxtScriptCode_LostFocus(object sender, RoutedEventArgs e)
        {
            if (
                _selectedMacro == null
                || _isLoadingMacro
                || _isUpdatingScriptFromUI
                || _isSyncingTabs
            )
            {
                return;
            }

            AutoScriptCompiler compiler = new();
            CompilationResult result = compiler.Compile(TxtScriptCode.Text, _selectedMacro);
            if (result.Success || result.HasMetadata)
            {
                SyncMacroToUIInputs(_selectedMacro);
            }
        }

        private void UpdateLineNumbers()
        {
            if (TxtLineNumbers == null || TxtScriptCode == null)
            {
                return;
            }

            int count = TxtScriptCode.Text.Split('\n').Length;
            TxtLineNumbers.Text = string.Join("\n", Enumerable.Range(1, Math.Max(1, count)));
        }

        private void TxtScriptCode_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (TxtLineNumbers != null && TxtScriptCode != null)
            {
                TxtLineNumbers.ScrollToVerticalOffset(TxtScriptCode.VerticalOffset);
            }
        }

        private void UpdateHotkeyConflictWarning()
        {
            if (PnlHotkeyConflict == null)
            {
                return;
            }

            if (_selectedMacro == null || _selectedMacro.TriggerHotKey.IsEmpty)
            {
                PnlHotkeyConflict.Visibility = Visibility.Collapsed;
                return;
            }

            bool hasConflict = _macroService.HasHotKeyConflict(
                _selectedMacro.TriggerHotKey,
                _selectedMacro.Id
            );
            PnlHotkeyConflict.Visibility = hasConflict ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool FilterMacroItem(object obj)
        {
            if (obj is not MacroDefinition macro)
            {
                return false;
            }

            string query = TxtSearchMacro?.Text?.Trim() ?? "";
            return string.IsNullOrEmpty(query)
                || macro.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || macro.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                || macro
                    .TriggerHotKey.ToString()
                    .Contains(query, StringComparison.OrdinalIgnoreCase)
                || macro.TargetMode.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private void TxtSearchMacro_TextChanged(object sender, TextChangedEventArgs e)
        {
            _macrosView.Refresh();
            if (
                SelectedMacro != null
                && !_macrosView.Cast<MacroDefinition>().Contains(SelectedMacro)
            )
            {
                SelectedMacro = _macrosView.Cast<MacroDefinition>().FirstOrDefault();
            }
        }

        private void OnMacroStateChanged(MacroDefinition macro, MacroExecutionState state)
        {
            Dispatcher.Invoke(() =>
            {
                if (SelectedMacro == macro)
                {
                    UpdateTestRunButtonState(state);
                    TxtRunCount.Text = macro.RunCount.ToString();
                }
            });
        }

        private void LoadSelectedMacroInternal()
        {
            if (_selectedMacro == null)
            {
                EditorPanel.IsEnabled = true;
                TxtMacroName.Text = string.Empty;
                TxtMacroDescription.Text = string.Empty;
                HotkeyBoxTrigger.HotKey = new HotKey(Key.None, ModifierKeys.None);
                CmbTriggerMode.SelectedValue = MacroTriggerMode.Forever;
                PnlDuration.Visibility = Visibility.Collapsed;
                TxtDurationSeconds.Text = "10.0";
                CmbTargetMode.SelectedValue = MacroTargetMode.ProTanki;
                ChkHumanize.IsChecked = true;
                TxtScriptCode.Text = string.Empty;
                TxtRunCount.Text = "0";
                _currentVisualActions.Clear();
                TxtEmptyVisualActions.Visibility = Visibility.Visible;
                if (TxtDiagnosticStatus != null)
                {
                    TxtDiagnosticStatus.Text = string.Empty;
                }
                UpdateLineNumbers();
                return;
            }

            EditorPanel.IsEnabled = true;
            TxtMacroName.Text = _selectedMacro.Name;
            TxtMacroDescription.Text = _selectedMacro.Description;
            HotkeyBoxTrigger.HotKey = _selectedMacro.TriggerHotKey;
            CmbTriggerMode.SelectedValue = _selectedMacro.TriggerMode;
            PnlDuration.Visibility =
                _selectedMacro.TriggerMode == MacroTriggerMode.Timed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            TxtDurationSeconds.Text = _selectedMacro.DurationSeconds.ToString(
                CultureInfo.InvariantCulture
            );
            CmbTargetMode.SelectedValue = _selectedMacro.TargetMode;
            ChkHumanize.IsChecked = _selectedMacro.Humanize;
            TxtScriptCode.Text = _selectedMacro.ScriptText;
            TxtRunCount.Text = _selectedMacro.RunCount.ToString();

            _currentVisualActions.Clear();
            if (_selectedMacro.VisualActions != null && _selectedMacro.VisualActions.Count > 0)
            {
                foreach (MacroAction action in _selectedMacro.VisualActions)
                {
                    _currentVisualActions.Add(action.Clone());
                }
            }
            else
            {
                AutoScriptCompiler compiler = new();
                CompilationResult result = compiler.Compile(
                    _selectedMacro.ScriptText,
                    _selectedMacro
                );
                if (result.Success && result.Instructions.Length > 0)
                {
                    List<MacroAction> actions = VisualActionConverter.FromInstructions(
                        result.Instructions
                    );
                    foreach (MacroAction a in actions)
                    {
                        _currentVisualActions.Add(a.Clone());
                    }
                    _selectedMacro.VisualActions = actions;
                }
            }

            UpdateTestRunButtonState(_selectedMacro.State);
            UpdateHotkeyConflictWarning();
            UpdateLineNumbers();
            TxtEmptyVisualActions.Visibility =
                _currentVisualActions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ValidateSyntaxSilently();
        }

        private void SyncMacroToUIInputs(MacroDefinition? macro)
        {
            if (macro == null)
            {
                return;
            }

            _isLoadingMacro = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(macro.Name))
                {
                    TxtMacroName.Text = macro.Name;
                }
                if (!string.IsNullOrWhiteSpace(macro.Description))
                {
                    TxtMacroDescription.Text = macro.Description;
                }
                HotkeyBoxTrigger.HotKey = macro.TriggerHotKey;
                CmbTriggerMode.SelectedValue = macro.TriggerMode;
                PnlDuration.Visibility =
                    macro.TriggerMode == MacroTriggerMode.Timed
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                TxtDurationSeconds.Text = macro.DurationSeconds.ToString(
                    CultureInfo.InvariantCulture
                );
                CmbTargetMode.SelectedValue = macro.TargetMode;
                ChkHumanize.IsChecked = macro.Humanize;

                UpdateHotkeyConflictWarning();
            }
            finally
            {
                _isLoadingMacro = false;
            }
        }

        private void UpdateTestRunButtonState(MacroExecutionState state)
        {
            if (state is MacroExecutionState.Running or MacroExecutionState.Paused)
            {
                TestRunButton.Content = "Stop";
                TestRunButton.Appearance = ControlAppearance.Danger;
                TestRunButton.Icon = new SymbolIcon(SymbolRegular.Stop24);
            }
            else
            {
                TestRunButton.Content = "Test Run";
                TestRunButton.Appearance = ControlAppearance.Primary;
                TestRunButton.Icon = new SymbolIcon(SymbolRegular.Play24);
            }
        }

        private void MacroListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingMacro)
            {
                return;
            }

            if (MacroListView.SelectedItem is MacroDefinition def)
            {
                SelectedMacro = def;
            }
        }

        private void AddMacroButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewMacro();
        }

        private void EnsureSelectedMacro()
        {
            if (SelectedMacro == null)
            {
                AddNewMacro();
            }
        }

        private void AddNewMacro()
        {
            string name = $"Macro {_macroService.Macros.Count + 1}";
            MacroDefinition newMacro = new()
            {
                Name = name,
                TriggerHotKey = new HotKey(Key.None, ModifierKeys.None),
                TriggerMode = MacroTriggerMode.Forever,
                DurationSeconds = 10.0,
                TargetMode = MacroTargetMode.ProTanki,
                Humanize = true,
                ScriptText =
                    $"macro \"{name}\"\nhotkey: \"None\"\nmode: Forever\ntarget: ProTanki\nhumanize: true\n\nmain:\n    sleep 50ms\n",
                VisualActions = [],
            };

            AutoScriptCompiler compiler = new();
            CompilationResult result = compiler.Compile(newMacro.ScriptText, newMacro);
            if (result.Success && result.Instructions.Length > 0)
            {
                newMacro.VisualActions = VisualActionConverter.FromInstructions(
                    result.Instructions
                );
            }

            _macroService.AddMacro(newMacro);
            SelectedMacro = newMacro;
            MacroListView.SelectedItem = newMacro;
        }

        private void DuplicateMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMacro == null)
            {
                return;
            }

            SaveCurrentState();

            string copyName = $"{SelectedMacro.Name} (Copy)";
            MacroDefinition duplicate = new()
            {
                Name = copyName,
                Description = SelectedMacro.Description,
                TriggerHotKey = new HotKey(Key.None, ModifierKeys.None),
                TriggerMode = SelectedMacro.TriggerMode,
                DurationSeconds = SelectedMacro.DurationSeconds,
                TargetMode = SelectedMacro.TargetMode,
                Humanize = SelectedMacro.Humanize,
                ScriptText = VisualActionConverter.UpdateScriptDirective(
                    SelectedMacro.ScriptText,
                    "macro",
                    $"macro \"{copyName.Replace("\"", "\\\"")}\""
                ),
                VisualActions = SelectedMacro.VisualActions.Select(a => a.Clone()).ToList(),
            };

            duplicate.ScriptText = VisualActionConverter.UpdateScriptDirective(
                duplicate.ScriptText,
                "hotkey",
                "hotkey: \"None\""
            );

            _macroService.AddMacro(duplicate);
            SelectedMacro = duplicate;
            MacroListView.SelectedItem = duplicate;
        }

        private async void ImportMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new()
                {
                    Title = "Import Automatization Macro",
                    Filter = MacroPackage.FileFilter,
                    DefaultExt = MacroPackage.FileExtension,
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    MacroDefinition imported = _macroService.ImportMacroFromFile(
                        openFileDialog.FileName
                    );
                    SelectedMacro = imported;
                    MacroListView.SelectedItem = imported;

                    WpfMessageBox uiMessageBox = new()
                    {
                        Title = "Import Successful",
                        Content = $"Successfully imported macro '{imported.Name}'!",
                        PrimaryButtonText = "OK",
                    };
                    _ = await uiMessageBox.ShowDialogAsync();
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to import macro", ex);
                WpfMessageBox uiMessageBox = new()
                {
                    Title = "Import Failed",
                    Content = $"Failed to import macro:\n{ex.Message}",
                    PrimaryButtonText = "OK",
                };
                _ = await uiMessageBox.ShowDialogAsync();
            }
        }

        private async void ExportMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedMacro == null)
                {
                    WpfMessageBox warningBox = new()
                    {
                        Title = "No Macro Selected",
                        Content = "Please select a macro from the list to export.",
                        PrimaryButtonText = "OK",
                    };
                    _ = await warningBox.ShowDialogAsync();
                    return;
                }

                SaveCurrentState();

                string safeName = string.Join(
                    "_",
                    SelectedMacro.Name.Split(Path.GetInvalidFileNameChars())
                );
                if (string.IsNullOrWhiteSpace(safeName))
                {
                    safeName = "Macro";
                }

                Microsoft.Win32.SaveFileDialog saveFileDialog = new()
                {
                    Title = "Export Automatization Macro",
                    Filter = MacroPackage.FileFilter,
                    DefaultExt = MacroPackage.FileExtension,
                    FileName = $"{safeName}{MacroPackage.FileExtension}",
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    MacroService.ExportMacroToFile(SelectedMacro, saveFileDialog.FileName);

                    WpfMessageBox uiMessageBox = new()
                    {
                        Title = "Export Successful",
                        Content =
                            $"Successfully exported '{SelectedMacro.Name}' to:\n{saveFileDialog.FileName}",
                        PrimaryButtonText = "OK",
                    };
                    _ = await uiMessageBox.ShowDialogAsync();
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to export macro", ex);
                WpfMessageBox uiMessageBox = new()
                {
                    Title = "Export Failed",
                    Content = $"Failed to export macro:\n{ex.Message}",
                    PrimaryButtonText = "OK",
                };
                _ = await uiMessageBox.ShowDialogAsync();
            }
        }

        private void DocsMacroButton_Click(object sender, RoutedEventArgs e)
        {
            MacroDocsWindow docsWindow = new() { Owner = this };
            docsWindow.Show();
        }

        private async void DeleteMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedMacro == null)
                {
                    return;
                }

                WpfMessageBox uiMessageBox = new()
                {
                    Title = "Confirm Delete",
                    Content = $"Are you sure you want to delete '{SelectedMacro.Name}'?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No",
                };

                if (await uiMessageBox.ShowDialogAsync() == WpfMessageBoxResult.Primary)
                {
                    MacroDefinition toRemove = SelectedMacro;
                    _selectedMacro = null;
                    _macroService.RemoveMacro(toRemove);
                    if (_macroService.Macros.Count > 0)
                    {
                        SelectedMacro = _macroService.Macros[0];
                        MacroListView.SelectedItem = SelectedMacro;
                    }
                    else
                    {
                        AddNewMacro();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to delete macro", ex);
            }
        }

        private bool _isSyncingTabs = false;

        private void EditorTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (
                e.Source != EditorTabControl
                || _isSyncingTabs
                || _isLoadingMacro
                || _selectedMacro == null
            )
            {
                return;
            }

            _isSyncingTabs = true;
            try
            {
                if (EditorTabControl.SelectedIndex == 1)
                {
                    string generatedScript = VisualActionConverter.ToScript(
                        _currentVisualActions,
                        _selectedMacro
                    );
                    TxtScriptCode.Text = generatedScript;
                    _selectedMacro.ScriptText = generatedScript;
                    _selectedMacro.VisualActions = _currentVisualActions
                        .Select(a => a.Clone())
                        .ToList();
                    UpdateLineNumbers();
                    ValidateSyntaxSilently();
                }
                else if (EditorTabControl.SelectedIndex == 0)
                {
                    AutoScriptCompiler compiler = new();
                    CompilationResult result = compiler.Compile(TxtScriptCode.Text, _selectedMacro);
                    if (result.Success)
                    {
                        SyncMacroToUIInputs(_selectedMacro);
                        _currentVisualActions.Clear();
                        List<MacroAction> actions = VisualActionConverter.FromInstructions(
                            result.Instructions
                        );
                        foreach (MacroAction a in actions)
                        {
                            _currentVisualActions.Add(a.Clone());
                        }
                        _selectedMacro.VisualActions = _currentVisualActions
                            .Select(a => a.Clone())
                            .ToList();
                        TxtEmptyVisualActions.Visibility =
                            _currentVisualActions.Count == 0
                                ? Visibility.Visible
                                : Visibility.Collapsed;
                    }
                    else
                    {
                        CompileDiagnostic first = result.Diagnostics.FirstOrDefault(d => d.IsError);
                        string errMsg =
                            first.Message != null
                                ? $"Line {first.Line}:{first.Column} - {first.Message}"
                                : "Syntax contains errors.";

                        EditorTabControl.SelectedIndex = 1;

                        WpfMessageBox uiMessageBox = new()
                        {
                            Title = "Syntax Error",
                            Content =
                                $"Cannot switch to Visual Builder because the code contains syntax errors:\n\n{errMsg}\n\nPlease fix the errors in the Code tab first.",
                            PrimaryButtonText = "OK",
                        };
                        _ = uiMessageBox.ShowDialogAsync();
                    }
                }
            }
            finally
            {
                _isSyncingTabs = false;
            }
        }

        private void OnVisualActionsUpdated()
        {
            if (_isLoadingMacro || _isSyncingTabs || _selectedMacro == null)
            {
                return;
            }

            TxtEmptyVisualActions.Visibility =
                _currentVisualActions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            _selectedMacro.VisualActions = _currentVisualActions.Select(a => a.Clone()).ToList();

            if (EditorTabControl.SelectedIndex == 0)
            {
                string generatedScript = VisualActionConverter.ToScript(
                    _currentVisualActions,
                    _selectedMacro
                );
                _selectedMacro.ScriptText = generatedScript;
                TxtScriptCode.Text = generatedScript;
                UpdateLineNumbers();
                ValidateSyntaxSilently();
            }

            _macroService.SaveMacros();
        }

        #region Visual Builder Actions

        private void ActionParam_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingMacro || _isSyncingTabs || _selectedMacro == null)
            {
                return;
            }

            if (sender is FrameworkElement fe && !fe.IsLoaded)
            {
                return;
            }

            OnVisualActionsUpdated();
        }

        private void ActionParam_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingMacro || _isSyncingTabs || _selectedMacro == null)
            {
                return;
            }

            if (sender is FrameworkElement fe && !fe.IsLoaded)
            {
                return;
            }

            OnVisualActionsUpdated();
        }

        private void PickCardCoordinate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is MacroAction action)
            {
                MacroCoordinatePickerWindow picker = new() { Owner = this };
                if (picker.ShowDialog() == true)
                {
                    action.X = (int)picker.SelectedPoint.X;
                    action.Y = (int)picker.SelectedPoint.Y;
                    if (action.IsColorVisible)
                    {
                        action.ColorHex = picker.SelectedColorHex;
                    }
                    OnVisualActionsUpdated();
                }
            }
        }

        private void AddKeyPressAction_Click(object sender, RoutedEventArgs e)
        {
            EnsureSelectedMacro();
            _currentVisualActions.Add(
                new MacroAction
                {
                    Type = MacroActionType.KeyPress,
                    Key = Key.D1,
                    HoldMs = 25,
                }
            );
            OnVisualActionsUpdated();
        }

        private void AddDelayAction_Click(object sender, RoutedEventArgs e)
        {
            EnsureSelectedMacro();
            _currentVisualActions.Add(
                new MacroAction { Type = MacroActionType.Delay, DelayMs = 150 }
            );
            OnVisualActionsUpdated();
        }

        private void AddMouseClickAction_Click(object sender, RoutedEventArgs e)
        {
            EnsureSelectedMacro();
            _currentVisualActions.Add(
                new MacroAction
                {
                    Type = MacroActionType.MouseClick,
                    MouseButton = MacroMouseButton.Left,
                    X = 500,
                    Y = 500,
                    HoldMs = 20,
                }
            );
            OnVisualActionsUpdated();
        }

        private void AddPixelCheckAction_Click(object sender, RoutedEventArgs e)
        {
            EnsureSelectedMacro();
            _currentVisualActions.Add(
                new MacroAction
                {
                    Type = MacroActionType.CheckPixel,
                    X = 100,
                    Y = 100,
                    ColorHex = "#00FF00",
                }
            );
            OnVisualActionsUpdated();
        }

        private void AddLoopAction_Click(object sender, RoutedEventArgs e)
        {
            EnsureSelectedMacro();
            _currentVisualActions.Add(
                new MacroAction { Type = MacroActionType.Repeat, RepeatCount = 5 }
            );
            OnVisualActionsUpdated();
        }

        private void MoveActionUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MacroAction action)
            {
                int index = _currentVisualActions.IndexOf(action);
                if (index > 0)
                {
                    _currentVisualActions.Move(index, index - 1);
                    OnVisualActionsUpdated();
                }
            }
        }

        private void MoveActionDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MacroAction action)
            {
                int index = _currentVisualActions.IndexOf(action);
                if (index >= 0 && index < _currentVisualActions.Count - 1)
                {
                    _currentVisualActions.Move(index, index + 1);
                    OnVisualActionsUpdated();
                }
            }
        }

        private void DeleteAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MacroAction action)
            {
                _ = _currentVisualActions.Remove(action);
                OnVisualActionsUpdated();
            }
        }

        private void SyncVisualToCode_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null)
            {
                return;
            }

            string generatedScript = VisualActionConverter.ToScript(
                _currentVisualActions,
                _selectedMacro
            );
            TxtScriptCode.Text = generatedScript;
            _selectedMacro.ScriptText = generatedScript;
            _selectedMacro.VisualActions = _currentVisualActions.Select(a => a.Clone()).ToList();
            UpdateLineNumbers();
            ValidateSyntaxSilently();
            _isSyncingTabs = true;
            try
            {
                EditorTabControl.SelectedIndex = 1;
            }
            finally
            {
                _isSyncingTabs = false;
            }
            _macroService.SaveMacros();
        }

        private async void SyncCodeToVisual_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null)
            {
                return;
            }

            try
            {
                AutoScriptCompiler compiler = new();
                CompilationResult result = compiler.Compile(TxtScriptCode.Text, _selectedMacro);
                if (result.Success)
                {
                    SyncMacroToUIInputs(_selectedMacro);
                    _currentVisualActions.Clear();
                    List<MacroAction> actions = VisualActionConverter.FromInstructions(
                        result.Instructions
                    );
                    foreach (MacroAction a in actions)
                    {
                        _currentVisualActions.Add(a.Clone());
                    }
                    _selectedMacro.VisualActions = _currentVisualActions
                        .Select(a => a.Clone())
                        .ToList();
                    TxtEmptyVisualActions.Visibility =
                        _currentVisualActions.Count == 0
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    _isSyncingTabs = true;
                    try
                    {
                        EditorTabControl.SelectedIndex = 0;
                    }
                    finally
                    {
                        _isSyncingTabs = false;
                    }
                    _macroService.SaveMacros();
                }
                else
                {
                    CompileDiagnostic first = result.Diagnostics.FirstOrDefault(d => d.IsError);
                    string errMsg =
                        first.Message != null
                            ? $"Line {first.Line}:{first.Column} - {first.Message}"
                            : "Syntax contains errors.";
                    WpfMessageBox uiMessageBox = new()
                    {
                        Title = "Syntax Error",
                        Content = $"Cannot generate visual blocks:\n{errMsg}\n\nFix code first.",
                        PrimaryButtonText = "OK",
                    };
                    _ = await uiMessageBox.ShowDialogAsync();
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to sync code to visual", ex);
            }
        }

        #endregion

        #region Coordinate & Color Picker & Snippets

        private void InsertSnippet(string snippet)
        {
            int caretIndex = TxtScriptCode.CaretIndex;
            if (caretIndex >= 0 && caretIndex <= TxtScriptCode.Text.Length)
            {
                TxtScriptCode.Text = TxtScriptCode.Text.Insert(caretIndex, snippet);
                TxtScriptCode.CaretIndex = caretIndex + snippet.Length;
            }
            else
            {
                TxtScriptCode.AppendText(snippet);
                TxtScriptCode.CaretIndex = TxtScriptCode.Text.Length;
            }
            _ = TxtScriptCode.Focus();
        }

        private void InsertWaitSnippet_Click(object sender, RoutedEventArgs e)
        {
            InsertSnippet("    sleep 100ms\n");
        }

        private void InsertKeySnippet_Click(object sender, RoutedEventArgs e)
        {
            InsertSnippet("    press \"1\" for 25ms\n");
        }

        private void InsertClickSnippet_Click(object sender, RoutedEventArgs e)
        {
            InsertSnippet("    click left at (500, 500)\n");
        }

        private void InsertPixelSnippet_Click(object sender, RoutedEventArgs e)
        {
            InsertSnippet("    if pixel(500, 500) == #FFFFFF:\n        sleep 50ms\n");
        }

        private void PickCoordinatesAndColor_Click(object sender, RoutedEventArgs e)
        {
            MacroCoordinatePickerWindow picker = new() { Owner = this };
            if (picker.ShowDialog() == true)
            {
                int x = (int)picker.SelectedPoint.X;
                int y = (int)picker.SelectedPoint.Y;
                string hex = picker.SelectedColorHex;

                if (EditorTabControl.SelectedIndex == 1)
                {
                    string snippet =
                        $"\n    # Sample captured target\n    click left at ({x}, {y})\n    if pixel({x}, {y}) == {hex}:\n        sleep 100ms\n";
                    InsertSnippet(snippet);
                    ValidateSyntaxSilently();
                }
                else
                {
                    _currentVisualActions.Add(
                        new MacroAction
                        {
                            Type = MacroActionType.CheckPixel,
                            X = x,
                            Y = y,
                            ColorHex = hex,
                        }
                    );
                    OnVisualActionsUpdated();
                }
            }
        }

        #endregion

        #region Syntax Check & Execution

        private void CheckSyntaxButton_Click(object sender, RoutedEventArgs e)
        {
            AutoScriptCompiler compiler = new();
            CompilationResult result = compiler.Compile(TxtScriptCode.Text, SelectedMacro);
            if (result.Success || result.HasMetadata)
            {
                SyncMacroToUIInputs(SelectedMacro);
            }

            if (result.Success)
            {
                TxtDiagnosticStatus.Foreground = new SolidColorBrush(
                    MediaColor.FromRgb(0x00, 0xE6, 0x76)
                );
                TxtDiagnosticStatus.Text =
                    $"✓ Syntax Valid: {result.Instructions.Length} instructions compiled.";
            }
            else
            {
                TxtDiagnosticStatus.Foreground = new SolidColorBrush(
                    MediaColor.FromRgb(0xF4, 0x43, 0x36)
                );
                TxtDiagnosticStatus.Text = string.Join(
                    "\n",
                    result.Diagnostics.Select(d => $"Line {d.Line}:{d.Column} - {d.Message}")
                );
            }
        }

        private void ValidateSyntaxSilently()
        {
            if (_selectedMacro == null)
            {
                return;
            }

            AutoScriptCompiler compiler = new();
            CompilationResult result = compiler.Compile(TxtScriptCode.Text, _selectedMacro);
            if (result.Success)
            {
                TxtDiagnosticStatus.Foreground = new SolidColorBrush(
                    MediaColor.FromRgb(0x00, 0xE6, 0x76)
                );
                TxtDiagnosticStatus.Text =
                    $"✓ Syntax Valid ({result.Instructions.Length} instructions)";
            }
            else
            {
                TxtDiagnosticStatus.Foreground = new SolidColorBrush(
                    MediaColor.FromRgb(0xF4, 0x43, 0x36)
                );
                CompileDiagnostic first = result.Diagnostics.FirstOrDefault(d => d.IsError);
                TxtDiagnosticStatus.Text = $"Line {first.Line}:{first.Column} - {first.Message}";
            }
        }

        private void TestRunButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMacro == null)
            {
                return;
            }

            SaveCurrentState();
            _macroService.ToggleMacro(SelectedMacro, isTestRun: true);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveCurrentState();
                AutoScriptCompiler compiler = new();
                CompilationResult result = compiler.Compile(TxtScriptCode.Text, SelectedMacro);
                if (!result.Success)
                {
                    CompileDiagnostic first = result.Diagnostics.FirstOrDefault(d => d.IsError);
                    WpfMessageBox uiMessageBox = new()
                    {
                        Title = "Saved with Syntax Warnings",
                        Content =
                            $"Warning: Macro '{SelectedMacro?.Name}' has syntax errors:\nLine {first.Line}:{first.Column} - {first.Message}\n\nThe macro has been saved, but will not run until errors are fixed.",
                        PrimaryButtonText = "OK",
                    };
                    _ = await uiMessageBox.ShowDialogAsync();
                }
                else
                {
                    WpfMessageBox uiMessageBox = new()
                    {
                        Title = "Saved",
                        Content = "Macro saved successfully!",
                        PrimaryButtonText = "OK",
                    };
                    _ = await uiMessageBox.ShowDialogAsync();
                }
                _macroService.SaveMacros();
                _macroService.ReRegisterMacroHotkeys();
                _macrosView.Refresh();
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to save macro", ex);
            }
        }

        private void SaveCurrentState()
        {
            if (SelectedMacro == null)
            {
                return;
            }

            if (EditorTabControl.SelectedIndex == 1)
            {
                SelectedMacro.ScriptText = TxtScriptCode.Text;
                AutoScriptCompiler compiler = new();
                CompilationResult result = compiler.Compile(
                    SelectedMacro.ScriptText,
                    SelectedMacro
                );
                if (result.Success || result.HasMetadata)
                {
                    SyncMacroToUIInputs(SelectedMacro);
                }

                if (result.Success)
                {
                    List<MacroAction> actions = VisualActionConverter.FromInstructions(
                        result.Instructions
                    );
                    SelectedMacro.VisualActions = actions;
                }
            }
            else
            {
                SelectedMacro.VisualActions = _currentVisualActions.Select(a => a.Clone()).ToList();
                string code = VisualActionConverter.ToScript(_currentVisualActions, SelectedMacro);
                SelectedMacro.ScriptText = code;
                TxtScriptCode.Text = code;
            }

            SelectedMacro.Name = TxtMacroName.Text.Trim();
            SelectedMacro.Description = TxtMacroDescription.Text.Trim();
            SelectedMacro.TriggerHotKey = HotkeyBoxTrigger.HotKey;
            if (CmbTriggerMode.SelectedValue is MacroTriggerMode tm)
            {
                SelectedMacro.TriggerMode = tm;
            }

            if (CmbTargetMode.SelectedValue is MacroTargetMode targetM)
            {
                SelectedMacro.TargetMode = targetM;
            }

            if (
                double.TryParse(
                    TxtDurationSeconds.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double secs
                )
                && secs > 0
            )
            {
                SelectedMacro.DurationSeconds = secs;
            }

            SelectedMacro.Humanize = ChkHumanize.IsChecked ?? true;

            _macroService.SaveMacros();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _macroService.MacroStateChanged -= OnMacroStateChanged;
            SaveCurrentState();
            _macroService.SaveMacros();
            _macroService.ReRegisterMacroHotkeys();
        }

        #endregion
    }
}
