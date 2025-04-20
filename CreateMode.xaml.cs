using Microsoft.Win32;
using NonogramLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Nonograms.NET
{
    /// <summary>
    /// Interaction logic for CreateMode.xaml
    /// </summary>
    internal enum EditMode : byte
    {
        Idle,
        BW,
        Color
    }
    public partial class CreateMode : Window
    {
        private EditMode mode = EditMode.Idle;
        private BWEditUI? bw;
        private ColorEditUI? col;
        private string openPath = "";
        public CreateMode()
        {
            InitializeComponent();
        }
        private void MainMenu(object source, RoutedEventArgs e)
        {
            StartWindow w = new StartWindow();
            w.Show();
            Close();
        }
        private void OpenDoc(object source, RoutedEventArgs ev)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/h4cksaw32/Nonograms.NET",
                UseShellExecute = true
            });
        }
        private void OpenPuzzle(object source, RoutedEventArgs ev)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Multiselect = false;
            fd.Filter = "B&W Nonograms |*.ng|Color Nonograms |*.ngc|All files |*.*";
            if (fd.ShowDialog() == true)
            {
                FileStream file = new FileStream(fd.FileName, FileMode.Open, FileAccess.Read);
                byte check = (byte)file.ReadByte();
                if (check == 0)
                {
                    mode = EditMode.BW;
                    PuzzleDisp.Children.Clear();
                    PuzzleBW p = Parser.ParseBW(file);
                    bw = new BWEditUI(p, PuzzleDisp);
                }
                else if (check == 254)
                {
                    mode = EditMode.Color;
                    PuzzleDisp.Children.Clear();
                    PuzzleCol p = Parser.ParseCol(file);
                    col = new ColorEditUI(p, PuzzleDisp);
                }
                else 
                {
                    MessageBox.Show("Could not determine puzzle type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                openPath = fd.FileName;
                string[] buffer = openPath.Split('\\', '/');
                Title = $"Nonograms.NET - Create - {buffer[buffer.Length - 1]}";
            }
        }
        private void NewPuzzle(object source, RoutedEventArgs ev)
        {
            CreateDialog cd = new CreateDialog();
            if (cd.ShowDialog() == true)
            {
                if (!cd.IsColored)
                {
                    mode = EditMode.BW;
                    PuzzleDisp.Children.Clear();
                    PuzzleBW p = new PuzzleBW(cd.PuzzleWidth, cd.PuzzleHeight);
                    bw = new BWEditUI(p, PuzzleDisp);
                }
                else
                {
                    mode = EditMode.Color;
                    PuzzleDisp.Children.Clear();
                    PuzzleCol p = new PuzzleCol(cd.PuzzleWidth, cd.PuzzleHeight);
                    col = new ColorEditUI(p, PuzzleDisp);
                }
                openPath = "";
                Title = "Nonograms.NET - Create - Untitled";
            }
        }
        private void SavePuzzle(object source, RoutedEventArgs ev)
        {
            if (String.IsNullOrEmpty(openPath))
            {
                SavePuzzleAs(source, ev);
                return;
            }
            switch (mode)
            {
                case EditMode.BW:
                    if (bw == null) return;
                    Parser.SerializeBW(openPath, bw.puzzle);
                    break;
                case EditMode.Color:
                    if (col == null) return;
                    Parser.SerializeCol(openPath, col.puzzle);
                    break;
                default:
                    break;
            }
        }
        private void SavePuzzleAs(object source, RoutedEventArgs ev)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "B&W Nonograms |*.ng|Color Nonograms |*.ngc";
            if (fd.ShowDialog() == true)
            {
                switch (mode)
                {
                    case EditMode.BW:
                        if (bw == null) return;
                        Parser.SerializeBW(fd.FileName, bw.puzzle);
                        break;
                    case EditMode.Color:
                        if (col == null) return;
                        Parser.SerializeCol(fd.FileName, col.puzzle);
                        break;
                    default:
                        break;
                }
                if (String.IsNullOrEmpty(openPath)) 
                { 
                    openPath = fd.FileName;
                    string[] buffer = openPath.Split('\\', '/');
                    Title = $"Nonograms.NET - Create - {buffer[buffer.Length - 1]}";
                }
            }
        }
    }
    internal class IndexedButton : Button
    {
        public IndexedButton() : base() { }
        public int index { get; set; }
    }
    internal enum UIMode : byte
    {
        Idle,
        Draw,
        Erase,
        Cross,
        Fill,
    }
    internal class BWEditUI
    {
        public PuzzleBW puzzle;
        public Panel container;
        public UniformGrid cells;
        private UIMode mode = UIMode.Idle;
        private static SolidColorBrush White = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private static SolidColorBrush Black = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private TextBlock modeDisp = new TextBlock();
        public BWEditUI(PuzzleBW p, Panel c)
        {
            puzzle = p;
            container = c;
            Button b;
            ToolBar tools = new ToolBar();
            modeDisp.Text = "None";
            tools.Items.Add(modeDisp);
            tools.Items.Add(new Separator());
            b = new Button
            {
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\pencil.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Draw: Fill in a single cell.",
            };
            b.Click += (source, ev) => { mode = UIMode.Draw; modeDisp.Text = "Draw"; };
            tools.Items.Add(b);
            b = new Button
            {
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\eraser.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Erase: Empty a single cell.",
            };
            b.Click += (source, ev) => { mode = UIMode.Erase; modeDisp.Text = "Erase"; };
            tools.Items.Add(b);
            b = new Button
            {
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\bucket.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Fill: Fills in a blank region.",
            };
            b.Click += (source, ev) => { mode = UIMode.Fill; modeDisp.Text = "Fill"; };
            tools.Items.Add(b);
            container.Children.Add(tools);
            DockPanel.SetDock(tools, Dock.Bottom);
            cells = new UniformGrid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            cells.Columns = puzzle.width;
            cells.Rows = puzzle.height;
            for (int i = 0; i < puzzle.cells.Length; i++)
            {
                b = new IndexedButton
                {
                    Width = 16,
                    Height = 16,
                    Background = puzzle.cells[i] ? Black : White,
                    index = i,
                    ClickMode = ClickMode.Press,
                };
                b.MouseEnter += EditCell;
                b.Click += EditCell;
                cells.Children.Add(b);
            }
            ScrollViewer sc = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            sc.Content = cells;
            container.Children.Add(sc);
        }
        private void EditCell(object source, RoutedEventArgs ev)
        {
            Mouse.Capture(null);
            if(Mouse.LeftButton == MouseButtonState.Pressed)
            {
                IndexedButton b = (IndexedButton)source;
                switch (mode)
                {
                    case UIMode.Draw:
                        b.Background = Black;
                        puzzle.cells[b.index] = true;
                        break;
                    case UIMode.Erase:
                        b.Background = White;
                        puzzle.cells[b.index] = false;
                        break;
                    case UIMode.Fill:
                        FillRegion(b.index);
                        break;
                    default:
                        break;
                }
            }
        }
        private void FillRegion(int index)
        {
            ((IndexedButton)cells.Children[index]).Background = Black;
            puzzle.cells[index] = true;
            if (index % puzzle.width > 0 && !puzzle.cells[index - 1]) FillRegion(index - 1);
            if (index % puzzle.width < puzzle.width - 1 && !puzzle.cells[index + 1]) FillRegion(index + 1);
            if (index >= puzzle.width && !puzzle.cells[index - puzzle.width]) FillRegion(index - puzzle.width);
            if (index < puzzle.cells.Length - puzzle.width && !puzzle.cells[index + puzzle.width]) FillRegion(index + puzzle.width);
        }
    }
    internal class ColorEditUI
    {
        public PuzzleCol puzzle;
        public Panel container;
        public UniformGrid cells;
        private byte colorIndex = 0;
        private List<Brush> palette = new();
        private Brush BGBrush;
        private UIMode mode = UIMode.Idle;
        private TextBlock modeDisp = new TextBlock();
        private TextBlock colorIndexDisp = new TextBlock { FontWeight = FontWeights.Bold, Text = "Background" };
        private RGBASlider colorDisp = new RGBASlider { };
        private ToolBar paletteDisp = new ToolBar();
        private static Color ColorConvert(System.Drawing.Color c) => Color.FromArgb(c.A, c.R, c.G, c.B);
        private static System.Drawing.Color ColorConvert(Color c) => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        public ColorEditUI(PuzzleCol p, Panel c)
        {
            puzzle = p;
            container = c;
            Button b;
            colorDisp.Color = ColorConvert(puzzle.palette[0]);
            foreach (System.Drawing.Color col in puzzle.palette) palette.Add(new SolidColorBrush(ColorConvert(col)));
            BGBrush = palette[0];
            StackPanel sp = new StackPanel();
            sp.Children.Add(colorIndexDisp);
            sp.Children.Add(colorDisp);
            b = new Button { HorizontalAlignment = HorizontalAlignment.Left, Content = "Assign Color", Margin = new Thickness(8) };
            b.Click += ColorChange;
            sp.Children.Add(b);
            container.Children.Add(sp);
            DockPanel.SetDock(sp, Dock.Top);
            ToolBar tools = new ToolBar();
            ToolBarTray paletteTray = new ToolBarTray { Orientation = Orientation.Vertical };
            for (int i = 0; i < puzzle.palette.Count; i++)
            {
                b = new IndexedButton
                {
                    index = i,
                    Background = palette[i],
                    Content = i == 0 ? "BG" : i.ToString(),
                    Width = 32,
                    Height = 32,
                };
                b.Click += ColorSelect;
                paletteDisp.Items.Add(b);
            }
            paletteTray.ToolBars.Add(paletteDisp);
            ToolBar colTools = new ToolBar();
            b = new Button
            {
                Width = 32,
                Height = 32,
                Content = "\u2795",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                ToolTip = "Adds a color to the palette (copies selected color).",
            };
            b.Click += ColorAdd;
            colTools.Items.Add(b);
            b = new Button
            {
                Width = 32,
                Height = 32,
                Content = "\u274c",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
                ToolTip = "Remove selected color from palette (clears cells of that color).\nNOTE: Background color cannot be removed.",
            };
            b.Click += ColorRemove;
            colTools.Items.Add(b);
            paletteTray.ToolBars.Add(colTools);
            container.Children.Add(paletteTray);
            DockPanel.SetDock(paletteTray, Dock.Left);
            modeDisp.Text = "None";
            tools.Items.Add(modeDisp);
            tools.Items.Add(new Separator());
            b = new Button
            {
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\pencil.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Draw: Fill in a single cell.",
            };
            b.Click += (source, ev) => { mode = UIMode.Draw; modeDisp.Text = "Draw"; };
            tools.Items.Add(b);
            b = new Button
            {
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\eraser.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Erase: Empty a single cell.",
            };
            b.Click += (source, ev) => { mode = UIMode.Erase; modeDisp.Text = "Erase"; };
            tools.Items.Add(b);
            b = new Button
            {
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\bucket.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Fill: Fills in a region.",
            };
            b.Click += (source, ev) => { mode = UIMode.Fill; modeDisp.Text = "Fill"; };
            tools.Items.Add(b);
            container.Children.Add(tools);
            DockPanel.SetDock(tools, Dock.Bottom);
            cells = new UniformGrid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            cells.Columns = puzzle.width;
            cells.Rows = puzzle.height;
            for (int i = 0; i < puzzle.cells.Length; i++)
            {
                b = new IndexedButton
                {
                    Width = 16,
                    Height = 16,
                    Background = palette[puzzle.cells[i]],
                    index = i,
                    ClickMode = ClickMode.Press,
                };
                b.MouseEnter += EditCell;
                b.Click += EditCell;
                cells.Children.Add(b);
            }
            ScrollViewer sc = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            sc.Content = cells;
            container.Children.Add(sc);
        }
        private void ColorSelect(object source, RoutedEventArgs ev)
        {
            IndexedButton b = (IndexedButton)source;
            colorIndex = (byte)b.index;
            colorDisp.Color = ColorConvert(puzzle.palette[colorIndex]);
            colorIndexDisp.Text = colorIndex == 0 ? "Background" : $"Color #{colorIndex}";
        }
        private void ColorAdd(object source, RoutedEventArgs ev)
        {
            int i = puzzle.palette.Count;
            puzzle.palette.Add(ColorConvert(colorDisp.Color));
            palette.Add(new SolidColorBrush(colorDisp.Color));
            IndexedButton b = new IndexedButton
            {
                index = i,
                Background = palette[i],
                Content = i.ToString(),
                Width = 32,
                Height = 32,
            };
            b.Click += ColorSelect;
            paletteDisp.Items.Add(b);
        }
        private void ColorRemove(object source, RoutedEventArgs ev)
        {
            paletteDisp.Items.RemoveAt(colorIndex);
            palette.RemoveAt(colorIndex);
            puzzle.palette.RemoveAt(colorIndex);
            BGBrush = palette[0];
            IndexedButton b;
            for (int i = colorIndex; i < paletteDisp.Items.Count; i++)
            {
                b = (IndexedButton)paletteDisp.Items[i];
                b.index--;
                b.Content = i == 0 ? "BG" : i.ToString();
            }
            for (int i = 0; i < puzzle.cells.Length; i++) 
            {
                b = (IndexedButton)cells.Children[i];
                if (puzzle.cells[i] == colorIndex)
                {
                    puzzle.cells[i] = 0;
                    b.Background = BGBrush;
                }
                else if (puzzle.cells[i] > colorIndex)
                {
                    b.Background = palette[--puzzle.cells[i]];
                }
            }
        }
        private void ColorChange(object source, RoutedEventArgs ev)
        {
            palette[colorIndex] = new SolidColorBrush(colorDisp.Color);
            ((IndexedButton)paletteDisp.Items[colorIndex]).Background = palette[colorIndex];
            puzzle.palette[colorIndex] = ColorConvert(colorDisp.Color);
            if (colorIndex == 0) BGBrush = palette[0];
            IndexedButton b;
            for (int i = 0; i < puzzle.cells.Length; i++)
            {
                b = (IndexedButton)cells.Children[i];
                if (puzzle.cells[i] == colorIndex)
                {
                    b.Background = palette[colorIndex];
                }
            }
        }
        private void EditCell(object source, RoutedEventArgs ev)
        {
            Mouse.Capture(null);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                IndexedButton b = (IndexedButton)source;
                switch (mode)
                {
                    case UIMode.Draw:
                        b.Background = palette[colorIndex];
                        puzzle.cells[b.index] = colorIndex;
                        break;
                    case UIMode.Erase:
                        b.Background = BGBrush;
                        puzzle.cells[b.index] = 0;
                        break;
                    case UIMode.Fill:
                        FillRegion(b.index, puzzle.cells[b.index]);
                        break;
                    default:
                        break;
                }
            }
        }
        private void FillRegion(int index, byte filter)
        {
            ((IndexedButton)cells.Children[index]).Background = palette[colorIndex];
            puzzle.cells[index] = colorIndex;
            if (index % puzzle.width > 0 && puzzle.cells[index - 1] == filter) FillRegion(index - 1, filter);
            if (index % puzzle.width < puzzle.width - 1 && puzzle.cells[index + 1] == filter) FillRegion(index + 1, filter);
            if (index >= puzzle.width && puzzle.cells[index - puzzle.width] == filter) FillRegion(index - puzzle.width, filter);
            if (index < puzzle.cells.Length - puzzle.width && puzzle.cells[index + puzzle.width] == filter) FillRegion(index + puzzle.width, filter);
        }
    }
}
