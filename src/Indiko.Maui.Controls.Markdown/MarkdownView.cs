using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics.Text;
using SkiaSharp;
using Svg.Model;
using Svg.Skia;
using Image = Microsoft.Maui.Controls.Image;

namespace Indiko.Maui.Controls.Markdown;

public partial class MarkdownView : ContentView
{

    private static readonly Regex KaTeXBlockRegex = new Regex(@"\$\$(.*?)\$\$", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex KaTeXInlineRegex = new Regex(@"\$(.*?)\$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);

    private readonly Thickness _defaultListIndent = new(10, 0, 10, 0);
    private Dictionary<string, ImageSource> _imageCache = [];

    public delegate void HyperLinkClicked(object sender, LinkEventArgs e);
    public event HyperLinkClicked OnHyperLinkClicked;

    public delegate void EmailClickedEventHandler(object sender, EmailEventArgs e);
    public event EmailClickedEventHandler OnEmailClicked;


    public static readonly BindableProperty MarkdownTextProperty =
        BindableProperty.Create(nameof(MarkdownText), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    public static readonly BindableProperty LineBreakModeTextProperty =
       BindableProperty.Create(nameof(LineBreakModeText), typeof(LineBreakMode), typeof(MarkdownView), LineBreakMode.WordWrap, propertyChanged: OnMarkdownTextChanged);

    public LineBreakMode LineBreakModeText
    {
        get => (LineBreakMode)GetValue(LineBreakModeTextProperty);
        set => SetValue(LineBreakModeTextProperty, value);
    }

    public static readonly BindableProperty LineBreakModeHeaderProperty =
       BindableProperty.Create(nameof(LineBreakModeHeader), typeof(LineBreakMode), typeof(MarkdownView), LineBreakMode.TailTruncation, propertyChanged: OnMarkdownTextChanged);

    public LineBreakMode LineBreakModeHeader
    {
        get => (LineBreakMode)GetValue(LineBreakModeHeaderProperty);
        set => SetValue(LineBreakModeHeaderProperty, value);
    }

    public static readonly BindableProperty H1ColorProperty =
        BindableProperty.Create(nameof(H1Color), typeof(Color), typeof(MarkdownView), Colors.Black, propertyChanged: OnMarkdownTextChanged);

    public Color H1Color
    {
        get => (Color)GetValue(H1ColorProperty);
        set => SetValue(H1ColorProperty, value);
    }

    public static readonly BindableProperty H1FontSizeProperty =
      BindableProperty.Create(nameof(H1FontSize), typeof(double), typeof(MarkdownView), defaultValue: 24d, propertyChanged: OnMarkdownTextChanged);

    [TypeConverter(typeof(FontSizeConverter))]
    public double H1FontSize
    {
        get => (double)GetValue(H1FontSizeProperty);
        set => SetValue(H1FontSizeProperty, value);
    }

    public static readonly BindableProperty H2ColorProperty =
        BindableProperty.Create(nameof(H2Color), typeof(Color), typeof(MarkdownView), Colors.DarkGray, propertyChanged: OnMarkdownTextChanged);

    public Color H2Color
    {
        get => (Color)GetValue(H2ColorProperty);
        set => SetValue(H2ColorProperty, value);
    }

    public static readonly BindableProperty H2FontSizeProperty =
     BindableProperty.Create(nameof(H2FontSize), typeof(double), typeof(MarkdownView), defaultValue: 20d, propertyChanged: OnMarkdownTextChanged);

    [TypeConverter(typeof(FontSizeConverter))]
    public double H2FontSize
    {
        get => (double)GetValue(H2FontSizeProperty);
        set => SetValue(H2FontSizeProperty, value);
    }

    // H3Color property
    public static readonly BindableProperty H3ColorProperty =
        BindableProperty.Create(nameof(H3Color), typeof(Color), typeof(MarkdownView), Colors.Gray, propertyChanged: OnMarkdownTextChanged);

    public Color H3Color
    {
        get => (Color)GetValue(H3ColorProperty);
        set => SetValue(H3ColorProperty, value);
    }

    public static readonly BindableProperty H3FontSizeProperty =
     BindableProperty.Create(nameof(H3FontSize), typeof(double), typeof(MarkdownView), defaultValue: 18d, propertyChanged: OnMarkdownTextChanged);

    [TypeConverter(typeof(FontSizeConverter))]
    public double H3FontSize
    {
        get => (double)GetValue(H3FontSizeProperty);
        set => SetValue(H3FontSizeProperty, value);
    }

    /* **** Table Header Style ***/

    public static readonly BindableProperty TableHeaderFontSizeProperty =
        BindableProperty.Create(nameof(TableHeaderFontSize), typeof(double), typeof(MarkdownView), defaultValue: 14d, propertyChanged: OnMarkdownTextChanged);

    [TypeConverter(typeof(FontSizeConverter))]
    public double TableHeaderFontSize
    {
        get => (double)GetValue(TableHeaderFontSizeProperty);
        set => SetValue(TableHeaderFontSizeProperty, value);
    }

    public static readonly BindableProperty TableHeaderTextColorProperty =
      BindableProperty.Create(nameof(TableHeaderTextColor), typeof(Color), typeof(MarkdownView), Colors.Black, propertyChanged: OnMarkdownTextChanged);

    public Color TableHeaderTextColor
    {
        get => (Color)GetValue(TableHeaderTextColorProperty);
        set => SetValue(TableHeaderTextColorProperty, value);
    }

    public static readonly BindableProperty TableHeaderBackgroundColorProperty =
     BindableProperty.Create(nameof(TableHeaderBackgroundColor), typeof(Color), typeof(MarkdownView), Colors.LightGrey, propertyChanged: OnMarkdownTextChanged);

    public Color TableHeaderBackgroundColor
    {
        get => (Color)GetValue(TableHeaderBackgroundColorProperty);
        set => SetValue(TableHeaderBackgroundColorProperty, value);
    }

    public static readonly BindableProperty TableHeaderFontFaceProperty =
        BindableProperty.Create(nameof(TableHeaderFontFace), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

    public string TableHeaderFontFace
    {
        get => (string)GetValue(TableHeaderFontFaceProperty);
        set => SetValue(TableHeaderFontFaceProperty, value);
    }

    /***** Table Row Styling **/

    public static readonly BindableProperty TableRowFontFaceProperty =
       BindableProperty.Create(nameof(TableRowFontFace), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

    public string TableRowFontFace
    {
        get => (string)GetValue(TableRowFontFaceProperty);
        set => SetValue(TableRowFontFaceProperty, value);
    }

    public static readonly BindableProperty TableRowTextColorProperty =
     BindableProperty.Create(nameof(TableRowTextColor), typeof(Color), typeof(MarkdownView), Colors.Black, propertyChanged: OnMarkdownTextChanged);

    public Color TableRowTextColor
    {
        get => (Color)GetValue(TableRowTextColorProperty);
        set => SetValue(TableRowTextColorProperty, value);
    }

    public static readonly BindableProperty TableRowFontSizeProperty =
       BindableProperty.Create(nameof(TableRowFontSize), typeof(double), typeof(MarkdownView), defaultValue: 12d, propertyChanged: OnMarkdownTextChanged);

    [TypeConverter(typeof(FontSizeConverter))]
    public double TableRowFontSize
    {
        get => (double)GetValue(TableRowFontSizeProperty);
        set => SetValue(TableRowFontSizeProperty, value);
    }


    /* ****** Text Styling ******** */

    public static readonly BindableProperty TextColorProperty =
       BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(MarkdownView), Colors.Black, propertyChanged: OnMarkdownTextChanged);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty TextFontSizeProperty =
       BindableProperty.Create(nameof(TextFontSize), typeof(double), typeof(MarkdownView), defaultValue: 12d, propertyChanged: OnMarkdownTextChanged);

    [TypeConverter(typeof(FontSizeConverter))]
    public double TextFontSize
    {
        get => (double)GetValue(TextFontSizeProperty);
        set => SetValue(TextFontSizeProperty, value);
    }

    public static readonly BindableProperty TextFontFaceProperty =
      BindableProperty.Create(nameof(TextFontFace), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

    public string TextFontFace
    {
        get => (string)GetValue(TextFontFaceProperty);
        set => SetValue(TextFontFaceProperty, value);
    }

    /* ****** Line Block Styling ******** */

    public static readonly BindableProperty LineColorProperty =
    BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(MarkdownView), Colors.LightGray, propertyChanged: OnMarkdownTextChanged);

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    /* ****** Code Block Styling ******** */
    public static readonly BindableProperty CodeBlockBackgroundColorProperty =
       BindableProperty.Create(nameof(CodeBlockBackgroundColor), typeof(Color), typeof(MarkdownView), Colors.LightGray, propertyChanged: OnMarkdownTextChanged);

    public Color CodeBlockBackgroundColor
    {
        get => (Color)GetValue(CodeBlockBackgroundColorProperty);
        set => SetValue(CodeBlockBackgroundColorProperty, value);
    }

    public static readonly BindableProperty CodeBlockBorderColorProperty =
       BindableProperty.Create(nameof(CodeBlockBorderColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

    public Color CodeBlockBorderColor
    {
        get => (Color)GetValue(CodeBlockBorderColorProperty);
        set => SetValue(CodeBlockBorderColorProperty, value);
    }

    public static readonly BindableProperty CodeBlockTextColorProperty =
       BindableProperty.Create(nameof(CodeBlockTextColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

    public Color CodeBlockTextColor
    {
        get => (Color)GetValue(CodeBlockTextColorProperty);
        set => SetValue(CodeBlockTextColorProperty, value);
    }

    public static readonly BindableProperty CodeBlockFontSizeProperty =
       BindableProperty.Create(nameof(CodeBlockFontSize), typeof(double), typeof(MarkdownView), defaultValue: 12d, propertyChanged: OnMarkdownTextChanged);

    [TypeConverter(typeof(FontSizeConverter))]
    public double CodeBlockFontSize
    {
        get => (double)GetValue(CodeBlockFontSizeProperty);
        set => SetValue(CodeBlockFontSizeProperty, value);
    }

    public static readonly BindableProperty CodeBlockFontFaceProperty =
      BindableProperty.Create(nameof(CodeBlockFontFace), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

    public string CodeBlockFontFace
    {
        get => (string)GetValue(CodeBlockFontFaceProperty);
        set => SetValue(CodeBlockFontFaceProperty, value);
    }

    /* ****** BlockQuote Block Styling ******** */

    public static readonly BindableProperty BlockQuoteBackgroundColorProperty =
     BindableProperty.Create(nameof(BlockQuoteBackgroundColor), typeof(Color), typeof(MarkdownView), Colors.LightGray, propertyChanged: OnMarkdownTextChanged);

    public Color BlockQuoteBackgroundColor
    {
        get => (Color)GetValue(BlockQuoteBackgroundColorProperty);
        set => SetValue(BlockQuoteBackgroundColorProperty, value);
    }

    public static readonly BindableProperty BlockQuoteBorderColorProperty =
      BindableProperty.Create(nameof(BlockQuoteBorderColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

    public Color BlockQuoteBorderColor
    {
        get => (Color)GetValue(BlockQuoteBorderColorProperty);
        set => SetValue(BlockQuoteBorderColorProperty, value);
    }

    public static readonly BindableProperty BlockQuoteTextColorProperty =
      BindableProperty.Create(nameof(BlockQuoteTextColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

    public Color BlockQuoteTextColor
    {
        get => (Color)GetValue(BlockQuoteTextColorProperty);
        set => SetValue(BlockQuoteTextColorProperty, value);
    }

    public static readonly BindableProperty BlockQuoteFontFaceProperty =
     BindableProperty.Create(nameof(BlockQuoteFontFace), typeof(string), typeof(MarkdownView), defaultValue: "Consolas", propertyChanged: OnMarkdownTextChanged);

    public string BlockQuoteFontFace
    {
        get => (string)GetValue(BlockQuoteFontFaceProperty);
        set => SetValue(BlockQuoteFontFaceProperty, value);
    }

    /* ****** Hyplerlink Styling ******** */

    public static readonly BindableProperty HyperlinkColorProperty =
     BindableProperty.Create(nameof(HyperlinkColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

    public Color HyperlinkColor
    {
        get => (Color)GetValue(HyperlinkColorProperty);
        set => SetValue(HyperlinkColorProperty, value);
    }

    public static readonly BindableProperty LinkCommandProperty =
    BindableProperty.Create(nameof(LinkCommand), typeof(ICommand), typeof(MarkdownView));

    public ICommand LinkCommand
    {
        get => (ICommand)GetValue(LinkCommandProperty);
        set => SetValue(LinkCommandProperty, value);
    }

    public static readonly BindableProperty LinkCommandParameterProperty =
        BindableProperty.Create(nameof(LinkCommandParameter), typeof(object), typeof(MarkdownView));

    public object LinkCommandParameter
    {
        get => GetValue(LinkCommandParameterProperty);
        set => SetValue(LinkCommandParameterProperty, value);
    }

    /* **************** E-Mail Links ************************/

    public static readonly BindableProperty EMailCommandProperty =
   BindableProperty.Create(nameof(EMailCommand), typeof(ICommand), typeof(MarkdownView));

    public ICommand EMailCommand
    {
        get => (ICommand)GetValue(EMailCommandProperty);
        set => SetValue(EMailCommandProperty, value);
    }

    public static readonly BindableProperty EMailCommandParameterProperty =
        BindableProperty.Create(nameof(EMailCommandParameter), typeof(object), typeof(MarkdownView));

    public object EMailCommandParameter
    {
        get => GetValue(EMailCommandParameterProperty);
        set => SetValue(EMailCommandParameterProperty, value);
    }

    /* **************** Image Styling ***********************/

    public static readonly BindableProperty ImageAspectProperty =
       BindableProperty.Create(nameof(ImageAspect), typeof(Aspect), typeof(MarkdownView), defaultValue: Aspect.AspectFit, propertyChanged: OnMarkdownTextChanged);

    public Aspect ImageAspect
    {
        get => (Aspect)GetValue(ImageAspectProperty);
        set => SetValue(ImageAspectProperty, value);
    }


    public static readonly BindableProperty ListIndentProperty =
    BindableProperty.Create(nameof(ListIndent), typeof(Thickness), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

    public Thickness ListIndent
    {
        get => (Thickness)GetValue(ListIndentProperty);
        set => SetValue(ListIndentProperty, value);
    }

    public static readonly BindableProperty ParagraphSpacingProperty = BindableProperty.Create(nameof(ParagraphSpacing),
        typeof(double), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged, defaultValue: 3.0);

    public double ParagraphSpacing
    {
        get => (double)GetValue(ParagraphSpacingProperty);
        set => SetValue(ParagraphSpacingProperty, value);
    }

    public static readonly BindableProperty LineHeightMultiplierProperty =
        BindableProperty.Create(nameof(LineHeightMultiplier), typeof(double), typeof(MarkdownView),
            propertyChanged: OnMarkdownTextChanged, defaultValue: 1.0);

    public double LineHeightMultiplier
    {
        get => (double)GetValue(LineHeightMultiplierProperty);
        set => SetValue(LineHeightMultiplierProperty, value);
    }

    private static void OnMarkdownTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (MarkdownView)bindable;
        control.RenderMarkdown();
    }
    private void RenderMarkdown()
    {
        if (string.IsNullOrWhiteSpace(MarkdownText))
            return;

        Content = null;

        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(0, 0, 0, 0),
            RowSpacing = ParagraphSpacing,
            ColumnSpacing = 0,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        var lines = Regex.Split(MarkdownText, @"\r\n?|\n", RegexOptions.Compiled);
        lines = lines.Where(line => !string.IsNullOrEmpty(line)).ToArray();

        int gridRow = 0;
        bool isUnorderedListActive = false;
        bool isOrderedListActive = false;
        bool currentLineIsBlockQuote = true;
        bool isExitingList = false;
        Label activeCodeBlockLabel = null;
        int startCodeBlock = 0; // Gets the index of the initial code block

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            bool lineBeforeWasBlockQuote = currentLineIsBlockQuote;
            currentLineIsBlockQuote = false;
            if (activeCodeBlockLabel == null)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            if (activeCodeBlockLabel != null)
            {
                //Creates an indented code line based on the start code block
                var indentedCodeLine = CreateIndentedCodeLine(lines[i], lines[startCodeBlock]);
                HandleActiveCodeBlock(indentedCodeLine, ref activeCodeBlockLabel, ref gridRow);
            }
            else if (IsHeadline(line, out int headlineLevel))
            {
                var headlineText = line[(headlineLevel + 1)..].Trim();
                Color textColor = headlineLevel == 1 ? H1Color :
                                  headlineLevel == 2 ? H2Color :
                                  headlineLevel == 3 ? H3Color : TextColor; // Default for h4-h6
                double fontSize = headlineLevel == 1 ? H1FontSize :
                                  headlineLevel == 2 ? H2FontSize :
                                  headlineLevel == 3 ? H3FontSize : TextFontSize; // Default for h4-h6

                var label = new Label
                {
                    LineHeight = LineHeightMultiplier,
                    Text = headlineText,
                    TextColor = textColor,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = fontSize,
                    FontFamily = TextFontFace,
                    LineBreakMode = LineBreakModeHeader,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };

                grid.Children.Add(label);
                Grid.SetColumnSpan(label, 2);
                Grid.SetRow(label, gridRow++);
                isExitingList = false;
            }
            else if (IsImage(line))
            {
                var image = CreateImageBlock(line);

                if (image == null)
                {
                    continue;
                }

                grid.Children.Add(image);
                Grid.SetColumnSpan(image, 2);
                Grid.SetRow(image, gridRow++);
                isExitingList = false;
            }
            else if (IsBlockQuote(line))
            {
                HandleBlockQuote(line, lineBeforeWasBlockQuote, grid, out currentLineIsBlockQuote, ref gridRow);
                isExitingList = false;
            }
            else if (IsTaskList(line, out bool isChecked))
            {
                AddTaskListItemToGrid(line[6..], isChecked, grid, gridRow);
                gridRow++;
                isExitingList = true;
                continue;
            }
            else if (IsUnorderedList(line))
            {
                if (!isUnorderedListActive)
                {
                    isUnorderedListActive = true;
                }

                AddBulletPointToGrid(grid, gridRow);
                AddListItemTextToGrid(line[2..], grid, gridRow);

                gridRow++;
                isExitingList = true;
            }
            else if (IsOrderedList(line, out int listItemIndex))
            {
                if (!isOrderedListActive)
                {
                    isOrderedListActive = true;
                }

                AddOrderedListItemToGrid(listItemIndex, grid, gridRow);
                AddListItemTextToGrid(line[(listItemIndex.ToString().Length + 2)..], grid, gridRow);

                gridRow++;
                isExitingList = true;
            }
            else if (IsCodeBlock(line, out bool isSingleLineCodeBlock))
            {
                startCodeBlock = i; // Sets the initial code block index
                HandleSingleLineOrStartOfCodeBlock(line, grid, ref gridRow, isSingleLineCodeBlock, ref activeCodeBlockLabel);
                isExitingList = false;
            }
            else if (IsHorizontalRule(line))
            {
                var horizontalLine = new Rectangle
                {
                    MinimumHeightRequest = 2,
                    Background = LineColor,
                    BackgroundColor = LineColor,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center
                };

                grid.Children.Add(horizontalLine);
                Grid.SetRow(horizontalLine, gridRow);
                Grid.SetColumnSpan(horizontalLine, 2);
                gridRow++;
                isExitingList = false;
            }
            else if (IsKaTeXBlock(line))
            {
                var match = KaTeXBlockRegex.Match(line);
                var latexFormula = match.Groups[1].Value;

                var latexView = new LatexView
                {
                    Text = latexFormula,
                    FontSize = (float)(TextFontSize * 4),
                    TextColor = TextColor,
                    HighlightColor = Colors.Transparent,
                    ErrorColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                };

                grid.Children.Add(latexView);
                Grid.SetRow(latexView, gridRow++);
                Grid.SetColumnSpan(latexView, 2);
                continue;
            }
            else if (IsKaTeXInline(line))
            {
                var katexGrid = CreateInlineKatexBlock(line, TextColor);
                grid.Children.Add(katexGrid);
                Grid.SetRow(katexGrid, gridRow++);
                Grid.SetColumnSpan(katexGrid, 2);
                continue;
            }
            else if (IsTable(lines, i, out int tableEndIndex)) // Detect table
            {
                var table = CreateTable(lines, i, tableEndIndex);
                grid.Children.Add(table);
                Grid.SetColumnSpan(table, 2);
                Grid.SetRow(table, gridRow++);
                i = tableEndIndex; // Skip processed lines
                isExitingList = false;
            }
            else // Regular text
            {
                if (isUnorderedListActive || isOrderedListActive || isExitingList)
                {
                    isUnorderedListActive = false;
                    isOrderedListActive = false;
                    isExitingList = false;
                    gridRow++;
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                var formattedString = CreateFormattedString(line, TextColor);
                var label = new Label
                {
                    LineHeight = LineHeightMultiplier,
                    FormattedText = formattedString,
                    LineBreakMode = LineBreakModeText,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Start
                };

                grid.Children.Add(label);
                Grid.SetRow(label, gridRow);
                Grid.SetColumn(label, 0);
                Grid.SetColumnSpan(label, 2);

                gridRow++;

                // never finish with empty superfluous empty line
                if (i != lines.Length - 1) AddEmptyRow(grid, ref gridRow);
            }
        }

        Content = grid;
    }

    private void HandleBlockQuote(string line, bool lineBeforeWasBlockQuote, Grid grid, out bool currentLineIsBlockQuote, ref int gridRow)
    {
        var box = new Border
        {
            Margin = new Thickness(0),
            BackgroundColor = BlockQuoteBorderColor,
            Stroke = new SolidColorBrush(BlockQuoteBorderColor),
            StrokeShape = new RoundRectangle { CornerRadius = 0 },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var blockQuotelabel = new Label
        {
            LineHeight = LineHeightMultiplier,
            FormattedText = CreateFormattedString(line[1..].Trim(), BlockQuoteTextColor),
            LineBreakMode = LineBreakModeText,
            FontFamily = BlockQuoteFontFace,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(5)
        };

        var blockQuoteGrid = new Grid
        {
            RowSpacing = 0,
            ColumnSpacing = 0,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 5 },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        blockQuoteGrid.Children.Add(box);
        Grid.SetRow(box, 0);
        Grid.SetColumn(box, 0);

        blockQuoteGrid.Children.Add(blockQuotelabel);
        Grid.SetRow(blockQuotelabel, 0);
        Grid.SetColumn(blockQuotelabel, 1);

        var blockquote = new Border
        {
            Padding = new Thickness(0),
            Stroke = new SolidColorBrush(BlockQuoteBorderColor),
            StrokeShape = new RoundRectangle { CornerRadius = 0 },
            BackgroundColor = BlockQuoteBackgroundColor,
            Content = blockQuoteGrid
        };

        if (lineBeforeWasBlockQuote)
        {
            blockquote.Margin = new Thickness(0, -grid.RowSpacing, 0, 0);
        }

        currentLineIsBlockQuote = true;

        grid.Children.Add(blockquote);
        Grid.SetColumnSpan(blockquote, 2);
        Grid.SetRow(blockquote, gridRow++);
    }

    private void HandleSingleLineOrStartOfCodeBlock(string line, Grid grid, ref int gridRow, bool isSingleLineCodeBlock, ref Label activeCodeBlockLabel)
    {
        Border codeBlock = CreateCodeBlock(line, out Label contentLabel);
        grid.Children.Add(codeBlock);
        Grid.SetRow(codeBlock, gridRow);
        Grid.SetColumnSpan(codeBlock, 2);
        if (isSingleLineCodeBlock)
            gridRow++;
        else
            activeCodeBlockLabel = contentLabel;
    }

    private static void HandleActiveCodeBlock(string line, ref Label activeCodeBlockLabel, ref int gridRow)
    {
        if (IsCodeBlock(line, out bool _))
        {
            activeCodeBlockLabel = null;
            gridRow++;
        }
        else
        {
            activeCodeBlockLabel.Text += (string.IsNullOrWhiteSpace(activeCodeBlockLabel.Text) ? "" : "\n") + line;
        }
    }

    private void AddEmptyRow(Grid grid, ref int gridRow)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
        gridRow++;
    }

    private static bool IsKaTeXBlock(string line)
    {
        string trimmedLine = line.TrimStart();
        return KaTeXBlockRegex.IsMatch(trimmedLine);
    }

    private static bool IsKaTeXInline(string line)
    {
        string trimmedLine = line.TrimStart();
        return KaTeXInlineRegex.IsMatch(trimmedLine);
    }

    private static bool IsHorizontalRule(string line)
    {
        string compactLine = line.Replace(" ", string.Empty);

        return compactLine.Length >= 3 &&
               (compactLine.All(c => c == '-') || compactLine.All(c => c == '*') || compactLine.All(c => c == '_'));
    }

    private static bool IsHeadline(string line, out int level)
    {
        level = 0;
        line = line.TrimStart();
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }
        bool isHeadline = level > 0 && level < 7 && line.Length > level && line[level] == ' ';

        if (!isHeadline)
        {
            level = 0;
        }
        return isHeadline;
    }

    private static bool IsUnorderedList(string line)
    {
        string trimmedLine = line.TrimStart();

        return trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ") || trimmedLine.StartsWith("+ ");
    }

    private static bool IsBlockQuote(string line)
    {
        string trimmedLine = line.TrimStart();

        return trimmedLine.StartsWith('>');
    }

    private static bool IsImage(string line)
    {
        string trimmedLine = line.TrimStart();

        return trimmedLine.StartsWith("![");
    }

    private static bool IsCodeBlock(string line, out bool isSingleLineCodeBlock)
    {
        string trimmedLine = line.Trim();
        isSingleLineCodeBlock = trimmedLine.Count(x => x == '`') >= 6 && trimmedLine.EndsWith("```", StringComparison.Ordinal);

        return trimmedLine.StartsWith("```", StringComparison.Ordinal);
    }

    private static bool IsOrderedList(string line, out int listItemIndex)
    {
        listItemIndex = 0;
        string trimmedLine = line.TrimStart();

        var match = Regex.Match(trimmedLine, @"^(\d+)\. ");
        if (match.Success)
        {
            listItemIndex = int.Parse(match.Groups[1].Value);
            return true;
        }

        return false;
    }

    private static bool IsTaskList(string line, out bool isChecked)
    {
        isChecked = line.StartsWith("- [x]", StringComparison.OrdinalIgnoreCase);
        return line.StartsWith("- [ ]") || isChecked;
    }

    private static bool IsTable(string[] lines, int currentIndex, out int tableEndIndex)
    {
        tableEndIndex = currentIndex;
        if (!lines[currentIndex].Contains('|'))
            return false;

        for (int i = currentIndex + 1; i < lines.Length; i++)
        {
            if (!lines[i].Contains('|'))
            {
                tableEndIndex = i - 1;
                return true;
            }
        }

        tableEndIndex = lines.Length - 1;
        return true;
    }

    private static int CountLeadingSpaces(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return 0; // Empty or null string has no leading spaces
        }

        int spaceCount = 0;
        foreach (char c in input)
        {
            if (char.IsWhiteSpace(c) || c == '\u3000') // Includes Unicode space
            {
                spaceCount++;
            }
            else
            {
                break; // Stop counting when a non-space char is found
            }
        }
        return spaceCount;
    }

    private static string CreateIndentedCodeLine(string line, string lineStart)
    {
        try
        {
            int firstLineIndent = CountLeadingSpaces(line);
            int secondLineIndent = CountLeadingSpaces(lineStart);
            
            if (firstLineIndent >= secondLineIndent) 
            {
                // Remove the extra indentation to align the code properly
                var indentedCodeLine = line.Substring(secondLineIndent);
                return indentedCodeLine;
            } 
            else
            {
                // Trim the original line
                return line.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating indented code line: {ex.Message}");
        }

        // Return the original line if an exception occurs
        return line;
    }

    private Image CreateImageBlock(string line)
    {
        int startIndex = line.IndexOf('(') + 1;
        int endIndex = line.IndexOf(')', startIndex);
        string imageUrl = line[startIndex..endIndex];

        var image = new Image
        {
            Aspect = ImageAspect,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = new Thickness(0),
        };

        LoadImageAsync(imageUrl).ContinueWith(task =>
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                var imageSource = task.Result;
                MainThread.BeginInvokeOnMainThread(() => image.Source = imageSource);
            }
        });

        return image;
    }
    private Border CreateCodeBlock(string codeText, out Label contentLabel)
    {
        Label content = new()
        {
            Text = codeText.Trim('`', ' '),
            FontSize = CodeBlockFontSize,
            FontAutoScalingEnabled = true,
            FontFamily = CodeBlockFontFace,
            TextColor = CodeBlockTextColor,
            BackgroundColor = Colors.Transparent
        };
        contentLabel = content;
        return new Border
        {
            Padding = new Thickness(10),
            Stroke = new SolidColorBrush(CodeBlockBorderColor),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            StrokeThickness = 1f,
            BackgroundColor = CodeBlockBackgroundColor,
            Content = content
        };
    }

    /// <summary>
    /// Parses a single line of text containing Markdown syntax and returns a formatted string.
    /// </summary>
    /// <param name="line">The input text line containing Markdown elements.</param>
    /// <param name="textColor">The default text color for the line.</param>
    /// <returns>A FormattedString representing the parsed Markdown.</returns>
    private FormattedString CreateFormattedString(string line, Color textColor)
    {
        var formattedString = new FormattedString();

        // Split the input line into parts, detecting Markdown syntax and email links
        var parts = Regex.Split(line, @"(\*\*.*?\*\*|__.*?__|_.*?_|~~.*?~~|`.*?`|\[.*?\]\(.*?\)|\*.*?\*|\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b)");

        foreach (var part in parts)
        {
            Span span = new()
            {
                TextColor = TextColor
            };

            if (EmailRegex.IsMatch(part)) // Detect email addresses
            {
                var email = EmailRegex.Match(part).Value;
                span.Text = email;
                span.TextColor = HyperlinkColor; // Use hyperlink color for email
                span.TextDecorations = TextDecorations.Underline;

                // Add tap gesture recognizer to trigger email link handling
                var emailTapGestureRecognizer = new TapGestureRecognizer();
                emailTapGestureRecognizer.Tapped += (_, _) => TriggerEmailClicked(email);
                span.GestureRecognizers.Add(emailTapGestureRecognizer);
            }
            else if (part.StartsWith("`") && part.EndsWith("`")) // Inline code block
            {
                span.Text = part.Trim('`'); // Remove the backticks
                span.BackgroundColor = CodeBlockBackgroundColor;
                span.FontFamily = CodeBlockFontFace;
                span.TextColor = CodeBlockTextColor;
            }
            else if (part.StartsWith("**") && part.EndsWith("**")) // Bold text
            {
                var nestedFormatted = CreateFormattedString(part.Trim('*', ' '), textColor);
                foreach (var nestedSpan in nestedFormatted.Spans)
                {
                    nestedSpan.FontAttributes = FontAttributes.Bold;
                    formattedString.Spans.Add(nestedSpan);
                }
                continue; // Skip adding this span since it's already handled
            }
            else if (part.StartsWith("__") && part.EndsWith("__")) // Bold text (alternative syntax)
            {
                span.TextColor = textColor;
                span.Text = part.Trim('_', ' ');
                span.FontAttributes = FontAttributes.Bold;
            }
            else if (part.StartsWith('_') && part.EndsWith('_')) // Italic text (alternative syntax)
            {
                span.TextColor = textColor;
                span.Text = part.Trim('_', ' ');
                span.FontAttributes = FontAttributes.Italic;
            }
            else if (part.StartsWith("~~") && part.EndsWith("~~")) // Strikethrough text
            {
                span.TextColor = textColor;
                span.Text = part.Trim('~');
                span.TextDecorations = TextDecorations.Strikethrough;
            }
            else if (part.StartsWith('[') && part.Contains("](")) // Markdown links
            {
                // Extract link text and URL
                var linkText = part.Substring(1, part.IndexOf(']') - 1);
                var linkUrl = part.Substring(part.IndexOf('(') + 1, part.IndexOf(')') - part.IndexOf('(') - 1);

                span.Text = linkText;
                span.TextColor = HyperlinkColor; // Use hyperlink color for links
                span.TextDecorations = TextDecorations.Underline;

                // Add tap gesture recognizer to trigger hyperlink handling
                var linkTapGestureRecognizer = new TapGestureRecognizer();
                linkTapGestureRecognizer.Tapped += (_, _) => TriggerHyperLinkClicked(linkUrl);
                span.GestureRecognizers.Add(linkTapGestureRecognizer);
            }
            else if (part.StartsWith('*') && part.EndsWith('*')) // Italic text
            {
                span.TextColor = textColor;
                span.Text = part.Trim('*');
                span.FontAttributes = FontAttributes.Italic;
            }
            else // Plain text
            {
                span.Text = part;
            }

            // Apply common properties for all spans
            span.FontSize = TextFontSize;
            span.FontFamily = TextFontFace;

            // Add the span to the formatted string
            formattedString.Spans.Add(span);
        }

        return formattedString;
    }

    private Grid CreateInlineKatexBlock(string line, Color textColor)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Auto }
        },
            RowDefinitions =
        {
            new RowDefinition { Height = GridLength.Auto }
        }
        };

        var match = KaTeXInlineRegex.Match(line);

        if (match.Success)
        {
            string beforeText = line[..match.Index];
            string katexFormula = match.Groups[1].Value;
            string afterText = line[(match.Index + match.Length)..];

            if (!string.IsNullOrEmpty(beforeText))
            {
                var beforeLabel = new Label
                {
                    LineHeight = LineHeightMultiplier,
                    Text = beforeText,
                    TextColor = textColor,
                    FontSize = TextFontSize,
                    FontFamily = TextFontFace,
                    VerticalOptions = LayoutOptions.Center
                };
                grid.Children.Add(beforeLabel);
                Grid.SetColumn(beforeLabel, 0);
            }

            var latexView = new LatexView
            {
                Text = katexFormula,
                FontSize = (float)TextFontSize * 4,
                TextColor = TextColor,
                HighlightColor = Colors.Transparent,
                ErrorColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(-10,-10)
            };
            grid.Children.Add(latexView);
            Grid.SetColumn(latexView, 1);

            if (!string.IsNullOrEmpty(afterText))
            {
                var afterLabel = new Label
                {
                    LineHeight = LineHeightMultiplier,
                    Text = afterText,
                    TextColor = textColor,
                    FontSize = TextFontSize,
                    FontFamily = TextFontFace,
                    VerticalOptions = LayoutOptions.Center
                };
                grid.Children.Add(afterLabel);
                Grid.SetColumn(afterLabel, 2);
            }
        }
        return grid;
    }


    private void AddBulletPointToGrid(Grid grid, int gridRow)
    {
        string bulletPointSign = "-";

#if ANDROID
    bulletPointSign = "\u2022";
#endif
#if iOS
    bulletPointSign = "\u2029";
#endif

        var bulletPoint = new Label
        {
            LineHeight = LineHeightMultiplier,
            Text = bulletPointSign,
            FontSize = Math.Ceiling(TextFontSize * 1.1),
            FontFamily = TextFontFace,
            TextColor = TextColor,
            FontAutoScalingEnabled = false,
            VerticalOptions = LayoutOptions.Start,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Start,
            HorizontalOptions = LayoutOptions.Start,
            Margin = (ListIndent != _defaultListIndent) ? ListIndent : _defaultListIndent,
            Padding = new Thickness(0, 0),
        };

        grid.Children.Add(bulletPoint);
        Grid.SetRow(bulletPoint, gridRow);
        Grid.SetColumn(bulletPoint, 0);
    }

    private void AddOrderedListItemToGrid(int listItemIndex, Grid grid, int gridRow)
    {
        var orderedListItem = new Label
        {
            LineHeight = LineHeightMultiplier,
            Text = $"{listItemIndex}.",
            FontSize = TextFontSize,
            FontFamily = TextFontFace,
            TextColor = TextColor,
            FontAutoScalingEnabled = false,
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Start,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Start,
            Margin = (ListIndent != _defaultListIndent) ? ListIndent : _defaultListIndent,
            Padding = new Thickness(0),
            LineBreakMode = LineBreakMode.NoWrap
        };

        grid.Children.Add(orderedListItem);
        Grid.SetRow(orderedListItem, gridRow);
        Grid.SetColumn(orderedListItem, 0);
    }

    private void AddListItemTextToGrid(string listItemText, Grid grid, int gridRow)
    {
        var formattedString = CreateFormattedString(listItemText, TextColor);

        var listItemLabel = new Label
        {
            LineHeight = LineHeightMultiplier,
            FormattedText = formattedString,
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };

        grid.Children.Add(listItemLabel);
        Grid.SetRow(listItemLabel, gridRow);
        Grid.SetColumn(listItemLabel, 1);
    }

    private void AddTaskListItemToGrid(string taskText, bool isChecked, Grid grid, int gridRow)
    {
        var checkbox = new CheckBox
        {
            IsChecked = isChecked,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            HeightRequest = 18,
            WidthRequest = 18,
            Margin = (ListIndent != _defaultListIndent) ? ListIndent : _defaultListIndent,
        };

        grid.Children.Add(checkbox);
        Grid.SetRow(checkbox, gridRow);
        Grid.SetColumn(checkbox, 0);

        // Add the task list item text
        var formattedString = CreateFormattedString(taskText, TextColor);
        var taskLabel = new Label
        {
            LineHeight = LineHeightMultiplier,
            FormattedText = formattedString,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };

        grid.Children.Add(taskLabel);
        Grid.SetRow(taskLabel, gridRow);
        Grid.SetColumn(taskLabel, 1);
    }

    private Grid CreateTable(string[] lines, int startIndex, int endIndex)
    {
        var tableGrid = new Grid
        {
            ColumnSpacing = 2,
            RowSpacing = 2,
            BackgroundColor = Colors.Transparent
        };

        // Parse header cells and alignment indicators
        var headerCells = lines[startIndex].Split('|').Select(cell => cell.Trim()).ToArray();
        var alignmentIndicators = lines[startIndex + 1].Split('|').Select(cell => cell.Trim()).ToArray();

        // Ensure alignmentIndicators has the same length as headerCells
        if (alignmentIndicators.Length != headerCells.Length)
        {
            // Handle the case where alignment indicators are missing or incorrect
            alignmentIndicators = [.. Enumerable.Repeat("", headerCells.Length)];
        }

        // Add columns based on the number of header cells
        for (int i = 0; i < headerCells.Length; i++)
        {
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        // Add header row with alignment
        tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int colIndex = 0; colIndex < headerCells.Length; colIndex++)
        {
            var alignment = GetTextAlignment(alignmentIndicators[colIndex]);

            var border = new Border
            {
                BackgroundColor = TableHeaderBackgroundColor,
                Padding = new Thickness(5)
            };

            var formattedString = CreateFormattedString(headerCells[colIndex], TableHeaderTextColor);

            var headerLabel = new Label
            {
                LineHeight = LineHeightMultiplier,
                FormattedText = formattedString,
                FontAttributes = FontAttributes.Bold,
                FontSize = TableHeaderFontSize,
                FontFamily = TableHeaderFontFace,
                TextColor = TableHeaderTextColor,
                HorizontalOptions = alignment,
                VerticalOptions = LayoutOptions.Center,
            };

            border.Content = headerLabel;

            tableGrid.Children.Add(border);

            Grid.SetColumn(border, colIndex);
            Grid.SetRow(border, 0);
        }

        // Add rows for table content
        int rowIndex = 1;
        for (int i = startIndex + 2; i <= endIndex; i++)
        {
            var rowCells = lines[i].Split('|').Select(cell => cell.Trim()).ToArray();
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int colIndex = 0; colIndex < rowCells.Length; colIndex++)
            {
                var alignment = GetTextAlignment(alignmentIndicators[colIndex]);
                var formattedString = CreateFormattedString(rowCells[colIndex], TableRowTextColor);

                var cellLabel = new Label
                {
                    LineHeight = LineHeightMultiplier,
                    FormattedText = formattedString,
                    FontSize = TableRowFontSize,
                    FontFamily = TableRowFontFace,
                    TextColor = TableRowTextColor,
                    HorizontalOptions = alignment,
                    VerticalOptions = LayoutOptions.Center,
                    Padding = new Thickness(5)
                };
                tableGrid.Children.Add(cellLabel);
                Grid.SetColumn(cellLabel, colIndex);
                Grid.SetRow(cellLabel, rowIndex);
            }
            rowIndex++;
        }

        return tableGrid;
    }


    private LayoutOptions GetTextAlignment(string alignmentIndicator)
    {
        if (alignmentIndicator.StartsWith(":") && alignmentIndicator.EndsWith(":"))
        {
            return LayoutOptions.Center;
        }
        else if (alignmentIndicator.StartsWith(":"))
        {
            return LayoutOptions.Start;
        }
        else if (alignmentIndicator.EndsWith(":"))
        {
            return LayoutOptions.End;
        }

        // Default alignment if no indicators are found
        return LayoutOptions.Start;
    }


    internal void TriggerHyperLinkClicked(string url)
    {
        OnHyperLinkClicked?.Invoke(this, new LinkEventArgs { Url = url });

        if (LinkCommand?.CanExecute(url) == true)
        {
            LinkCommand.Execute(url);
        }
    }

    private void TriggerEmailClicked(string email)
    {
        OnEmailClicked?.Invoke(this, new EmailEventArgs { Email = email });

        if (EMailCommand?.CanExecute(email) == true)
        {
            EMailCommand.Execute(email);
        }
    }



    private async Task<ImageSource> LoadImageAsync(string imageUrl)
    {
        ImageSource imageSource;

        try
        {
            if (System.Buffers.Text.Base64.IsValid(imageUrl))
            {
                byte[] imageBytes = Convert.FromBase64String(imageUrl);
                imageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            }
            else if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri uriResult))
            {

                if (imageUrl != null && _imageCache.TryGetValue(imageUrl, out ImageSource value))
                {
                    return value;
                }
                else
                {
                    try
                    {
                        if (imageUrl.ToLowerInvariant().EndsWith(".svg"))
                        {
                            var httpClient = new HttpClient();

                            var imageBytes = await httpClient.GetByteArrayAsync(uriResult)
                                .ConfigureAwait(false);
                            if (imageBytes != null)
                            {
                                XmlDocument xmlDocument = new();
                                xmlDocument.LoadXml(Encoding.UTF8.GetString(imageBytes));
                                XmlNodeList commentNodes = xmlDocument.SelectNodes("//comment()");
                                foreach (XmlNode comment in commentNodes)
                                {
                                    comment.ParentNode.RemoveChild(comment);
                                }

                                XmlReader xmlReader = XmlReader.Create(new StringReader(xmlDocument.OuterXml));

                                var svg = new SKSvg();
                                SKPicture svgImage = svg.Load(xmlReader);
                                var image = new SKBitmap((int)svg.Picture.CullRect.Width, (int)svg.Picture.CullRect.Height);
                                using (var surface = SKSurface.Create(new SKImageInfo(image.Width, image.Height)))
                                {
                                    var canvas = surface.Canvas;
                                    canvas.Clear(SKColors.Transparent);
                                    canvas.DrawPicture(svg.Picture);
                                    canvas.Flush();
                                    surface.Snapshot().ReadPixels(image.Info, image.GetPixels(), image.RowBytes, 0, 0);
                                }
                                var imageStream = new MemoryStream();
                                image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(imageStream);
                                imageStream.Position = 0;
                                imageSource = ImageSource.FromStream(() => imageStream);
                                if (imageUrl != null) _imageCache[imageUrl] = imageSource;

                                xmlDocument = null;
                                xmlReader.Dispose();
                            }
                            else
                            {
                                Console.WriteLine($"Failed to download image: {imageUrl}");
                                imageSource = default;
                            }
                            httpClient.Dispose();
                        }
                        else
                        {
                            using var httpClient = new HttpClient();
                            var imageBytes = await httpClient.GetByteArrayAsync(uriResult)
                                .ConfigureAwait(false);
                            if (imageBytes != null)
                            {
                                imageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                                if (imageUrl != null) _imageCache[imageUrl] = imageSource;
                            }
                            else
                            {
                                imageSource = default;
                                Console.WriteLine($"Failed to download image: {imageUrl}");
                            }
                            httpClient.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error downloading image: {ex.Message}");
                        throw;
                    }
                }
            }
            else
            {
                imageSource = ImageSource.FromFile(imageUrl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load image: {ex.Message}");
            throw;
        }

        return imageSource ?? ImageSource.FromFile("icon.png");
    }

    ~MarkdownView()
    {
        _imageCache.Clear();
        _imageCache = null;
    }
}