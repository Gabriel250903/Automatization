using System.IO;
using System.Windows;
using Wpf.Ui.Controls;

namespace Automatization.UI
{
    public partial class MacroDocsWindow : FluentWindow
    {
        private string _rawMarkdown = string.Empty;

        public MacroDocsWindow()
        {
            InitializeComponent();
            LoadDocumentation();
        }

        private void LoadDocumentation()
        {
            try
            {
                using Stream? stream = typeof(MacroDocsWindow).Assembly.GetManifestResourceStream(
                    "Automatization.Macros.README.md"
                );
                if (stream != null)
                {
                    using StreamReader reader = new(stream);
                    _rawMarkdown = reader.ReadToEnd();
                }
                else
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string readmePath = Path.Combine(baseDir, "Macros", "README.md");

                    if (!File.Exists(readmePath))
                    {
                        string projectPath = Path.GetFullPath(
                            Path.Combine(baseDir, "..", "..", "..", "Macros", "README.md")
                        );
                        if (File.Exists(projectPath))
                        {
                            readmePath = projectPath;
                        }
                    }

                    _rawMarkdown = File.Exists(readmePath)
                        ? File.ReadAllText(readmePath)
                        : GetDefaultDocumentation();
                }
            }
            catch
            {
                _rawMarkdown = GetDefaultDocumentation();
            }

            DocsViewer.Document = Automatization.Utils.MarkdownDocumentParser.Parse(_rawMarkdown);
        }

        private void CopyDocsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(_rawMarkdown);
            }
            catch { }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string GetDefaultDocumentation()
        {
            return @"# Automatization Macro Language (AutoScript) Reference

Every macro script is defined with:
- An entry function labeled 'main:'
- Indented instruction blocks (key presses, delays, mouse clicks, conditionals, loops)

## 1. Structure
main:
    press ""1"" for 25ms
    sleep 100ms
    click left at (500, 300)
    stop

## 2. Directives (Top of script)
macro ""Macro Name""
mode: Forever | Timed | Once
target: ProTanki | Global
humanize: true | false

## 3. Key Actions
- press ""<Key>"" for 25ms
- hold ""<Key>"" for 100ms
- key_down ""<Key>""
- key_up ""<Key>""

## 4. Delays & Sleep
- sleep 100ms
- wait 0.5s
- sleep 80ms ~ 140ms (random range)

## 5. Mouse Actions
- click left at (X, Y) for 20ms
- double_click left at (X, Y)
- mouse_down left at (X, Y)
- mouse_up left at (X, Y)
- move to (X, Y)

## 6. Conditionals & Loops
- if pixel(X, Y) == #FFFFFF:
- if pixel(X, Y) != #00FF00:
- repeat 5:
- stop";
        }
    }
}
