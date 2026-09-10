using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using Point = System.Windows.Point;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace Automatization.Utils
{
    public static partial class MarkdownDocumentParser
    {
        private static readonly FontFamily DefaultFont = new("Segoe UI, Helvetica Neue, Arial");
        private static readonly FontFamily CodeFont = new("Consolas, Cascadia Code, Courier New");

        private static readonly Brush PrimaryHeadingBrush = new SolidColorBrush(
            Color.FromRgb(0x40, 0xC4, 0xFF)
        );
        private static readonly Brush SecondaryHeadingBrush = new SolidColorBrush(
            Color.FromRgb(0x80, 0xD8, 0xFF)
        );
        private static readonly Brush TertiaryHeadingBrush = new SolidColorBrush(
            Color.FromRgb(0xBB, 0xDE, 0xFB)
        );
        private static readonly Brush TextBrush = new SolidColorBrush(
            Color.FromRgb(0xE0, 0xE0, 0xE0)
        );
        private static readonly Brush MutedBrush = new SolidColorBrush(
            Color.FromRgb(0xA0, 0xA0, 0xA0)
        );
        private static readonly Brush CodeBackgroundBrush = new SolidColorBrush(
            Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF)
        );
        private static readonly Brush CodeBlockBackgroundBrush = new SolidColorBrush(
            Color.FromRgb(0x13, 0x13, 0x1B)
        );
        private static readonly Brush CodeBlockBorderBrush = new SolidColorBrush(
            Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)
        );
        private static readonly Brush CodeTextBrush = new SolidColorBrush(
            Color.FromRgb(0x80, 0xD8, 0xFF)
        );
        private static readonly Brush TableBorderBrush = new SolidColorBrush(
            Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF)
        );
        private static readonly Brush TableHeaderBackgroundBrush = new SolidColorBrush(
            Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)
        );
        private static readonly Brush DividerBrush = new SolidColorBrush(
            Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF)
        );
        private static readonly Brush TocCardBackgroundBrush = new SolidColorBrush(
            Color.FromArgb(0x18, 0x12, 0x1A, 0x2A)
        );
        private static readonly Brush TocCardBorderBrush = new SolidColorBrush(
            Color.FromArgb(0x40, 0x40, 0xC4, 0xFF)
        );
        private static readonly Brush TocNumberBadgeBackground = new SolidColorBrush(
            Color.FromArgb(0x28, 0x40, 0xC4, 0xFF)
        );
        private static readonly Brush TocNumberBadgeBorder = new SolidColorBrush(
            Color.FromArgb(0x55, 0x40, 0xC4, 0xFF)
        );
        private static readonly Brush TocSubCardBackground = new SolidColorBrush(
            Color.FromArgb(0x0C, 0xFF, 0xFF, 0xFF)
        );
        private static readonly Brush TocSubCardBorder = new SolidColorBrush(
            Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)
        );
        private static readonly Brush TocHoverBackground = new SolidColorBrush(
            Color.FromArgb(0x1C, 0x40, 0xC4, 0xFF)
        );
        private static readonly Brush TocHoverBorder = new SolidColorBrush(
            Color.FromArgb(0x45, 0x40, 0xC4, 0xFF)
        );
        private static readonly Brush TocSubHoverBackground = new SolidColorBrush(
            Color.FromArgb(0x18, 0x80, 0xD8, 0xFF)
        );

        static MarkdownDocumentParser()
        {
            PrimaryHeadingBrush.Freeze();
            SecondaryHeadingBrush.Freeze();
            TertiaryHeadingBrush.Freeze();
            TextBrush.Freeze();
            MutedBrush.Freeze();
            CodeBackgroundBrush.Freeze();
            CodeBlockBackgroundBrush.Freeze();
            CodeBlockBorderBrush.Freeze();
            CodeTextBrush.Freeze();
            TableBorderBrush.Freeze();
            TableHeaderBackgroundBrush.Freeze();
            DividerBrush.Freeze();

            TocCardBackgroundBrush.Freeze();
            TocCardBorderBrush.Freeze();
            TocNumberBadgeBackground.Freeze();
            TocNumberBadgeBorder.Freeze();
            TocSubCardBackground.Freeze();
            TocSubCardBorder.Freeze();
            TocHoverBackground.Freeze();
            TocHoverBorder.Freeze();
            TocSubHoverBackground.Freeze();
        }

        [GeneratedRegex(@"^(\d+)\.\s+(.*)$")]
        private static partial Regex OrderedListRegex();

        [GeneratedRegex(
            @"(?<inlineCode>`[^`]+`)|(?<bold>\*\*[^*]+\*\*)|(?<italic>\*[^*]+\*)|(?<link>\[(?<linkText>[^\]]+)\]\((?<linkUrl>[^)]+)\))"
        )]
        private static partial Regex InlineMarkdownRegex();

        private class TocItem
        {
            public string Title { get; set; } = string.Empty;
            public string Anchor { get; set; } = string.Empty;
        }

        private class TocSection
        {
            public int Number { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Anchor { get; set; } = string.Empty;
            public List<TocItem> SubItems { get; } = [];
        }

        internal class HeadingAnchor
        {
            public string Text { get; set; } = string.Empty;
            public Paragraph Paragraph { get; set; } = null!;
            public FrameworkElement VisualAnchor { get; set; } = null!;
        }

        public static FlowDocument Parse(string markdown)
        {
            FlowDocument document = new()
            {
                PagePadding = new Thickness(24, 18, 24, 24),
                FontFamily = DefaultFont,
                FontSize = 13.5,
                Foreground = TextBrush,
                LineHeight = 22,
            };

            if (string.IsNullOrWhiteSpace(markdown))
            {
                return document;
            }

            Dictionary<string, HeadingAnchor> anchorMap = new(StringComparer.OrdinalIgnoreCase);
            List<HeadingAnchor> headings = [];

            string[] lines = markdown.Replace("\r\n", "\n").Split('\n');
            int i = 0;

            while (i < lines.Length)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    i++;
                    continue;
                }

                if (trimmed is "---" or "***" or "___" or "====")
                {
                    document.Blocks.Add(CreateHorizontalDivider());
                    i++;
                    continue;
                }

                if (trimmed.StartsWith("```"))
                {
                    i++;
                    List<string> codeLines = [];
                    while (i < lines.Length && !lines[i].Trim().StartsWith("```"))
                    {
                        codeLines.Add(lines[i]);
                        i++;
                    }
                    if (i < lines.Length && lines[i].Trim().StartsWith("```"))
                    {
                        i++;
                    }
                    document.Blocks.Add(CreateCodeBlock(string.Join("\n", codeLines)));
                    continue;
                }

                if (trimmed.StartsWith('#'))
                {
                    int headingLevel = 0;
                    while (headingLevel < trimmed.Length && trimmed[headingLevel] == '#')
                    {
                        headingLevel++;
                    }

                    if (
                        headingLevel <= 6
                        && headingLevel < trimmed.Length
                        && trimmed[headingLevel] == ' '
                    )
                    {
                        string headingText = trimmed[(headingLevel + 1)..].Trim();

                        if (
                            (
                                headingText.Equals(
                                    "Table of Contents",
                                    StringComparison.OrdinalIgnoreCase
                                )
                                || headingText.Equals(
                                    "Contents",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            && TryParseTableOfContents(
                                lines,
                                ref i,
                                anchorMap,
                                headings,
                                document,
                                out BlockUIContainer? tocContainer
                            )
                        )
                        {
                            if (tocContainer != null)
                            {
                                document.Blocks.Add(tocContainer);
                            }
                            continue;
                        }

                        document.Blocks.Add(
                            CreateHeading(headingText, headingLevel, anchorMap, headings, document)
                        );
                        i++;
                        continue;
                    }
                }

                if (trimmed.StartsWith('|') && trimmed.EndsWith('|'))
                {
                    List<string> tableLines = [];
                    while (
                        i < lines.Length
                        && lines[i].Trim().StartsWith('|')
                        && lines[i].Trim().EndsWith('|')
                    )
                    {
                        tableLines.Add(lines[i].Trim());
                        i++;
                    }

                    Table? tableBlock = TryParseTable(tableLines, anchorMap, headings, document);
                    if (tableBlock != null)
                    {
                        document.Blocks.Add(tableBlock);
                        continue;
                    }
                }

                if (trimmed.StartsWith('>'))
                {
                    List<string> quoteLines = [];
                    while (i < lines.Length && lines[i].Trim().StartsWith('>'))
                    {
                        string qLine = lines[i].Trim();
                        quoteLines.Add(
                            qLine.Length > 1 && qLine[1] == ' ' ? qLine[2..] : qLine[1..]
                        );
                        i++;
                    }
                    document.Blocks.Add(CreateQuoteBlock(string.Join(" ", quoteLines)));
                    continue;
                }

                if (
                    trimmed.StartsWith("- ")
                    || trimmed.StartsWith("* ")
                    || trimmed.StartsWith("+ ")
                    || OrderedListRegex().IsMatch(trimmed)
                )
                {
                    List listBlock = ParseListBlock(lines, ref i, anchorMap, headings, document);
                    document.Blocks.Add(listBlock);
                    continue;
                }

                List<string> paraLines = [];
                while (i < lines.Length)
                {
                    string pLine = lines[i].Trim();
                    if (
                        string.IsNullOrWhiteSpace(pLine)
                        || pLine.StartsWith('#')
                        || pLine.StartsWith("```")
                        || pLine.StartsWith("- ")
                        || pLine.StartsWith("* ")
                        || pLine.StartsWith("+ ")
                        || pLine is "---" or "***"
                        || (pLine.StartsWith('|') && pLine.EndsWith('|'))
                        || OrderedListRegex().IsMatch(pLine)
                    )
                    {
                        break;
                    }

                    paraLines.Add(pLine);
                    i++;
                }

                if (paraLines.Count > 0)
                {
                    Paragraph p = new() { Margin = new Thickness(0, 3, 0, 7) };
                    AppendFormattedInlines(
                        p.Inlines,
                        string.Join(" ", paraLines),
                        anchorMap,
                        headings,
                        document
                    );
                    document.Blocks.Add(p);
                }
            }

            return document;
        }

        private static bool TryParseTableOfContents(
            string[] lines,
            ref int i,
            Dictionary<string, HeadingAnchor> anchorMap,
            List<HeadingAnchor> headings,
            FlowDocument document,
            out BlockUIContainer? tocContainer
        )
        {
            tocContainer = null;
            int lookAhead = i + 1;

            while (lookAhead < lines.Length && string.IsNullOrWhiteSpace(lines[lookAhead]))
            {
                lookAhead++;
            }

            if (lookAhead >= lines.Length)
            {
                return false;
            }

            string firstItem = lines[lookAhead].Trim();
            if (
                !OrderedListRegex().IsMatch(firstItem)
                && !firstItem.StartsWith("- ")
                && !firstItem.StartsWith("* ")
            )
            {
                return false;
            }

            List<TocSection> sections = [];
            TocSection? currentSection = null;
            int sectionCounter = 1;

            i = lookAhead;

            while (i < lines.Length)
            {
                string curLine = lines[i];
                string curTrim = curLine.Trim();

                if (string.IsNullOrWhiteSpace(curTrim))
                {
                    int nextNonEmpty = i + 1;
                    while (
                        nextNonEmpty < lines.Length
                        && string.IsNullOrWhiteSpace(lines[nextNonEmpty])
                    )
                    {
                        nextNonEmpty++;
                    }

                    if (nextNonEmpty < lines.Length)
                    {
                        string peekTrim = lines[nextNonEmpty].Trim();
                        if (
                            !peekTrim.StartsWith('#')
                            && peekTrim is not ("---" or "***" or "___" or "====")
                            && (
                                OrderedListRegex().IsMatch(peekTrim)
                                || peekTrim.StartsWith("- ")
                                || peekTrim.StartsWith("* ")
                            )
                        )
                        {
                            i = nextNonEmpty;
                            continue;
                        }
                    }
                    break;
                }

                if (curTrim.StartsWith('#') || curTrim is "---" or "***" or "___" or "====")
                {
                    break;
                }

                int indent = GetIndent(curLine);
                bool isSubItem = indent >= 2;

                if (isSubItem)
                {
                    string subContent = curTrim;
                    if (
                        subContent.StartsWith("- ")
                        || subContent.StartsWith("* ")
                        || subContent.StartsWith("+ ")
                    )
                    {
                        subContent = subContent[2..].Trim();
                    }
                    else if (OrderedListRegex().IsMatch(subContent))
                    {
                        subContent = OrderedListRegex().Match(subContent).Groups[2].Value.Trim();
                    }

                    ExtractTitleAndAnchor(subContent, out string subTitle, out string subAnchor);

                    if (currentSection != null)
                    {
                        currentSection.SubItems.Add(
                            new TocItem { Title = subTitle, Anchor = subAnchor }
                        );
                    }
                    else
                    {
                        currentSection = new TocSection
                        {
                            Number = sectionCounter++,
                            Title = subTitle,
                            Anchor = subAnchor,
                        };
                        sections.Add(currentSection);
                    }
                }
                else
                {
                    Match numMatch = OrderedListRegex().Match(curTrim);
                    int secNum = sectionCounter++;
                    string content = curTrim;

                    if (numMatch.Success)
                    {
                        if (int.TryParse(numMatch.Groups[1].Value, out int pNum))
                        {
                            secNum = pNum;
                            sectionCounter = pNum + 1;
                        }
                        content = numMatch.Groups[2].Value.Trim();
                    }
                    else if (
                        curTrim.StartsWith("- ")
                        || curTrim.StartsWith("* ")
                        || curTrim.StartsWith("+ ")
                    )
                    {
                        content = curTrim[2..].Trim();
                    }

                    ExtractTitleAndAnchor(content, out string title, out string anchor);

                    currentSection = new TocSection
                    {
                        Number = secNum,
                        Title = title,
                        Anchor = anchor,
                    };
                    sections.Add(currentSection);
                }

                i++;
            }

            if (sections.Count == 0)
            {
                return false;
            }

            tocContainer = CreateTableOfContentsCard(sections, anchorMap, headings, document);
            return true;
        }

        private static void ExtractTitleAndAnchor(string text, out string title, out string anchor)
        {
            Match linkMatch = Regex.Match(text, @"\[(?<linkText>[^\]]+)\]\((?<linkUrl>[^)]+)\)");
            if (linkMatch.Success)
            {
                title = linkMatch.Groups["linkText"].Value;
                anchor = linkMatch.Groups["linkUrl"].Value;
            }
            else
            {
                title = text;
                anchor = string.Empty;
            }
        }

        private static BlockUIContainer CreateTableOfContentsCard(
            List<TocSection> sections,
            Dictionary<string, HeadingAnchor> anchorMap,
            List<HeadingAnchor> headings,
            FlowDocument document
        )
        {
            Border card = new()
            {
                Background = TocCardBackgroundBrush,
                BorderBrush = TocCardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 14, 18, 16),
                Margin = new Thickness(0, 10, 0, 18),
            };

            Grid headerGrid = new();
            headerGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel headerLeft = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };

            Border indexBadge = new()
            {
                Background = TocNumberBadgeBackground,
                BorderBrush = PrimaryHeadingBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "INDEX",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = PrimaryHeadingBrush,
                    FontFamily = DefaultFont,
                },
            };

            TextBlock titleText = new()
            {
                Text = "Table of Contents",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = PrimaryHeadingBrush,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            TextBlock subtitleText = new()
            {
                Text = "•  Click any topic to jump directly to its section",
                FontSize = 11.5,
                Foreground = MutedBrush,
                Margin = new Thickness(10, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            _ = headerLeft.Children.Add(indexBadge);
            _ = headerLeft.Children.Add(titleText);
            _ = headerLeft.Children.Add(subtitleText);

            Border countPill = new()
            {
                Background = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)),
                BorderBrush = TocCardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 3, 10, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = $"{sections.Count} Topics",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = SecondaryHeadingBrush,
                    FontFamily = DefaultFont,
                },
            };

            Grid.SetColumn(headerLeft, 0);
            Grid.SetColumn(countPill, 1);
            _ = headerGrid.Children.Add(headerLeft);
            _ = headerGrid.Children.Add(countPill);

            Border headerDivider = new()
            {
                Height = 1,
                Background = DividerBrush,
                Margin = new Thickness(0, 12, 0, 12),
            };

            StackPanel itemsPanel = new();

            foreach (TocSection section in sections)
            {
                Border itemRow = new()
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 2, 0, 2),
                    Cursor = Cursors.Hand,
                };

                Grid rowGrid = new();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Border numBadge = new()
                {
                    Width = 26,
                    Height = 22,
                    CornerRadius = new CornerRadius(4),
                    Background = TocNumberBadgeBackground,
                    BorderBrush = TocNumberBadgeBorder,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = section.Number.ToString("D2"),
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = PrimaryHeadingBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontFamily = DefaultFont,
                    },
                };

                TextBlock titleTb = new()
                {
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TextBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                };
                AppendFormattedInlines(
                    titleTb.Inlines,
                    section.Title,
                    anchorMap,
                    headings,
                    document
                );

                TextBlock arrowTb = new()
                {
                    Text = "›",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = MutedBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 4, 0),
                };

                Grid.SetColumn(numBadge, 0);
                Grid.SetColumn(titleTb, 1);
                Grid.SetColumn(arrowTb, 2);
                _ = rowGrid.Children.Add(numBadge);
                _ = rowGrid.Children.Add(titleTb);
                _ = rowGrid.Children.Add(arrowTb);
                itemRow.Child = rowGrid;

                itemRow.MouseEnter += (s, e) =>
                {
                    itemRow.Background = TocHoverBackground;
                    itemRow.BorderBrush = TocHoverBorder;
                    arrowTb.Foreground = PrimaryHeadingBrush;
                };
                itemRow.MouseLeave += (s, e) =>
                {
                    itemRow.Background = Brushes.Transparent;
                    itemRow.BorderBrush = Brushes.Transparent;
                    arrowTb.Foreground = MutedBrush;
                };

                string secAnchor = section.Anchor;
                string secTitle = section.Title;

                void OnRowClick()
                {
                    NavigateToAnchor(secAnchor, secTitle, anchorMap, headings, document);
                }

                itemRow.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    OnRowClick();
                };
                itemRow.MouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    OnRowClick();
                };

                _ = itemsPanel.Children.Add(itemRow);

                if (section.SubItems.Count > 0)
                {
                    Border subCard = new()
                    {
                        Margin = new Thickness(36, 2, 4, 6),
                        Padding = new Thickness(8, 5, 8, 5),
                        Background = TocSubCardBackground,
                        BorderBrush = TocSubCardBorder,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                    };

                    StackPanel subPanel = new();

                    foreach (TocItem subItem in section.SubItems)
                    {
                        Border subRow = new()
                        {
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 3, 6, 3),
                            Margin = new Thickness(0, 1, 0, 1),
                            Cursor = Cursors.Hand,
                        };

                        StackPanel subContent = new() { Orientation = Orientation.Horizontal };

                        TextBlock treeTb = new()
                        {
                            Text = "↳",
                            FontSize = 11,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = PrimaryHeadingBrush,
                            Margin = new Thickness(0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        TextBlock subTitleTb = new()
                        {
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xD4, 0xE2)),
                            VerticalAlignment = VerticalAlignment.Center,
                        };
                        AppendFormattedInlines(
                            subTitleTb.Inlines,
                            subItem.Title,
                            anchorMap,
                            headings,
                            document
                        );

                        _ = subContent.Children.Add(treeTb);
                        _ = subContent.Children.Add(subTitleTb);
                        subRow.Child = subContent;

                        subRow.MouseEnter += (s, e) =>
                        {
                            subRow.Background = TocSubHoverBackground;
                        };
                        subRow.MouseLeave += (s, e) =>
                        {
                            subRow.Background = Brushes.Transparent;
                        };

                        string subAnchor = subItem.Anchor;
                        string subTitle = subItem.Title;

                        void OnSubClick()
                        {
                            NavigateToAnchor(subAnchor, subTitle, anchorMap, headings, document);
                        }

                        subRow.PreviewMouseLeftButtonUp += (s, e) =>
                        {
                            e.Handled = true;
                            OnSubClick();
                        };
                        subRow.MouseLeftButtonUp += (s, e) =>
                        {
                            e.Handled = true;
                            OnSubClick();
                        };

                        _ = subPanel.Children.Add(subRow);
                    }

                    subCard.Child = subPanel;
                    _ = itemsPanel.Children.Add(subCard);
                }
            }

            StackPanel cardContent = new();
            _ = cardContent.Children.Add(headerGrid);
            _ = cardContent.Children.Add(headerDivider);
            _ = cardContent.Children.Add(itemsPanel);
            card.Child = cardContent;

            return new BlockUIContainer(card);
        }

        private static List ParseListBlock(
            string[] lines,
            ref int i,
            Dictionary<string, HeadingAnchor> anchorMap,
            List<HeadingAnchor> headings,
            FlowDocument document,
            int depth = 0
        )
        {
            string firstLine = lines[i];
            string trimmedFirst = firstLine.Trim();
            Match firstNumMatch = OrderedListRegex().Match(trimmedFirst);
            bool isOrdered = firstNumMatch.Success;
            int currentNumber = 1;
            if (isOrdered && int.TryParse(firstNumMatch.Groups[1].Value, out int parsedNum))
            {
                currentNumber = parsedNum;
            }

            List list = new()
            {
                MarkerStyle = TextMarkerStyle.None,
                Margin = new Thickness(
                    depth == 0 ? 10 : 20,
                    depth == 0 ? 3 : 1,
                    0,
                    depth == 0 ? 6 : 2
                ),
                Padding = new Thickness(0),
            };

            int baseIndent = GetIndent(firstLine);

            while (i < lines.Length)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    if (i + 1 < lines.Length)
                    {
                        string nextTrim = lines[i + 1].Trim();
                        int nextIndent = GetIndent(lines[i + 1]);
                        if (
                            !string.IsNullOrWhiteSpace(nextTrim)
                            && (nextIndent > baseIndent || IsListMarker(nextTrim, isOrdered))
                        )
                        {
                            i++;
                            continue;
                        }
                    }
                    break;
                }

                int indent = GetIndent(line);
                if (indent < baseIndent)
                {
                    break;
                }

                Match numMatch = OrderedListRegex().Match(trimmed);
                bool matchesMarker = isOrdered
                    ? numMatch.Success
                    : (
                        trimmed.StartsWith("- ")
                        || trimmed.StartsWith("* ")
                        || trimmed.StartsWith("+ ")
                    );

                if (indent == baseIndent && !matchesMarker)
                {
                    break;
                }

                if (indent > baseIndent)
                {
                    break;
                }

                string itemContent;
                int itemNumber = currentNumber;
                if (isOrdered)
                {
                    if (numMatch.Success && int.TryParse(numMatch.Groups[1].Value, out int pNum))
                    {
                        itemNumber = pNum;
                        currentNumber = pNum + 1;
                    }
                    else
                    {
                        currentNumber++;
                    }
                    itemContent = numMatch.Groups[2].Value;
                }
                else
                {
                    itemContent = trimmed[2..].TrimStart();
                }

                int indentWidth = isOrdered ? (itemNumber >= 10 ? 26 : 22) : (depth == 0 ? 18 : 16);

                ListItem listItem = new()
                {
                    Margin = new Thickness(0, 1, 0, 1),
                    Padding = new Thickness(0),
                };

                Paragraph itemParagraph = new()
                {
                    Margin = new Thickness(indentWidth, 2, 0, 3),
                    TextIndent = -indentWidth,
                    LineHeight = 22,
                    Foreground = TextBrush,
                };

                if (isOrdered)
                {
                    string numPrefix = depth switch
                    {
                        0 => $"{itemNumber}. ",
                        1 => $"{(char)('a' + ((itemNumber - 1) % 26))}. ",
                        _ => $"{itemNumber}) ",
                    };
                    Run numRun = new(numPrefix)
                    {
                        Foreground = depth == 0 ? PrimaryHeadingBrush : SecondaryHeadingBrush,
                        FontWeight = FontWeights.Bold,
                        FontFamily = DefaultFont,
                        FontSize = depth == 0 ? 13 : 12,
                    };
                    itemParagraph.Inlines.Add(numRun);
                }
                else
                {
                    if (itemContent.StartsWith("[ ] "))
                    {
                        Run checkRun = new("☐ ")
                        {
                            Foreground = MutedBrush,
                            FontWeight = FontWeights.Bold,
                            FontSize = 13,
                        };
                        itemParagraph.Inlines.Add(checkRun);
                        itemContent = itemContent[4..];
                    }
                    else if (itemContent.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase))
                    {
                        Run checkRun = new("☑ ")
                        {
                            Foreground = PrimaryHeadingBrush,
                            FontWeight = FontWeights.Bold,
                            FontSize = 13,
                        };
                        itemParagraph.Inlines.Add(checkRun);
                        itemContent = itemContent[4..];
                    }
                    else
                    {
                        string bulletGlyph = depth switch
                        {
                            0 => "• ",
                            1 => "› ",
                            _ => "– ",
                        };
                        Run bulletRun = new(bulletGlyph)
                        {
                            Foreground = depth == 0 ? PrimaryHeadingBrush : SecondaryHeadingBrush,
                            FontWeight = FontWeights.Bold,
                            FontSize = depth == 0 ? 12 : 11,
                        };
                        itemParagraph.Inlines.Add(bulletRun);
                    }
                }

                AppendFormattedInlines(
                    itemParagraph.Inlines,
                    itemContent,
                    anchorMap,
                    headings,
                    document
                );
                listItem.Blocks.Add(itemParagraph);

                i++;

                List<string> childLines = [];
                while (i < lines.Length)
                {
                    string childLine = lines[i];
                    string childTrim = childLine.Trim();

                    if (string.IsNullOrWhiteSpace(childTrim))
                    {
                        if (
                            i + 1 < lines.Length
                            && GetIndent(lines[i + 1]) > baseIndent
                            && !string.IsNullOrWhiteSpace(lines[i + 1].Trim())
                        )
                        {
                            childLines.Add(string.Empty);
                            i++;
                            continue;
                        }
                        break;
                    }

                    if (GetIndent(childLine) > baseIndent)
                    {
                        childLines.Add(childLine);
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (childLines.Count > 0)
                {
                    int cIdx = 0;
                    while (cIdx < childLines.Count)
                    {
                        string cLine = childLines[cIdx];
                        string cTrim = cLine.Trim();

                        if (string.IsNullOrWhiteSpace(cTrim))
                        {
                            cIdx++;
                            continue;
                        }

                        if (IsListLine(cTrim))
                        {
                            List subList = ParseListBlock(
                                [.. childLines],
                                ref cIdx,
                                anchorMap,
                                headings,
                                document,
                                depth + 1
                            );
                            listItem.Blocks.Add(subList);
                        }
                        else
                        {
                            itemParagraph.Inlines.Add(new Run(" "));
                            AppendFormattedInlines(
                                itemParagraph.Inlines,
                                cTrim,
                                anchorMap,
                                headings,
                                document
                            );
                            cIdx++;
                        }
                    }
                }

                list.ListItems.Add(listItem);
            }

            return list;
        }

        private static int GetIndent(string line)
        {
            int spaces = 0;
            foreach (char c in line)
            {
                if (c == ' ')
                {
                    spaces++;
                }
                else if (c == '\t')
                {
                    spaces += 4;
                }
                else
                {
                    break;
                }
            }
            return spaces;
        }

        private static bool IsListMarker(string trimmed, bool isOrdered)
        {
            return isOrdered
                ? OrderedListRegex().IsMatch(trimmed)
                : (
                    trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ")
                );
        }

        private static bool IsListLine(string trimmed)
        {
            return trimmed.StartsWith("- ")
                || trimmed.StartsWith("* ")
                || trimmed.StartsWith("+ ")
                || OrderedListRegex().IsMatch(trimmed);
        }

        private static Paragraph CreateHeading(
            string text,
            int level,
            Dictionary<string, HeadingAnchor> anchorMap,
            List<HeadingAnchor> headings,
            FlowDocument document
        )
        {
            Paragraph p = new();

            switch (level)
            {
                case 1:
                    p.FontSize = 22;
                    p.FontWeight = FontWeights.Bold;
                    p.Foreground = PrimaryHeadingBrush;
                    p.Margin = new Thickness(0, 16, 0, 8);
                    break;
                case 2:
                    p.FontSize = 17;
                    p.FontWeight = FontWeights.SemiBold;
                    p.Foreground = SecondaryHeadingBrush;
                    p.Margin = new Thickness(0, 14, 0, 6);
                    break;
                case 3:
                    p.FontSize = 14.5;
                    p.FontWeight = FontWeights.SemiBold;
                    p.Foreground = TertiaryHeadingBrush;
                    p.Margin = new Thickness(0, 12, 0, 4);
                    break;
                default:
                    p.FontSize = 13.5;
                    p.FontWeight = FontWeights.SemiBold;
                    p.Foreground = TextBrush;
                    p.Margin = new Thickness(0, 8, 0, 3);
                    break;
            }

            Border visualAnchor = new()
            {
                Width = 1,
                Height = 1,
                Opacity = 0,
                Focusable = false,
            };
            InlineUIContainer anchorContainer = new(visualAnchor)
            {
                BaselineAlignment = BaselineAlignment.Top,
            };
            p.Inlines.Add(anchorContainer);

            AppendFormattedInlines(p.Inlines, text, anchorMap, headings, document);
            RegisterHeading(text, p, visualAnchor, anchorMap, headings);
            return p;
        }

        private static void RegisterHeading(
            string text,
            Paragraph paragraph,
            FrameworkElement visualAnchor,
            Dictionary<string, HeadingAnchor> anchorMap,
            List<HeadingAnchor> headings
        )
        {
            HeadingAnchor item = new()
            {
                Text = text,
                Paragraph = paragraph,
                VisualAnchor = visualAnchor,
            };
            headings.Add(item);

            foreach (
                string variant in GenerateSlugVariants(text)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            )
            {
                if (!string.IsNullOrEmpty(variant))
                {
                    anchorMap[variant] = item;
                }
            }
        }

        private static void NavigateToAnchor(
            string? anchor,
            string fallbackText,
            Dictionary<string, HeadingAnchor> anchorMap,
            List<HeadingAnchor> headings,
            FlowDocument document
        )
        {
            if (!string.IsNullOrWhiteSpace(anchor))
            {
                string cleanAnchor = anchor.TrimStart('#').Trim();

                foreach (
                    string variant in GenerateSlugVariants(cleanAnchor)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                )
                {
                    if (anchorMap.TryGetValue(variant, out HeadingAnchor? target))
                    {
                        ScrollToHeading(target, document);
                        return;
                    }
                }

                Match anchorNumMatch = Regex.Match(cleanAnchor, @"^\d+[\-_]+(.*)$");
                if (anchorNumMatch.Success)
                {
                    string anchorRemainder = anchorNumMatch.Groups[1].Value;
                    foreach (
                        string variant in GenerateSlugVariants(anchorRemainder)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                    )
                    {
                        if (anchorMap.TryGetValue(variant, out HeadingAnchor? target))
                        {
                            ScrollToHeading(target, document);
                            return;
                        }
                    }
                }

                string normalizedAnchor = NormalizeSlug(cleanAnchor);
                foreach (HeadingAnchor h in headings)
                {
                    string hSlug = NormalizeSlug(h.Text);
                    if (
                        (
                            !string.IsNullOrEmpty(hSlug)
                            && (
                                hSlug.Contains(normalizedAnchor) || normalizedAnchor.Contains(hSlug)
                            )
                        ) || h.Text.Contains(cleanAnchor, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        ScrollToHeading(h, document);
                        return;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(fallbackText))
            {
                foreach (
                    string variant in GenerateSlugVariants(fallbackText)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                )
                {
                    if (anchorMap.TryGetValue(variant, out HeadingAnchor? targetFallback))
                    {
                        ScrollToHeading(targetFallback, document);
                        return;
                    }
                }

                string fallbackSlug = NormalizeSlug(fallbackText);
                foreach (HeadingAnchor h in headings)
                {
                    string hSlug = NormalizeSlug(h.Text);
                    if (
                        (
                            !string.IsNullOrEmpty(hSlug)
                            && (hSlug.Contains(fallbackSlug) || fallbackSlug.Contains(hSlug))
                        )
                        || h.Text.Contains(fallbackText.Trim(), StringComparison.OrdinalIgnoreCase)
                        || fallbackText.Trim().Contains(h.Text, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        ScrollToHeading(h, document);
                        return;
                    }
                }
            }
        }

        private static void ScrollToHeading(HeadingAnchor anchor, FlowDocument document)
        {
            void ExecuteScroll()
            {
                anchor.Paragraph.BringIntoView();

                _ = document.Dispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() =>
                    {
                        try
                        {
                            ScrollViewer? scrollViewer =
                                FindScrollViewer(document)
                                ?? FindVisualParent<ScrollViewer>(anchor.VisualAnchor);

                            if (
                                scrollViewer != null
                                && VisualTreeHelper.GetParent(anchor.VisualAnchor) != null
                            )
                            {
                                GeneralTransform transform =
                                    anchor.VisualAnchor.TransformToAncestor(scrollViewer);
                                Point offsetPoint = transform.Transform(new Point(0, 0));
                                double targetOffset =
                                    scrollViewer.VerticalOffset + offsetPoint.Y - 14;
                                if (targetOffset < 0)
                                {
                                    targetOffset = 0;
                                }

                                if (targetOffset > scrollViewer.ScrollableHeight)
                                {
                                    targetOffset = scrollViewer.ScrollableHeight;
                                }

                                scrollViewer.ScrollToVerticalOffset(targetOffset);
                            }
                        }
                        catch { }

                        HighlightHeading(anchor.Paragraph);
                    })
                );
            }

            if (document.Dispatcher.CheckAccess())
            {
                ExecuteScroll();
            }
            else
            {
                document.Dispatcher.Invoke(ExecuteScroll);
            }
        }

        private static ScrollViewer? FindScrollViewer(FlowDocument document)
        {
            DependencyObject? current = document.Parent;

            while (current != null)
            {
                if (current is ScrollViewer sv)
                {
                    return sv;
                }

                if (current is FlowDocumentScrollViewer fdsv)
                {
                    ScrollViewer? nested = FindVisualChild<ScrollViewer>(fdsv);
                    if (nested != null)
                    {
                        return nested;
                    }
                }

                if (current is Visual or Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else if (current is FrameworkContentElement fce)
                {
                    current = fce.Parent;
                }
                else
                {
                    break;
                }
            }

            return null;
        }

        private static T? FindVisualParent<T>(DependencyObject? child)
            where T : DependencyObject
        {
            DependencyObject? current = child;
            while (current != null)
            {
                if (current is T typed)
                {
                    return typed;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                {
                    return typed;
                }

                T? nested = FindVisualChild<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        private static void HighlightHeading(Paragraph paragraph)
        {
            Brush original = paragraph.Foreground;
            paragraph.Foreground = Brushes.White;

            DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(450) };
            timer.Tick += (s, e) =>
            {
                paragraph.Foreground = original;
                timer.Stop();
            };
            timer.Start();
        }

        private static string NormalizeSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string clean = Regex.Replace(text, @"[`*_\[\]\(\)]", "");
            clean = Regex.Replace(clean.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return clean;
        }

        private static IEnumerable<string> GenerateSlugVariants(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            string trimmed = text.Trim();
            yield return trimmed.ToLowerInvariant();

            string slug = NormalizeSlug(trimmed);
            if (!string.IsNullOrEmpty(slug))
            {
                yield return slug;
                string collapsed = Regex.Replace(slug, @"-+", "-");
                yield return collapsed;
            }

            string gfm = Regex.Replace(trimmed.ToLowerInvariant(), @"[`*_\[\]\(\):,\.]", "");
            gfm = Regex.Replace(gfm, @"\s+", "-").Trim('-');
            if (!string.IsNullOrEmpty(gfm))
            {
                yield return gfm;
                yield return Regex.Replace(gfm, @"[^a-z0-9\-]", "-").Trim('-');
            }

            string withoutParens = Regex.Replace(trimmed, @"\s*\(.*?\).*", "").Trim();
            if (
                !string.IsNullOrEmpty(withoutParens)
                && !withoutParens.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
            )
            {
                yield return withoutParens.ToLowerInvariant();

                string baseSlug = NormalizeSlug(withoutParens);
                if (!string.IsNullOrEmpty(baseSlug))
                {
                    yield return baseSlug;
                    yield return Regex.Replace(baseSlug, @"-+", "-");
                }

                string baseGfm = Regex.Replace(
                    withoutParens.ToLowerInvariant(),
                    @"[`*_\[\]:,\.]",
                    ""
                );
                baseGfm = Regex.Replace(baseGfm, @"\s+", "-").Trim('-');
                if (!string.IsNullOrEmpty(baseGfm))
                {
                    yield return baseGfm;
                    yield return Regex.Replace(baseGfm, @"[^a-z0-9\-]", "-").Trim('-');
                }
            }

            Match numMatch = Regex.Match(trimmed, @"^(\d+(\.\d+)*)[\.\)\s]+(.*)$");
            if (numMatch.Success)
            {
                string textWithoutNum = numMatch.Groups[3].Value.Trim();
                if (!string.IsNullOrEmpty(textWithoutNum))
                {
                    yield return textWithoutNum.ToLowerInvariant();

                    string slugWithoutNum = NormalizeSlug(textWithoutNum);
                    if (!string.IsNullOrEmpty(slugWithoutNum))
                    {
                        yield return slugWithoutNum;
                        yield return Regex.Replace(slugWithoutNum, @"-+", "-");
                    }

                    string baseWithoutNumAndParens = Regex
                        .Replace(textWithoutNum, @"\s*\(.*?\).*", "")
                        .Trim();
                    if (
                        !string.IsNullOrEmpty(baseWithoutNumAndParens)
                        && !baseWithoutNumAndParens.Equals(
                            textWithoutNum,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        yield return baseWithoutNumAndParens.ToLowerInvariant();

                        string baseSlugNoNum = NormalizeSlug(baseWithoutNumAndParens);
                        if (!string.IsNullOrEmpty(baseSlugNoNum))
                        {
                            yield return baseSlugNoNum;
                            yield return Regex.Replace(baseSlugNoNum, @"-+", "-");
                        }
                    }
                }
            }
        }

        private static BlockUIContainer CreateHorizontalDivider()
        {
            Border divider = new()
            {
                Height = 1,
                Background = DividerBrush,
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            return new BlockUIContainer(divider);
        }

        private static BlockUIContainer CreateCodeBlock(string code)
        {
            Border border = new()
            {
                Background = CodeBlockBackgroundBrush,
                BorderBrush = CodeBlockBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 6, 0, 10),
            };

            TextBlock tb = new()
            {
                Text = code,
                FontFamily = CodeFont,
                FontSize = 12.5,
                Foreground = CodeTextBrush,
                Background = Brushes.Transparent,
                TextWrapping = TextWrapping.NoWrap,
            };

            border.Child = tb;
            return new BlockUIContainer(border);
        }

        private static BlockUIContainer CreateQuoteBlock(string quote)
        {
            Border border = new()
            {
                Background = new SolidColorBrush(Color.FromArgb(0x15, 0x40, 0xC4, 0xFF)),
                BorderBrush = PrimaryHeadingBrush,
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new CornerRadius(0, 4, 4, 0),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 6, 0, 8),
            };

            TextBlock tb = new()
            {
                Text = quote,
                FontStyle = FontStyles.Italic,
                Foreground = MutedBrush,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
            };

            border.Child = tb;
            return new BlockUIContainer(border);
        }

        private static Table? TryParseTable(
            List<string> lines,
            Dictionary<string, HeadingAnchor> anchorMap,
            List<HeadingAnchor> headings,
            FlowDocument document
        )
        {
            if (lines.Count < 2)
            {
                return null;
            }

            string[] headerCols = SplitTableRow(lines[0]);
            if (headerCols.Length == 0)
            {
                return null;
            }

            if (!lines[1].Contains("---"))
            {
                return null;
            }

            Table table = new()
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 8, 0, 12),
                BorderBrush = TableBorderBrush,
                BorderThickness = new Thickness(1),
            };

            foreach (string _ in headerCols)
            {
                table.Columns.Add(new TableColumn());
            }

            TableRowGroup rowGroup = new();
            table.RowGroups.Add(rowGroup);

            TableRow headerRow = new() { Background = TableHeaderBackgroundBrush };
            foreach (string hText in headerCols)
            {
                TableCell cell = new()
                {
                    Padding = new Thickness(10, 6, 10, 6),
                    BorderBrush = TableBorderBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                };
                Paragraph p = new()
                {
                    FontWeight = FontWeights.SemiBold,
                    Foreground = PrimaryHeadingBrush,
                };
                AppendFormattedInlines(p.Inlines, hText.Trim(), anchorMap, headings, document);
                cell.Blocks.Add(p);
                headerRow.Cells.Add(cell);
            }
            rowGroup.Rows.Add(headerRow);

            for (int i = 2; i < lines.Count; i++)
            {
                string[] dataCols = SplitTableRow(lines[i]);
                TableRow row = new()
                {
                    Background =
                        (i % 2 == 0)
                            ? new SolidColorBrush(Color.FromArgb(0x06, 0xFF, 0xFF, 0xFF))
                            : Brushes.Transparent,
                };

                for (int c = 0; c < headerCols.Length; c++)
                {
                    string colVal = c < dataCols.Length ? dataCols[c] : string.Empty;
                    TableCell cell = new()
                    {
                        Padding = new Thickness(10, 5, 10, 5),
                        BorderBrush = TableBorderBrush,
                        BorderThickness = new Thickness(0, 0, 1, 1),
                    };
                    Paragraph p = new() { Foreground = TextBrush };
                    AppendFormattedInlines(p.Inlines, colVal.Trim(), anchorMap, headings, document);
                    cell.Blocks.Add(p);
                    row.Cells.Add(cell);
                }
                rowGroup.Rows.Add(row);
            }

            return table;
        }

        private static string[] SplitTableRow(string row)
        {
            string trimmed = row.Trim();
            if (trimmed.StartsWith('|'))
            {
                trimmed = trimmed[1..];
            }

            if (trimmed.EndsWith('|'))
            {
                trimmed = trimmed[..^1];
            }

            return trimmed.Split('|');
        }

        public static void AppendFormattedInlines(InlineCollection inlines, string text)
        {
            AppendFormattedInlines(inlines, text, null, null, null);
        }

        internal static void AppendFormattedInlines(
            InlineCollection inlines,
            string text,
            Dictionary<string, HeadingAnchor>? anchorMap,
            List<HeadingAnchor>? headings,
            FlowDocument? document
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            int lastIndex = 0;
            MatchCollection matches = InlineMarkdownRegex().Matches(text);

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    inlines.Add(new Run(text[lastIndex..match.Index]));
                }

                if (match.Groups["inlineCode"].Success)
                {
                    string rawCode = match.Value.Trim('`');
                    Run codeRun = new(rawCode)
                    {
                        FontFamily = CodeFont,
                        FontSize = 12,
                        Background = CodeBackgroundBrush,
                        Foreground = CodeTextBrush,
                    };
                    inlines.Add(codeRun);
                }
                else if (match.Groups["bold"].Success)
                {
                    string boldText = match.Value[2..^2];
                    Bold b = new();
                    AppendFormattedInlines(b.Inlines, boldText, anchorMap, headings, document);
                    inlines.Add(b);
                }
                else if (match.Groups["italic"].Success)
                {
                    string italicText = match.Value[1..^1];
                    Italic it = new();
                    AppendFormattedInlines(it.Inlines, italicText, anchorMap, headings, document);
                    inlines.Add(it);
                }
                else if (match.Groups["link"].Success)
                {
                    string linkText = match.Groups["linkText"].Value;
                    string linkUrl = match.Groups["linkUrl"].Value;

                    Hyperlink hyperlink = new()
                    {
                        Foreground = PrimaryHeadingBrush,
                        TextDecorations = null,
                        Cursor = Cursors.Hand,
                    };

                    hyperlink.MouseEnter += (s, e) =>
                        hyperlink.TextDecorations = TextDecorations.Underline;
                    hyperlink.MouseLeave += (s, e) => hyperlink.TextDecorations = null;

                    hyperlink.Click += (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(linkUrl))
                        {
                            return;
                        }

                        if (linkUrl.StartsWith('#'))
                        {
                            if (anchorMap != null && headings != null && document != null)
                            {
                                NavigateToAnchor(linkUrl, linkText, anchorMap, headings, document);
                            }
                        }
                        else if (
                            Uri.TryCreate(linkUrl, UriKind.Absolute, out Uri? uri)
                            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                        )
                        {
                            try
                            {
                                _ = Process.Start(
                                    new ProcessStartInfo(linkUrl) { UseShellExecute = true }
                                );
                            }
                            catch { }
                        }
                    };

                    AppendFormattedInlines(
                        hyperlink.Inlines,
                        linkText,
                        anchorMap,
                        headings,
                        document
                    );
                    inlines.Add(hyperlink);
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                inlines.Add(new Run(text[lastIndex..]));
            }
        }
    }
}
