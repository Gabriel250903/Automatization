using Automatization.Services;
using Automatization.Types;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;

namespace Automatization.Converters
{
    public class MarkdownToFlowDocumentConverter : IValueConverter
    {
        private static readonly Regex ImageRegex = new(@"!\[.*?\]\((?<url1>.*?)\)|<img[^>]*src=[""'](?<url2>.*?)[""'][^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FlowDocument doc = new()
            {
                PagePadding = new Thickness(0)
            };

            string markdown;
            int issueId;

            if (value is RoadmapItem roadmapItem)
            {
                markdown = roadmapItem.Body;
                issueId = roadmapItem.Number;
            }
            else if (value is IssueComment issueComment)
            {
                markdown = issueComment.Body;
                issueId = issueComment.ParentIssueNumber;
            }
            else
            {
                return doc;
            }

            if (string.IsNullOrEmpty(markdown))
            {
                return doc;
            }

            MatchCollection matches = ImageRegex.Matches(markdown);

            int lastIndex = 0;
            Paragraph paragraph = new();

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    string text = markdown[lastIndex..match.Index];
                    if (!string.IsNullOrEmpty(text))
                    {
                        paragraph.Inlines.Add(new Run(text));
                    }
                }

                string imageUrl = match.Groups["url1"].Success ? match.Groups["url1"].Value : match.Groups["url2"].Value;

                string? localPath = issueId > 0 ? ImageCacheService.GetIssueImagePath(imageUrl, issueId) : ImageCacheService.GetCachedImagePath(imageUrl);
                if (!string.IsNullOrEmpty(localPath))
                {
                    try
                    {
                        BitmapImage bitmap = new();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(localPath);
                        bitmap.EndInit();

                        Image img = new()
                        {
                            Source = bitmap,
                            MaxWidth = 500,
                            Stretch = Stretch.Uniform,
                            Margin = new Thickness(0, 10, 0, 10),
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        if (paragraph.Inlines.Count > 0)
                        {
                            doc.Blocks.Add(paragraph);
                            paragraph = new Paragraph();
                        }

                        doc.Blocks.Add(new BlockUIContainer(img));
                    }
                    catch
                    {
                        paragraph.Inlines.Add(new Run($" [Image: {imageUrl}] "));
                    }
                }
                else
                {
                    paragraph.Inlines.Add(new Run($" [Image: {imageUrl}] "));
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < markdown.Length)
            {
                string text = markdown[lastIndex..];
                if (!string.IsNullOrEmpty(text))
                {
                    paragraph.Inlines.Add(new Run(text));
                }
            }

            if (paragraph.Inlines.Count > 0)
            {
                doc.Blocks.Add(paragraph);
            }

            return doc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}