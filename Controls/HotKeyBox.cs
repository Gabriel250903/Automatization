using Automatization.Hotkeys;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Automatization.Controls
{
    public class HotKeyBox : TextBox
    {
        public static DependencyProperty HotKeyProperty =
            DependencyProperty.Register(nameof(HotKey), typeof(HotKey), typeof(HotKeyBox),
                new FrameworkPropertyMetadata(new HotKey(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotKeyChanged));

        public HotKey HotKey
        {
            get => (HotKey)GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }

        private static void OnHotKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotKeyBox box)
            {
                HotKey newHotKey = e.NewValue as HotKey ?? new HotKey();
                box.Text = newHotKey.ToString();
            }
        }

        public HotKeyBox()
        {
            IsReadOnly = true;
            IsReadOnlyCaretVisible = false;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true;

            ModifierKeys modifiers = Keyboard.Modifiers;
            Key key = e.Key;

            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            if (key is Key.LeftCtrl or Key.RightCtrl or
                Key.LeftShift or Key.RightShift or
                Key.LeftAlt or Key.RightAlt or
                Key.LWin or Key.RWin)
            {
                return;
            }

            if (key is Key.Back or Key.Delete)
            {
                HotKey = new HotKey();
                return;
            }

            if (modifiers != ModifierKeys.None || IsValidKey(key))
            {
                HotKey = new HotKey(key, modifiers);
            }

            _ = Keyboard.Focus(this);
        }

        private static bool IsValidKey(Key key)
        {
            return key is (>= Key.F1 and <= Key.F24) or
                   (>= Key.D0 and <= Key.D9) or
                   (>= Key.A and <= Key.Z) or
                   (>= Key.NumPad0 and <= Key.NumPad9) or
                   Key.Tab or Key.Enter or Key.Space or
                   Key.OemTilde or Key.OemMinus or Key.OemPlus or
                   Key.OemOpenBrackets or Key.OemCloseBrackets or
                   Key.OemPipe or Key.OemSemicolon or Key.OemQuotes or
                   Key.OemComma or Key.OemPeriod or Key.OemQuestion;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            GlobalHotKeyManager.IsPaused = true;

            SelectAll();
            Text = "Press a key...";
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            GlobalHotKeyManager.IsPaused = false;
            Text = HotKey.ToString();
        }
    }
}
