using System.Windows;
using System.Windows.Input;
using Automatization.Hotkeys;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Automatization.Controls
{
    public class HotKeyBox : TextBox
    {
        public static readonly DependencyProperty HotKeyProperty =
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
                var newHotKey = e.NewValue as HotKey ?? new HotKey();
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

            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            if (key == Key.Back || key == Key.Delete)
            {
                HotKey = new HotKey();
                return;
            }

            if (modifiers != ModifierKeys.None || IsValidKey(key))
            {
                HotKey = new HotKey(key, modifiers);
            }

            Keyboard.Focus(this);
        }

        private static bool IsValidKey(Key key)
        {
            return key >= Key.F1 && key <= Key.F24 ||
                   key >= Key.D0 && key <= Key.D9 ||
                   key >= Key.A && key <= Key.Z ||
                   key >= Key.NumPad0 && key <= Key.NumPad9 ||
                   key == Key.Tab || key == Key.Enter || key == Key.Space ||
                   key == Key.OemTilde || key == Key.OemMinus || key == Key.OemPlus ||
                   key == Key.OemOpenBrackets || key == Key.OemCloseBrackets ||
                   key == Key.OemPipe || key == Key.OemSemicolon || key == Key.OemQuotes ||
                   key == Key.OemComma || key == Key.OemPeriod || key == Key.OemQuestion;
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
