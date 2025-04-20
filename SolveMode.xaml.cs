using Microsoft.Win32;
using NonogramLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace Nonograms.NET
{
    /// <summary>
    /// Interaction logic for SolveMode.xaml
    /// </summary>
    public partial class SolveMode : Window
    {
        private BWSolveUI? bw;
        private ColorSolveUI? col;
        ObservableCollection<BWEntry> BWEntries = new();
        ObservableCollection<ColEntry> ColEntries = new();
        public SolveMode()
        {
            InitializeComponent();
            InitList();
            SelectBW.ItemsSource = BWEntries;
            SelectCol.ItemsSource = ColEntries;
        }
        private void InitList()
        {
            BWEntries.Clear();
            ColEntries.Clear();
            FileStream stream = new FileStream(".\\data\\search_dir.dat", FileMode.Open, FileAccess.Read);
            StreamReader paths = new StreamReader(stream);
            string buffer;
            DirectoryInfo folder;
            while (!paths.EndOfStream)
            {
                buffer = paths.ReadLine() ?? "";
                folder = new DirectoryInfo(buffer);
                foreach (BWEntry e in GetBW(folder)) BWEntries.Add(e);
                foreach (ColEntry e in GetCol(folder)) ColEntries.Add(e);
            }
            paths.Close();
        }
        private static List<BWEntry> GetBW(DirectoryInfo folder, bool recursive = true)
        {
            List<BWEntry> result = new();
            foreach (FileInfo f in folder.GetFiles())
            {
                if (f.Name.EndsWith(".ng"))
                {
                    result.Add(new BWEntry(f.FullName));
                }
            }
            if (recursive)
            {
                foreach (DirectoryInfo d in folder.GetDirectories())
                {
                    result.AddRange(GetBW(d, recursive));
                }
            }
            return result;
        }
        private static List<ColEntry> GetCol(DirectoryInfo folder, bool recursive = false)
        {
            List<ColEntry> result = new();
            foreach (FileInfo f in folder.GetFiles())
            {
                if (f.Name.EndsWith(".ngc"))
                {
                    result.Add(new ColEntry(f.FullName));
                }
            }
            if (recursive)
            {
                foreach (DirectoryInfo d in folder.GetDirectories())
                {
                    result.AddRange(GetCol(d, recursive));
                }
            }
            return result;
        }
        private void MainMenu(object source, RoutedEventArgs e)
        {
            StartWindow w = new StartWindow();
            w.Show();
            Close();
        }
        private void EditPaths(object source, RoutedEventArgs ev)
        {
            bw?.time.Stop();
            col?.time.Stop();
            PuzzleDisp.Children.Clear();
            bw = null;
            col = null;
            SelectBW.SelectedIndex = -1;
            SelectCol.SelectedIndex = -1;
            DirectoryEdit d = new DirectoryEdit();
            d.ShowDialog();
            InitList();
        }
        private void OpenPuzzle(object source, SelectionChangedEventArgs ev)
        {
            ListBox l = (ListBox)source;
            if (l.SelectedIndex > -1)
            {
                Title = "Nonograms.NET - Loading...";
                if (l.Equals(SelectBW))
                {
                    PuzzleDisp.Children.Clear();
                    PuzzleBW p = BWEntries[SelectBW.SelectedIndex].puzzle;
                    p.title = BWEntries[SelectBW.SelectedIndex].title;
                    bw = new BWSolveUI(p, PuzzleDisp);
                    Title = $"Nonograms.NET - {p.title}";
                }
                else if (l.Equals(SelectCol))
                {
                    PuzzleDisp.Children.Clear();
                    PuzzleCol p = ColEntries[SelectCol.SelectedIndex].puzzle;
                    p.title = ColEntries[SelectCol.SelectedIndex].title;
                    col = new ColorSolveUI(p, PuzzleDisp);
                    Title = $"Nonograms.NET - {p.title}";
                }
            }
                
        }
    }
    internal static class PuzzleUtils
    {
        public static int FillCountBW(PuzzleBW p)
        {
            int result = 0;
            foreach (bool cell in p.cells)
            {
                if (cell) result++;
            }
            return result;
        }
        public static int FillCountCol(PuzzleCol p)
        {
            int result = 0;
            foreach (byte cell in p.cells)
            {
                if (cell != 0) result++;
            }
            return result;
        }
    }
    internal struct BWEntry
    {
        public PuzzleBW puzzle { get{ return Parser.ParseBW(path); } }
        public string path { get; private set; }
        public string title { get; private set; }
        public int width { get => puzzle.width; }
        public int height { get => puzzle.height; }
        public BWEntry(string f)
        {
            string[] buffer;
            buffer = f.Split('\\');
            title = buffer[buffer.Length - 1].Split('.')[0];
            path = f;
        }
    }
    internal struct ColEntry
    {
        public PuzzleCol puzzle { get{ return Parser.ParseCol(path); } }
        public string path { get; private set; }
        public string title { get; private set; }
        public int width { get => puzzle.width; }
        public int height { get => puzzle.height; }
        public ColEntry(string f)
        {
            string[] buffer;
            buffer = f.Split('\\');
            title = buffer[buffer.Length - 1].Split('.')[0];
            path = f;
        }
    }
    internal class BWSolveUI
    {
        public PuzzleBW puzzle;
        public Panel container;
        public UniformGrid cells;
        private UIMode mode = UIMode.Idle;
        private static SolidColorBrush White = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private static SolidColorBrush Black = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private static BitmapImage cross = new BitmapImage(new Uri(".\\resource\\cross.png", UriKind.Relative));
        private TextBlock modeDisp = new TextBlock();
        private TextBlock timeDisp = new TextBlock { };
        private TextBlock mistakeDisp = new TextBlock { };
        private int mistakes = 0;
        private int fills = 0;
        private int goal;
        private int seconds = 0;
        public DispatcherTimer time = new DispatcherTimer();
        private bool active = true;
        private ScrollViewer columnScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden };
        private ScrollViewer rowScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        public PuzzleBW reference;
        public BWSolveUI(PuzzleBW p, Panel c)
        {
            time.Interval = TimeSpan.FromSeconds(1);
            time.Tick += (object? source, EventArgs ev) =>
            {
                seconds++;
                timeDisp.Text = $"Time elapsed: {((int)(seconds / 3600)).ToString().PadLeft(2, '0')}:{((int)((seconds % 3600) / 60)).ToString().PadLeft(2, '0')}:{(seconds % 60).ToString().PadLeft(2, '0')}";
            };
            reference = p;
            puzzle = new PuzzleBW(p.width, p.height);
            goal = PuzzleUtils.FillCountBW(p);
            container = c;
            Button b;
            StackPanel stats = new StackPanel();
            TextBlock title = new TextBlock { FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center };
            title.Text = reference.title;
            stats.Children.Add(title);
            timeDisp.Text = "Time elapsed: 00:00:00";
            stats.Children.Add(timeDisp);
            mistakeDisp.Text = "Mistakes: 0";
            stats.Children.Add(mistakeDisp);
            container.Children.Add(stats);
            DockPanel.SetDock(stats, Dock.Top);
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
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\marker.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Cross: Cross out a single cell.",
            };
            b.Click += (source, ev) => { mode = UIMode.Cross; modeDisp.Text = "Cross"; };
            tools.Items.Add(b);
            container.Children.Add(tools);
            DockPanel.SetDock(tools, Dock.Bottom);
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { });
            UniformGrid ug = new UniformGrid { Columns = 1, VerticalAlignment = VerticalAlignment.Center };
            StackPanel sp;
            foreach (List<int> row in reference.rowKeys)
            {
                sp = new StackPanel { Orientation = Orientation.Horizontal };
                foreach (int i in row)
                {
                    sp.Children.Add(new TextBlock
                    {
                        Width = 16,
                        Height = 16,
                        Text = i.ToString()
                    });
                }
                ug.Children.Add(sp);
            }
            rowScroll.Content = ug;
            g.Children.Add(rowScroll);
            Grid.SetRow(rowScroll, 1);
            ug = new UniformGrid { Rows = 1, HorizontalAlignment = HorizontalAlignment.Center };
            foreach (List<int> col in reference.columnKeys)
            {
                sp = new StackPanel { Orientation = Orientation.Vertical };
                foreach (int i in col)
                {
                    sp.Children.Add(new TextBlock
                    {
                        Width = 16,
                        Height = 16,
                        Text = i.ToString()
                    });
                }
                ug.Children.Add(sp);
            }
            columnScroll.Content = ug;
            g.Children.Add(columnScroll);
            Grid.SetColumn(columnScroll, 1);
            cells = new UniformGrid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            cells.Columns = puzzle.width;
            cells.Rows = puzzle.height;
            for (int i = 0; i < puzzle.cells.Length; i++)
            {
                b = new IndexedButton
                {
                    Width = 16,
                    Height = 16,
                    Background = White,
                    index = i,
                    ClickMode = ClickMode.Press,
                };
                b.MouseEnter += EditCell;
                b.Click += EditCell;
                cells.Children.Add(b);
            }
            ScrollViewer sc = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            sc.Content = cells;
            sc.ScrollChanged += (object source, ScrollChangedEventArgs ev) =>
            {
                rowScroll.ScrollToVerticalOffset(sc.VerticalOffset);
                columnScroll.ScrollToHorizontalOffset(sc.HorizontalOffset);
            };
            g.Children.Add(sc);
            Grid.SetColumn(sc, 1);
            Grid.SetRow(sc, 1);
            container.Children.Add(g);
            time.Start();
        }
        private void EditCell(object source, RoutedEventArgs ev)
        {
            Mouse.Capture(null);
            if (active && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                IndexedButton b = (IndexedButton)source;
                switch (mode)
                {
                    case UIMode.Draw:
                        int i = b.index;
                        if (!reference.cells[i])
                        {
                            MessageBox.Show("A mistake has been detected.", "Nonograms.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
                            mistakes++;
                            mistakeDisp.Text = $"Mistakes: {mistakes}";
                            b.Content = new Image { Source = cross, Stretch = Stretch.Fill };
                            break;
                        }
                        else
                        {
                            if (!puzzle.cells[i]) fills++;
                            b.Content = null;
                            b.Background = Black;
                            puzzle.cells[i] = true;
                            if (fills == goal) PuzzleSolved();
                            break;

                        }
                    case UIMode.Erase:
                        if (puzzle.cells[b.index]) fills--;
                        b.Content = null;
                        b.Background = White;
                        puzzle.cells[b.index] = false;
                        break;
                    case UIMode.Cross:
                        if (puzzle.cells[b.index]) fills--;
                        b.Content = new Image { Source = cross, Stretch = Stretch.Fill };
                        b.Background = White;
                        puzzle.cells[b.index] = false;
                        break;
                    default:
                        break;
                }
            }
        }
        private void PuzzleSolved()
        {
            time.Stop();
            active = false;
            MessageBox.Show($"The puzzle has been solved!\n{timeDisp.Text}\n{mistakeDisp.Text}", "Nonograms.NET", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    internal class ColorSolveUI
    {
        public PuzzleCol puzzle;
        public Panel container;
        public UniformGrid cells;
        private static SolidColorBrush White = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private static SolidColorBrush Black = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private byte colorIndex = 0;
        private List<Brush> palette = new();
        private Brush BGBrush;
        private UIMode mode = UIMode.Idle;
        private TextBlock modeDisp = new TextBlock();
        private Rectangle colorDisp = new Rectangle { };
        private ToolBar paletteDisp = new ToolBar();
        private static BitmapImage cross = new BitmapImage(new Uri(".\\resource\\cross.png", UriKind.Relative));
        private TextBlock timeDisp = new TextBlock { };
        private TextBlock mistakeDisp = new TextBlock { };
        private int mistakes = 0;
        private int fills = 0;
        private int goal;
        private int seconds = 0;
        public DispatcherTimer time = new DispatcherTimer();
        private bool active = true;
        private ScrollViewer columnScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden };
        private ScrollViewer rowScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        public PuzzleCol reference;
        private static Color ColorConvert(System.Drawing.Color c) => Color.FromArgb(c.A, c.R, c.G, c.B);
        private static System.Drawing.Color ColorConvert(Color c) => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        public ColorSolveUI(PuzzleCol p, Panel c)
        {
            time.Interval = TimeSpan.FromSeconds(1);
            time.Tick += (object? source, EventArgs ev) =>
            {
                seconds++;
                timeDisp.Text = $"Time elapsed: {((int)(seconds / 3600)).ToString().PadLeft(2, '0')}:{((int)((seconds % 3600) / 60)).ToString().PadLeft(2, '0')}:{(seconds % 60).ToString().PadLeft(2, '0')}";
            };
            reference = p;
            puzzle = new PuzzleCol(p.width, p.height, p.palette);
            goal = PuzzleUtils.FillCountCol(p);
            container = c;
            foreach (System.Drawing.Color color in puzzle.palette) palette.Add(new SolidColorBrush(ColorConvert(color)));
            BGBrush = palette[0];
            colorDisp.Fill = BGBrush;
            Button b;
            StackPanel stats = new StackPanel();
            TextBlock title = new TextBlock { FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center };
            title.Text = reference.title;
            stats.Children.Add(title);
            timeDisp.Text = "Time elapsed: 00:00:00";
            stats.Children.Add(timeDisp);
            mistakeDisp.Text = "Mistakes: 0";
            stats.Children.Add(mistakeDisp);
            container.Children.Add(stats);
            DockPanel.SetDock(stats, Dock.Top);
            ToolBar tools = new ToolBar();
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
            ToolBarTray paletteTray = new ToolBarTray { Orientation = Orientation.Vertical };
            paletteTray.ToolBars.Add(paletteDisp);
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
                Content = new Image { Source = new BitmapImage(new Uri(".\\resource\\marker.png", UriKind.Relative)), Stretch = Stretch.Uniform },
                Width = 32,
                Height = 32,
                ToolTip = "Cross: Cross out a single cell.",
            };
            b.Click += (source, ev) => { mode = UIMode.Cross; modeDisp.Text = "Cross"; };
            tools.Items.Add(b);
            container.Children.Add(tools);
            DockPanel.SetDock(tools, Dock.Bottom);
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { });
            g.Children.Add(colorDisp);
            System.Drawing.Color col;
            UniformGrid ug = new UniformGrid { Columns = 1, VerticalAlignment = VerticalAlignment.Center };
            StackPanel sp;
            for (int i = 0; i < reference.rowKeys.Length; i++)
            {
                sp = new StackPanel { Orientation = Orientation.Horizontal };
                for (int j = 0; j < reference.rowKeys[i].Count; j++)
                {
                    col = reference.palette[reference.rowValueKeys[i][j]];
                    sp.Children.Add(new TextBlock
                    {
                        Width = 16,
                        Height = 16,
                        Text = reference.rowKeys[i][j].ToString(),
                        Background = palette[reference.rowValueKeys[i][j]],
                        Foreground = col.R < 128 && col.G < 128 && col.B < 128 && col.A > 128 ? White : Black
                    });
                }
                ug.Children.Add(sp);
            }
            rowScroll.Content = ug;
            g.Children.Add(rowScroll);
            Grid.SetRow(rowScroll, 1);
            ug = new UniformGrid { Rows = 1, HorizontalAlignment = HorizontalAlignment.Center };
            for (int i = 0; i < reference.columnKeys.Length; i++)
            {
                sp = new StackPanel { Orientation = Orientation.Vertical };
                for (int j = 0; j < reference.columnKeys[i].Count; j++)
                {
                    col = reference.palette[reference.columnValueKeys[i][j]];
                    sp.Children.Add(new TextBlock
                    {
                        Width = 16,
                        Height = 16,
                        Text = reference.columnKeys[i][j].ToString(),
                        Background = palette[reference.columnValueKeys[i][j]],
                        Foreground = col.R < 128 && col.G < 128 && col.B < 128 && col.A > 128 ? White : Black
                    });
                }
                ug.Children.Add(sp);
            }
            columnScroll.Content = ug;
            g.Children.Add(columnScroll);
            Grid.SetColumn(columnScroll, 1);
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
            sc.ScrollChanged += (object source, ScrollChangedEventArgs ev) =>
            {
                rowScroll.ScrollToVerticalOffset(sc.VerticalOffset);
                columnScroll.ScrollToHorizontalOffset(sc.HorizontalOffset);
            };
            g.Children.Add(sc);
            Grid.SetColumn(sc, 1);
            Grid.SetRow(sc, 1);
            container.Children.Add(g);
            time.Start();
        }
        private void ColorSelect(object source, RoutedEventArgs ev)
        {
            IndexedButton b = (IndexedButton)source;
            colorIndex = (byte)b.index;
            colorDisp.Fill = palette[colorIndex];
        }
        private void EditCell(object source, RoutedEventArgs ev)
        {
            Mouse.Capture(null);
            if (active && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                IndexedButton b = (IndexedButton)source;
                switch (mode)
                {
                    case UIMode.Draw:
                        int i = b.index;
                        b.Content = null;
                        if (reference.cells[i] != colorIndex)
                        {
                            MessageBox.Show("A mistake has been detected.", "Nonograms.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
                            mistakes++;
                            mistakeDisp.Text = $"Mistakes: {mistakes}";
                            break;
                        }
                        else
                        {
                            if (puzzle.cells[i] != colorIndex && colorIndex > 0) fills++;
                            b.Background = palette[colorIndex];
                            puzzle.cells[i] = colorIndex;
                            if (fills == goal) PuzzleSolved();
                            break;

                        }
                    case UIMode.Erase:
                        if (puzzle.cells[b.index] > 0) fills--;
                        b.Content = null;
                        b.Background = BGBrush;
                        puzzle.cells[b.index] = 0;
                        break;
                    case UIMode.Cross:
                        if (puzzle.cells[b.index] > 0) fills--;
                        b.Content = new Image { Source = cross, Stretch = Stretch.Fill };
                        b.Background = BGBrush;
                        puzzle.cells[b.index] = 0;
                        break;
                    default:
                        break;
                }
            }
        }
        private void PuzzleSolved()
        {
            time.Stop();
            active = false;
            MessageBox.Show($"The puzzle has been solved!\n{timeDisp.Text}\n{mistakeDisp.Text}", "Nonograms.NET", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
