using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PaintDotNet;
using PaintDotNet.IndirectUI;
using PaintDotNet.Collections;
using PaintDotNet.PropertySystem;
using PaintDotNet.Effects;
using IntSliderControl = System.Int32;
using CheckboxControl = System.Boolean;
using ListBoxControl = System.Byte;
using MultiLineTextboxControl = System.String;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("WallOfText plugin for Paint.NET")]
[assembly: AssemblyDescription("Arranges text to fill the selection rectangle")]
[assembly: AssemblyConfiguration("text, typography, justified text, block text")]
[assembly: AssemblyCompany("Robot Graffiti")]
[assembly: AssemblyProduct("WallOfText")]
[assembly: AssemblyCopyright("Copyright ©2023 by Robot Graffiti")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.1.*")]
[assembly: AssemblyMetadata("BuiltByCodeLab", "Version=6.6.8256.1816")]
[assembly: SupportedOSPlatform("Windows")]

namespace WallOfTextEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
            }
        }

        public string Copyright
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
            }
        }

        public string DisplayName
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            }
        }

        public Version Version
        {
            get
            {
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("https://www.getpaint.net/redirect/plugins.html");
            }
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Wall of Text")]
    public class WallOfTextEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName
        {
            get
            {
                return "Wall of Text";
            }
        }

        public static Image StaticIcon
        {
            get
            {
                return new Bitmap(typeof(WallOfTextEffectPlugin), "WallOfText.png");
            }
        }

        public static string SubmenuName
        {
            get
            {
                return "Text Formations";
            }
        }

        public WallOfTextEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, new EffectOptions() { Flags = EffectFlags.Configurable | EffectFlags.SingleThreaded })
        {
        }

        public enum PropertyNames
        {
            Text,
            TextFont,
            IsBold,
            IsItalic,
            IsUppercase,
            Outline,
            RiserSpace,
            DescenderSpace,
            TextVertPadding,
            AreaPadding,
            Antialias,
            WordWrap
        }

        public enum LeaveSpaceOptions
        {
            Always,
            AsNeeded,
            Never
        }

        public enum AntialiasOptions
        {
            None,
            SlopesAndCurves,
            Everything
        }


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            FontFamily[] installedFontFamilies = new InstalledFontCollection().Families;
            int TextFontDefaultValueIndex = Array.FindIndex(installedFontFamilies, ff => ff.Name.Equals("Impact", StringComparison.OrdinalIgnoreCase));
            if (TextFontDefaultValueIndex < 0)
            {
                TextFontDefaultValueIndex = 0;
            }

            List<Property> props = new()
            {
                new StringProperty(PropertyNames.Text, @"Wall of text", 32767),
                new BooleanProperty(PropertyNames.WordWrap, true),
                new StaticListChoiceProperty(PropertyNames.TextFont, installedFontFamilies, TextFontDefaultValueIndex, false),
                new BooleanProperty(PropertyNames.IsBold, false),
                new BooleanProperty(PropertyNames.IsItalic, false),
                new BooleanProperty(PropertyNames.IsUppercase, true),
                new Int32Property(PropertyNames.Outline, 0, 0, 100),
                StaticListChoiceProperty.CreateForEnum(PropertyNames.RiserSpace, LeaveSpaceOptions.Never, false),
                StaticListChoiceProperty.CreateForEnum(PropertyNames.DescenderSpace, LeaveSpaceOptions.Never, false),
                new Int32Property(PropertyNames.TextVertPadding, 3, -100, 100),
                new Int32Property(PropertyNames.AreaPadding, 20, 0, 100),
                StaticListChoiceProperty.CreateForEnum(PropertyNames.Antialias, AntialiasOptions.SlopesAndCurves, false)
            };

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Text, ControlInfoPropertyNames.DisplayName, "Text");
            configUI.SetPropertyControlType(PropertyNames.Text, PropertyControlType.TextBox);
            configUI.SetPropertyControlValue(PropertyNames.Text, ControlInfoPropertyNames.Multiline, true);
            configUI.SetPropertyControlValue(PropertyNames.TextFont, ControlInfoPropertyNames.DisplayName, "Font");
            PropertyControlInfo TextFontFontFamilyControl = configUI.FindControlForPropertyName(PropertyNames.TextFont);
            FontFamily[] TextFontFontFamilies = new InstalledFontCollection().Families;
            foreach (FontFamily ff in TextFontFontFamilies)
            {
                TextFontFontFamilyControl.SetValueDisplayName(ff, ff.Name);
            }
            configUI.SetPropertyControlValue(PropertyNames.IsBold, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.IsBold, ControlInfoPropertyNames.Description, "Bold");
            configUI.SetPropertyControlValue(PropertyNames.IsItalic, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.IsItalic, ControlInfoPropertyNames.Description, "Italic");
            configUI.SetPropertyControlValue(PropertyNames.IsUppercase, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.IsUppercase, ControlInfoPropertyNames.Description, "UPPERCASE");
            configUI.SetPropertyControlValue(PropertyNames.Outline, ControlInfoPropertyNames.DisplayName, "Outline");
            configUI.SetPropertyControlValue(PropertyNames.RiserSpace, ControlInfoPropertyNames.DisplayName, "Extra space between lines for risers");
            PropertyControlInfo RiserSpaceOptionControl = configUI.FindControlForPropertyName(PropertyNames.RiserSpace);
            RiserSpaceOptionControl.SetValueDisplayName(LeaveSpaceOptions.Always, "Always");
            RiserSpaceOptionControl.SetValueDisplayName(LeaveSpaceOptions.AsNeeded, "Where Needed");
            RiserSpaceOptionControl.SetValueDisplayName(LeaveSpaceOptions.Never, "Never");
            configUI.SetPropertyControlValue(PropertyNames.DescenderSpace, ControlInfoPropertyNames.DisplayName, "Extra space between lines for descenders");
            PropertyControlInfo DescenderSpaceOptionControl = configUI.FindControlForPropertyName(PropertyNames.DescenderSpace);
            DescenderSpaceOptionControl.SetValueDisplayName(LeaveSpaceOptions.Always, "Always");
            DescenderSpaceOptionControl.SetValueDisplayName(LeaveSpaceOptions.AsNeeded, "Where Needed");
            DescenderSpaceOptionControl.SetValueDisplayName(LeaveSpaceOptions.Never, "Never");
            configUI.SetPropertyControlValue(PropertyNames.TextVertPadding, ControlInfoPropertyNames.DisplayName, "Space between lines (px)");
            configUI.SetPropertyControlValue(PropertyNames.AreaPadding, ControlInfoPropertyNames.DisplayName, "Padding around text (px)");
            configUI.SetPropertyControlValue(PropertyNames.Antialias, ControlInfoPropertyNames.DisplayName, "Antialias");
            PropertyControlInfo AntialiasControl = configUI.FindControlForPropertyName(PropertyNames.Antialias);
            AntialiasControl.SetValueDisplayName(AntialiasOptions.None, "None");
            AntialiasControl.SetValueDisplayName(AntialiasOptions.SlopesAndCurves, "Slopes and curves");
            AntialiasControl.SetValueDisplayName(AntialiasOptions.Everything, "Everything");
            configUI.SetPropertyControlValue(PropertyNames.WordWrap, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.WordWrap, ControlInfoPropertyNames.Description, "Word wrap");

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Change the effect's window title
            props[ControlInfoPropertyNames.WindowTitle].Value = "Wall of Text";
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "Wall of Text v1.1\nCopyright ©2023 by Robot Graffiti\nAll rights reserved.";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Text = token.GetProperty<StringProperty>(PropertyNames.Text).Value;
            WordWrap = token.GetProperty<BooleanProperty>(PropertyNames.WordWrap).Value;
            TextFont = (FontFamily)token.GetProperty<StaticListChoiceProperty>(PropertyNames.TextFont).Value;
            IsBold = token.GetProperty<BooleanProperty>(PropertyNames.IsBold).Value;
            IsItalic = token.GetProperty<BooleanProperty>(PropertyNames.IsItalic).Value;
            IsUppercase = token.GetProperty<BooleanProperty>(PropertyNames.IsUppercase).Value;
            Outline = token.GetProperty<Int32Property>(PropertyNames.Outline).Value;
            RiserSpaceOption = (byte)(int)token.GetProperty<StaticListChoiceProperty>(PropertyNames.RiserSpace).Value;
            DescenderSpaceOption = (byte)(int)token.GetProperty<StaticListChoiceProperty>(PropertyNames.DescenderSpace).Value;
            TextVertPadding = token.GetProperty<Int32Property>(PropertyNames.TextVertPadding).Value;
            AreaPadding = token.GetProperty<Int32Property>(PropertyNames.AreaPadding).Value;
            Antialias = (byte)(int)token.GetProperty<StaticListChoiceProperty>(PropertyNames.Antialias).Value;

            PreRender();

            base.OnSetRenderInfo(token, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface,SrcArgs.Surface,rois[i]);
            }
        }

        #region User Entered Code
        // Name: Wall of Text
        // Submenu: Text Formations
        // Author: Robot Graffiti
        // Title: Wall of Text
        // Version: 1.1
        // Desc: Arranges text to fill the selection rectangle
        // Keywords: Text, Typography, Justified text, Block text
        // URL:
        // Help:
        // Force Single Threaded
        
        #region UICode
        MultiLineTextboxControl Text = "Lorem_ipsum dolor sit amet, consectetur adipiscing elit. Mauris et interdum dolor, placerat aliquet leo. Morbi pellentesque vehicula condimentum. Nulla quam lacus, fermentum ut nulla eu, mattis scelerisque est. Aenean porttitor magna at dui efficitur, at maximus tellus feugiat. Fusce sed ornare lectus. Sed sed metus tristique, accumsan felis facilisis, tempor lorem. Morbi suscipit maximus malesuada. In hac habitasse platea dictumst. Quisque et tempor ligula. Lorem_ipsum dolor sit amet, consectetur adipiscing elit. Mauris et interdum dolor, placerat aliquet leo. Morbi pellentesque vehicula condimentum. Nulla quam lacus, fermentum ut nulla eu, mattis scelerisque est. Aenean porttitor magna at dui efficitur, at maximus tellus feugiat. Fusce sed ornare lectus. Sed sed metus tristique, accumsan felis facilisis, tempor lorem. Morbi suscipit maximus malesuada. In hac habitasse platea dictumst. Quisque et tempor ligula. Lorem_ipsum dolor sit amet, consectetur adipiscing elit. Mauris et interdum dolor, placerat aliquet leo. Morbi pellentesque vehicula condimentum. Nulla quam lacus, fermentum ut nulla eu, mattis scelerisque est. Aenean porttitor magna at dui efficitur, at maximus tellus feugiat. Fusce sed ornare lectus. Sed sed metus tristique, accumsan felis facilisis, tempor lorem. Morbi suscipit maximus malesuada. In hac habitasse platea dictumst. Quisque et tempor ligula. Lorem_ipsum dolor sit amet, consectetur adipiscing elit. Mauris et interdum dolor, placerat aliquet leo. Morbi pellentesque vehicula condimentum. Nulla quam lacus, fermentum ut nulla eu, mattis scelerisque est. Aenean porttitor magna at dui efficitur, at maximus tellus feugiat. Fusce sed ornare lectus. Sed sed metus tristique, accumsan felis facilisis, tempor lorem. Morbi suscipit maximus malesuada. In hac habitasse platea dictumst. Quisque et tempor ligula."; // [32767] Text|Hello World
        CheckboxControl WordWrap = true; // Word wrap
        FontFamily TextFont = new("Impact"); // Font
        CheckboxControl IsBold = false; // Bold
        CheckboxControl IsItalic = false; // Italic
        CheckboxControl IsUppercase = true; // UPPERCASE
        IntSliderControl Outline = 5; // [0,10] Outline thickness
        ListBoxControl RiserSpaceOption = 2; // Extra space between lines for risers|Always|Where Needed|Never
        ListBoxControl DescenderSpaceOption = 2; // Extra space between lines for descenders|Always|Where Needed|Never
        IntSliderControl TextVertPadding = 3; // [-100,100] Space between lines (px)
        IntSliderControl AreaPadding = 20; // [0,100] Padding around text (px)
        ListBoxControl Antialias = 1; // Antialias|None|Slopes and curves|Everything
        #endregion
        //MultiLineTextboxControl Text = "Lorem ipsum dolor sit amet,\r\nconsectetur adipiscing elit. Mauris et interdum dolor, placerat aliquet leo. Morbi pellentesque vehicula condimentum. Nulla quam lacus, fermentum ut nulla eu, mattis scelerisque est. Aenean porttitor magna at dui efficitur, at maximus tellus feugiat. Fusce sed ornare lectus. Sed sed metus tristique, accumsan felis facilisis, tempor lorem. Morbi suscipit maximus malesuada. In hac habitasse platea dictumst. Quisque et tempor ligula."; // [32767] Text|Hello World


        // Working surface
        readonly Surface[] wrk = new Surface[4];

        List<string> lines;
        List<RectangleF> lineSizes;
        List<RectangleF> lineBoxes;
        bool isValid = false;
        
        const int fontSize = 8;
        
        void MeasureLines(List<string> lines, Rectangle selection, out List<RectangleF> lineSizes, out List<RectangleF> lineBoxes, out RectangleF size)
        {
            lineSizes = new();
            lineBoxes = new();
            float drop = 0;
        
            RectangleF smallestSize = RectangleF.Empty;
            RectangleF biggestSize = RectangleF.Empty;
        
            if(RiserSpaceOption == 0 || DescenderSpaceOption == 0)
            {
                biggestSize = Measure("QWERTYUIOPASDFGHJKLZXCVBNMijftqgyp");
            }
            if(RiserSpaceOption == 2 || DescenderSpaceOption == 2)
            {
                smallestSize = IsUppercase ? Measure("V") : Measure("v");
            }
        
            foreach (var line in lines)
            {
                var lineSize = Measure(line);
                lineSizes.Add(lineSize);
                var originalHeight = lineSize.Height;
                var boxHeight = lineSize.Height * selection.Width / lineSize.Width;
                float topAdjustment;
                switch (RiserSpaceOption)
                {
                    case 0: // Always
                        topAdjustment = biggestSize.Top - lineSize.Top;
                        break;
                    case 1: // Where Needed
                        topAdjustment = 0;
                        break;
                    case 2: // Never
                        topAdjustment = smallestSize.Top - lineSize.Top;
                        break;
                    default : throw new NotImplementedException();
                }
                float bottomAdjustment;
                switch (DescenderSpaceOption)
                {
                    case 0: // Always
                        bottomAdjustment = biggestSize.Bottom - lineSize.Bottom;
                        break;
                    case 1: // Where Needed
                        bottomAdjustment = 0;
                        break;
                    case 2: // Never
                        bottomAdjustment = smallestSize.Bottom - lineSize.Bottom;
                        break;
                    default : throw new NotImplementedException();
                }
                var boxTop = selection.Top + drop - topAdjustment * selection.Width / lineSize.Width;
        
                var box = new RectangleF(selection.Left, boxTop, selection.Width, boxHeight);
                lineBoxes.Add(box);
                drop = box.Bottom - selection.Top;
                drop += bottomAdjustment * selection.Width / lineSize.Width;
                drop += TextVertPadding + 2 * Outline;
            }
            size = new(selection.Top, selection.Left, selection.Width, drop - TextVertPadding - 2 * Outline);
        }

        static float GetScore(List<RectangleF> lineBoxes, Rectangle selection, RectangleF size)
        {
            float heightVariance = lineBoxes.Max(box => box.Height) - lineBoxes.Min(box => box.Height);
            float endAccuracy = Math.Abs(size.Height - selection.Height);
            return endAccuracy + heightVariance;
        }
        
        readonly static object cacheLock = new();
        readonly Dictionary<string, RectangleF> measuredStringsByParameterSummary = new();
        readonly Dictionary<string, GraphicsPath> pathedStringsByParameterSummary = new();
        readonly LinkedList<string> pathQueue = new();
        
        readonly Dictionary<int, GraphicsPath> scaledPaths = new();
        
        string GetParameterSummary(string s)
        {
            return $"{RiserSpaceOption}{DescenderSpaceOption}{IsBold}{IsItalic}{TextFont.Name}{s}";
        }
        
        GraphicsPath GetTextPath(string text)
        {
            void Touch(string parameterSummary)
            {
                pathQueue.Remove(parameterSummary);
                pathQueue.AddLast(parameterSummary);
            }
        
            void Enqueue(string s)
            {
                while (pathQueue.Count >= 50)
                {
                    var oldS = pathQueue.First.Value;
                    pathQueue.RemoveFirst();
                    var oldPath = pathedStringsByParameterSummary[oldS];
                    pathedStringsByParameterSummary.Remove(oldS);
                    oldPath.Dispose();
                }
                pathQueue.AddLast(s);
            }
        
            lock (cacheLock)
            {
                string s = text.Replace('_', ' ');
                string summary = GetParameterSummary(s);
        
                if (pathedStringsByParameterSummary.TryGetValue(summary, out GraphicsPath path))
                {
                    Touch(summary);
                    return path;
                }
        
                path = new(FillMode.Winding);
                int fontStyle = GetFontStyle();
                path.AddString(s, TextFont, fontStyle, fontSize, Point.Empty, StringFormat.GenericTypographic);
                //path.Flatten(null, 0.02f);
        
                Enqueue(summary);
                pathedStringsByParameterSummary[summary] = path;
                return path;
            }
        }
        
        int GetFontStyle()
        {
            int fontStyle = (int)FontStyle.Regular;
            if (IsBold) fontStyle |= (int)FontStyle.Bold;
            if (IsItalic) fontStyle |= (int)FontStyle.Italic;
            return fontStyle;
        }
        
        RectangleF Measure(string s)
        {
            string summary = GetParameterSummary(s);
            if (measuredStringsByParameterSummary.TryGetValue(summary, out var rect))
                return rect;
        
            rect = GetTextPath(s).GetBounds();
        
            measuredStringsByParameterSummary[summary] = rect;
            return rect;
        }
        
        void ShrinkLines(List<string> lines, List<RectangleF> lineSizes)
        {
            float minWidth = float.MaxValue;
            int bestJ = -1;
            for (int j = 1; j < lines.Count; j++)
            {
                var width = lineSizes[j - 1].Width + lineSizes[j].Width;
                if (width < minWidth)
                {
                    minWidth = width;
                    bestJ = j;
                }
            }
            var word1 = lines[bestJ - 1];
            var word2 = lines[bestJ];
            var newWord = $"{word1} {word2}";
            lines.RemoveAt(bestJ);
            lines[bestJ - 1] = newWord;
        }
        
        void GrowLines(List<string> lines, List<RectangleF> lineSizes, Random r)
        {
            float maxWidth = float.MinValue;
            int bestJ = -1;
            for (int j = 0; j < lines.Count; j++)
            {
                if (!lines[j].Any(char.IsWhiteSpace)) continue;
                var width = lineSizes[j].Width;
                if (width > maxWidth)
                {
                    maxWidth = width;
                    bestJ = j;
                }
            }
            if (bestJ == -1) return;
            var word = lines[bestJ];
            int halfWay = lines[bestJ].Length / 2;
            var firstHalf = word[..halfWay];
            var secondHalf = word[halfWay..];
            var firstSpaceIndex = firstHalf.Length - firstHalf
                .Reverse()
                .TakeWhile(c => !char.IsWhiteSpace(c))
                .Count();
            var secondSpaceIndex = secondHalf
                .TakeWhile(c => !char.IsWhiteSpace(c))
                .Count();
        
            bool useFirst;
            if (firstSpaceIndex <= 0) useFirst = false;
            else if (secondSpaceIndex == secondHalf.Length) useFirst = true;
            else if (secondSpaceIndex < firstHalf.Length - firstSpaceIndex) useFirst = false;
            else useFirst = true;
        
            int splitPoint;
            if (useFirst)
            {
                splitPoint = firstSpaceIndex;
            }
            else
            {
                splitPoint = firstHalf.Length + secondSpaceIndex;
            }
            var word1 = word.Substring(0, splitPoint).Trim();
            var word2 = word.Substring(splitPoint).Trim();
        
            lines[bestJ] = word1;
            lines.Insert(bestJ + 1, word2);
        }
        
        void GrowLinesRandom(List<string> lines, List<RectangleF> lineSizes, Random r)
        {
            float maxWidth = float.MinValue;
            int bestJ = -1;
            for (int j = 0; j < lines.Count; j++)
            {
                if (!lines[j].Any(char.IsWhiteSpace)) continue;
                var width = lineSizes[j].Width;
                if (width > maxWidth)
                {
                    maxWidth = width;
                    bestJ = j;
                }
            }
            if (bestJ == -1) return;
            var line = lines[bestJ];
        
            var words = line.Split();
        
            int splitPoint = r.Next(words.Length);
                
            string line1 = string.Join(' ', words.Take(splitPoint));
            string line2 = string.Join(' ', words.Skip(splitPoint));
            lines[bestJ] = line1;
            lines.Insert(bestJ + 1, line2);
        }
        
        List<string> OptimiseLineBreaks(string text, Rectangle selection)
        {
            List<string> lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> words = text.Split().Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            List<string> bestLines = text.Split().Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            float bestScore = float.MaxValue;
        
            List<string[]> lineWords = lines.Select(line => line.Split()).ToList();
        
            var r = new Random(5); // Not random
            
            for (int i = Math.Min(40, words.Count * 2 + 2); i > 0; i--)
            {
                if (IsCancelRequested) return bestLines;
                List<string> newLines = new(lines);
        
                for (int k = words.Count; k > 0; k--)
                {
                    MeasureLines(newLines, selection, out var newLineSizes, out var newLineBoxes, out var allLinesSize);
                    var score = GetScore(newLineBoxes, selection, allLinesSize);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestLines = new(newLines);
                    }
                    if (allLinesSize.Height >= selection.Height) break;
        
                    if(r.Next(5) == 0)
                        GrowLinesRandom(newLines, newLineSizes, r);
                    else
                        GrowLines(newLines, newLineSizes, r);
                }
            }
            return bestLines;
        }
        
        
        // This single-threaded function is called after the UI changes and before the Render function is called
        // The purpose is to prepare anything you'll need in the Render function
        void PreRender()
        {
            isValid = false;
            if (string.IsNullOrWhiteSpace(Text)) return;
        
            Rectangle selection = EnvironmentParameters.SelectionBounds;
            selection.Inflate(-AreaPadding, -AreaPadding);
            selection.Inflate(-Outline, -Outline);
            if (selection.IsEmpty) return;
            if (selection.Width <= 0) return;
            if (selection.Height <= 0) return;
            var text = IsUppercase ? Text.ToUpper() : Text;
            text = text.Replace("\u00A0", "_");
            List<string> bestLines;
        
            if (WordWrap)
            {
                bestLines = OptimiseLineBreaks(text, selection);
            }
            else
            {
                bestLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            
            MeasureLines(bestLines, selection, out var newLineSizes, out var newLineBoxes, out var size);
        
            AdjustHeightsToFitSelection(newLineBoxes, selection, size);
            
            lines = bestLines;
            lineSizes = newLineSizes;
            lineBoxes = newLineBoxes;
        
            lock (cacheLock)
            {
                foreach (var path in scaledPaths.Values)
                    path.Dispose();
                scaledPaths.Clear();
            }
        
            isValid = true;
        }
        
        void AdjustHeightsToFitSelection(List<RectangleF> newLineBoxes, Rectangle selection, RectangleF size)
        {
            var currentHeight = size.Height;
            var desiredHeight = selection.Height;
        
            if (Math.Abs(currentHeight - desiredHeight) < 0.5f) return;
        
            for (int l = 0; l < newLineBoxes.Count; l++)
            {
                RectangleF box = newLineBoxes[l];
                var newBoxBottom = selection.Top + (box.Bottom - selection.Top) * desiredHeight / currentHeight; // Adjust the bottom, instead of the height, to reduce rounding errors
                var newBoxTop = selection.Top + (box.Top - selection.Top) * desiredHeight / currentHeight;
                box.Height = newBoxBottom - newBoxTop;
                box.Y = newBoxTop;
                newLineBoxes[l] = box;
            }
        }
        
        static readonly object graphicsLock = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Round(float f) => (int)(f + 0.5f);
        static PointF Round(PointF p) => new(Round(p.X), Round(p.Y));

        static PointF GetScaledPoint(RectangleF rect, RectangleF textSize, PointF point)
        {
            return new((point.X - textSize.X) * rect.Width / textSize.Width + rect.X, (point.Y - textSize.Y) * rect.Height / textSize.Height + rect.Y);
        }
        
        
        GraphicsPath GetScaledPath(int index, string text, RectangleF textSize, RectangleF rect)
        {
            if (text.Length == 0) return null;
            if (textSize.Width == 0) return null;
            if (textSize.Height == 0) return null;
        
            lock (cacheLock)
            {
                if (scaledPaths.TryGetValue(index, out GraphicsPath scaledPath)) return scaledPath;
            
                GraphicsPath path = GetTextPath(text);
                
                PointF[] points = path.PathPoints;
                PathPointType[] types = path.PathTypes.Select(t => (PathPointType)t).ToArray();
                List<PointF> scaledPathPoints = new();
                List<byte> scaledPathTypes = new();
                
                int counter = 0;
                for (int i = 0; i < points.Length; i++)
                {
                    PointF point = GetScaledPoint(rect, textSize, points[i]);
                    PathPointType type = types[i];
                    if (Antialias == 1) point = Round(point);
                    if ((type & PathPointType.PathTypeMask) != PathPointType.Line || i == 0 || point != scaledPathPoints.Last())
                    {
                        scaledPathPoints.Add(point);
                        scaledPathTypes.Add((byte)type);
                    }
                    else
                    {
                        counter++;
                    }
                }
        
                
        #if DEBUG
        Debug.WriteLine(text);
        Debug.WriteLine($"Skipped {counter}");
        #endif
        
                scaledPath = new(scaledPathPoints.ToArray(), scaledPathTypes.ToArray(), path.FillMode);
                scaledPaths[index] = scaledPath;
                return scaledPath;
            }
        }

        void InitialiseWrk(Rectangle box)
        {
            lock (graphicsLock)
            {
                var width = Math.Max(128 + 2 * Outline, box.Width);
                var height = Math.Max(128 + 2 * Outline, box.Height);
                if (wrk[0] == null)
                {
                    for (int i = 0; i < wrk.Length; i++) wrk[i] = new(width, height);
                }
                else if (wrk[0].Width < width || wrk[0].Height < height)
                {
                    width = Math.Max(wrk[0].Width, width);
                    height = Math.Max(wrk[0].Height, height);
                    for (int i = 0; i < wrk.Length; i++) wrk[i].Dispose();
                    for (int i = 0; i < wrk.Length; i++) wrk[i] = new(width, height);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int DistSquared(int x, int y) => x * x + y * y;


        void DrawLine(Graphics g, Brush brush, GraphicsPath path)
        {
            g.FillPath(brush, path);
        }

        void DrawLineWithOutlineFastNoAntialiasing(Graphics g, Brush brush, GraphicsPath path, Rectangle rect, Rectangle clipRect, RectangleF boxF, Surface dst, Surface wrk1, Surface wrk2, ColorBgra primaryColor, ColorBgra secondaryColor)
        {
            Rectangle box = new((int)boxF.X, (int)boxF.Y, (int)Math.Ceiling(boxF.Width), (int)Math.Ceiling(boxF.Height));
            box.Inflate(Outline, Outline);
            box.Intersect(clipRect);
            using Region gClipRegion = new(box);
            g.Clip = gClipRegion;
            Rectangle box2 = Rectangle.Intersect(box, rect);
            box.X -= clipRect.X;
            box.Y -= clipRect.Y;
            box2.X -= clipRect.X;
            box2.Y -= clipRect.Y;
            wrk1.Clear();
            wrk2.Clear();

            // Draw to wrk1
            g.FillPath(brush, path);

            // Propagate edges over work surface
            int rightColumn = box.Right - 1;
            int bottomRow = box.Bottom - 1;
            for (int w = 0; w < Outline; w++)
            {
                {
                    // Top row
                    int y = box.Top;
                    int down = y + 1;

                    // Top left corner
                    {
                        int x = box.Left;
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x + 1, y] != default || wrk1[x, down] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }

                    // Middle of top row
                    for (int x = box.Left + 1; x < rightColumn; x++)
                    {
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x + 1, y] != default || wrk1[x - 1, y] != default || wrk1[x, down] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }

                    // Top right corner
                    {
                        int x = rightColumn;
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x - 1, y] != default || wrk1[x, down] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }
                }

                // Middle rows
                for (int y = box.Top + 1; y < bottomRow; y++)
                {
                    int up = y - 1;
                    int down = y + 1;

                    // Left column
                    {
                        int x = box.Left;
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x + 1, y] != default || wrk1[x, up] != default || wrk1[x, down] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }

                    // Centre
                    for (int x = box.Left + 1; x < rightColumn; x++)
                    {
                        if (wrk2[x, y] == default)
                        {
                            var thisPixel = wrk1[x, y];
                            if (thisPixel != default) wrk2[x, y] = thisPixel;
                            else if (wrk1[x - 1, y] != default || wrk1[x + 1, y] != default || wrk1[x, up] != default || wrk1[x, down] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }
                    // Right column
                    {
                        int x = rightColumn;
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x - 1, y] != default || wrk1[x, up] != default || wrk1[x, down] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }
                }

                {
                    // Bottom row
                    int y = bottomRow;
                    int up = y - 1;
                    // Bottom left corner
                    {
                        int x = box.Left;
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x + 1, y] != default || wrk1[x, up] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }

                    // Middle of bottom row
                    for (int x = box.Left + 1; x < rightColumn; x++)
                    {
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x + 1, y] != default || wrk1[x - 1, y] != default || wrk1[x, up] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }

                    // Bottom right corner
                    {
                        int x = rightColumn;
                        if (wrk2[x, y] == default)
                        {
                            if (wrk1[x, y] != default) wrk2[x, y] = wrk1[x, y];
                            else if (wrk1[x - 1, y] != default || wrk1[x, up] != default) wrk2[x, y] = ColorBgra.Green;
                        }
                    }
                }

                (wrk1, wrk2) = (wrk2, wrk1); // Swap work surfaces so wrk1 is always the one most recently drawn to
            }

            // Draw colours to page
            for (int y = box2.Top; y < box2.Bottom; y++)
            {
                for (int x = box2.Left; x < box2.Right; x++)
                {
                    var thisPixel = wrk1[x, y];
                    ColorBgra textColour = default;
                    if (thisPixel == ColorBgra.Red) textColour = secondaryColor;
                    else if (thisPixel == ColorBgra.Green) textColour = primaryColor;
                    else continue;

                    byte textColourAlpha = textColour.A;
                    textColour.A = 255;
                    dst[x + clipRect.X, y + clipRect.Y] = ColorBgra.Overwrite(dst[x + clipRect.X, y + clipRect.Y], textColour, textColourAlpha);
                }
            }
        }

        void DrawLineWithOutline(Graphics g, Brush brush, GraphicsPath path, Rectangle rect, Rectangle clipRect, RectangleF boxF, Surface dst, ColorBgra primaryColor, ColorBgra secondaryColor)
        {
            Rectangle box = new((int)boxF.X, (int)boxF.Y, (int)Math.Ceiling(boxF.Width), (int)Math.Ceiling(boxF.Height));
            box.Inflate(Outline, Outline);
            box.Intersect(clipRect);
            using Region gClipRegion = new(box);
            g.Clip = gClipRegion;
            Rectangle box2 = Rectangle.Intersect(box, rect);
            box.X -= clipRect.X;
            box.Y -= clipRect.Y;
            box2.X -= clipRect.X;
            box2.Y -= clipRect.Y;
            g.Clear(Color.Black);

            g.FillPath(brush, path);

            {
                ColorBgra infinityColour = ColorBgra.FromBgra(255, 255, 0, 255);
                var wrk0 = wrk[0];
                var wrk1 = wrk[1];
                var wrk2 = wrk[2];
                var wrk3 = wrk[3];
                for (int y = box.Top; y < box.Bottom; y++)
                {
                    for (int x = box.Left; x < box.Right; x++)
                    {
                        var thisPixel = wrk0[x, y];
                        ColorBgra colour = thisPixel.R == 0 ? infinityColour : ColorBgra.FromBgra(0, 0, thisPixel.R, 255);
                        wrk0[x, y] = wrk1[x, y] = wrk2[x, y] = wrk3[x, y] = colour;
                    }
                }
            }

            // R: Fraction
            // G: X distance
            // B: Y distance
            Propagate(wrk[0], box, box2, 1, 1);
            Propagate(wrk[1], box, box2, -1, -1);
            Propagate(wrk[2], box, box2, -1, 1);
            Propagate(wrk[3], box, box2, 1, -1);

            for (int y = box2.Top; y < box2.Bottom; y++)
            {
                for (int x = box2.Left; x < box2.Right; x++)
                {
                    ColorBgra wrkPixel = default;
                    int xDist = int.MaxValue; int yDist = int.MaxValue; int adjustDist = default;
                    double dist = double.PositiveInfinity;
                    for (int w = 0; w < wrk.Length; w++)
                    {
                        ColorBgra wrkPixel2 = wrk[w][x, y];
                        int xDist2 = wrkPixel2.G;
                        int yDist2 = wrkPixel2.B;
                        int adjustDist2 = wrkPixel2.R;
                        double dist2 = Math.Sqrt(DistSquared(xDist2, yDist2));
                        if (Antialias > 0) dist2 -= adjustDist2 / 255.0;
                        if (dist2 < dist)
                        {
                            xDist = xDist2;
                            yDist = yDist2;
                            dist = dist2;
                            adjustDist = adjustDist2;
                            wrkPixel = wrkPixel2;
                        }
                    }

                    if (dist > Outline) continue;

                    var dstX = x + clipRect.X;
                    var dstY = y + clipRect.Y;
                    ColorBgra dstPixel = dst[dstX, dstY];
                    ColorBgra result;
                    if (Antialias == 0)
                    {
                        ColorBgra textColour = xDist + yDist > 0 ? primaryColor : secondaryColor;
                        byte textColourAlpha = textColour.A;
                        textColour.A = 255;
                        result = ColorBgra.Overwrite(dstPixel, textColour, textColourAlpha);
                    }
                    else
                    {
                        ColorBgra textColour = xDist + yDist > 0 ? primaryColor : ColorBgra.Lerp(primaryColor, secondaryColor, wrkPixel.R);
                        byte textColourAlpha = (byte)Math.Round(textColour.A * Math.Clamp(Outline - dist, 0, 1));
                        textColour.A = 255;
                        result = ColorBgra.Overwrite(dstPixel, textColour, textColourAlpha);
                    }

                    dst[dstX, dstY] = result;
                }
            }
        }

        private void Propagate(Surface workSurface, Rectangle outerBox, Rectangle innerBox, int xDirection, int yDirection)
        {
            int xStart = xDirection == 1 ? outerBox.Left + 1 : outerBox.Right - 2;
            Func<int, bool> isEndX = xDirection == 1 ? x => x < innerBox.Right : x => x >= innerBox.Left;


            int yStart = yDirection == 1 ? outerBox.Top + 1 : outerBox.Bottom - 2;
            Func<int, bool> isEndY = yDirection == 1 ? y => y < innerBox.Bottom : y => y >= innerBox.Top;

            int xEdge = xDirection == 1 ? outerBox.Left : outerBox.Right - 1;

            int minY = outerBox.Top;
            int maxY = outerBox.Bottom - 1;

            // This loop is unrolled a little bit, it does two rows at a time.
            for (int y = yStart; isEndY(y); y += 2 * yDirection)
            {
                var upperY = Math.Clamp(y - yDirection, minY, maxY); // Make sure not to go out of bounds vertically
                var lowerY = Math.Clamp(y + yDirection, minY, maxY);
                var leftPixel = workSurface[xEdge, y];
                var upperLeftPixel = workSurface[xEdge, upperY];
                var lowerLeftPixel = workSurface[xEdge, lowerY];
                for (int x = xStart; isEndX(x); x+= xDirection)
                {
                    var centrePixel = workSurface[x, y];
                    var upperPixel = workSurface[x, upperY];
                    centrePixel = PropagateFromClosestPixel(centrePixel, leftPixel, upperPixel, upperLeftPixel);
                    workSurface[x, y] = centrePixel;

                    var lowerPixel = workSurface[x, lowerY];
                    lowerPixel = PropagateFromClosestPixel(lowerPixel, lowerLeftPixel, centrePixel, leftPixel);
                    workSurface[x, lowerY] = lowerPixel;

                    leftPixel = centrePixel;
                    lowerLeftPixel = lowerPixel;
                    upperLeftPixel = upperPixel;
                }
            }
        }

        ColorBgra PropagateFromClosestPixel(ColorBgra thisPixel, ColorBgra prevPixelX, ColorBgra prevPixelY, ColorBgra prevPixelXY)
        {
            var thisDist = DistSquared(thisPixel.B, thisPixel.G) * 256 - thisPixel.R;
            var leftDist = DistSquared(prevPixelX.B, prevPixelX.G + 1) * 256 - prevPixelX.R;
            var upperLeftDist = DistSquared(prevPixelXY.B + 1, prevPixelXY.G + 1) * 256 - prevPixelXY.R;
            var upperDist = DistSquared(prevPixelY.B + 1, prevPixelY.G) * 256 - prevPixelY.R;
            if (thisDist <= leftDist && thisDist <= upperLeftDist && thisDist <= upperDist) return thisPixel;
            if (leftDist <= upperLeftDist && leftDist <= upperDist)
            {
                return ColorBgra.FromBgraClamped(prevPixelX.B, prevPixelX.G + 1, prevPixelX.R, 255);
            }
            else if (upperDist <= upperLeftDist)
            {
                return ColorBgra.FromBgraClamped(prevPixelY.B + 1, prevPixelY.G, prevPixelY.R, 255);
            }
            else
            {
                return ColorBgra.FromBgraClamped(prevPixelXY.B + 1, prevPixelXY.G + 1, prevPixelXY.R, 255);
            }
        }

        // Here is the main multi-threaded render function
        // The dst canvas is broken up into rectangles and
        // your job is to write to each pixel of that rectangle
        void Render(Surface dst, Surface src, Rectangle rect)
        {
            // Copy the src surface to the dst surface
            dst.CopySurface(src, rect.Location, rect);

            if (!isValid) return;
        
            Rectangle clipRect = rect;
            clipRect.Inflate(Outline, Outline);
            clipRect.Intersect(dst.Bounds);
        
            if (!lineBoxes.Any(box => box.IntersectsWith(clipRect))) return;
            
            lock (graphicsLock)
            {
                if (Outline > 0) InitialiseWrk(clipRect);

                // Setup for drawing using GDI+ commands
                using Graphics g = new RenderArgs(Outline > 0 ? wrk[0] : dst).Graphics;
                using SolidBrush brush = new(Outline > 0 ? Color.Red : EnvironmentParameters.PrimaryColor);
                if (Outline == 0)
                {
                    using Region gClipRegion = new(clipRect);
                    g.Clip = gClipRegion;
                }
                else
                {
                    g.TranslateTransform(-clipRect.X, -clipRect.Y);
                }
                if (Antialias > 0) g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;
                ColorBgra secondaryColor = EnvironmentParameters.SecondaryColor;
                
                int i = -1;
                foreach (var line in lines)
                {
                    if (IsCancelRequested) return;
                    i++;
                    RectangleF boxF = lineBoxes[i];
                    if (!boxF.IntersectsWith(clipRect)) continue;

                    var path = GetScaledPath(i, line, lineSizes[i], boxF);

                    if (Outline == 0) DrawLine(g, brush, path);
                    else if (Antialias == 0 && Outline < 3) DrawLineWithOutlineFastNoAntialiasing(g, brush, path, rect, clipRect, boxF, dst, wrk[0], wrk[1], primaryColor, secondaryColor);
                    else DrawLineWithOutline(g, brush, path, rect, clipRect, boxF, dst, primaryColor, secondaryColor);
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
        
            lock(cacheLock)
            {
                foreach (var path in pathedStringsByParameterSummary.Values)
                    path.Dispose();
                pathedStringsByParameterSummary.Clear();
                foreach (var path in scaledPaths.Values)
                    path.Dispose();
                scaledPaths.Clear();
            }
        
            lock (graphicsLock)
            {
                for (int i = 0; i < wrk.Length; i++) { wrk[i]?.Dispose(); wrk[i] = null; }
            }
        }
        
        #endregion
    }
}
