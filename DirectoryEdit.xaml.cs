using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Nonograms.NET
{
    /// <summary>
    /// Interaction logic for DirectoryEdit.xaml
    /// </summary>
    public partial class DirectoryEdit : Window
    {
        public ObservableCollection<string> paths { get; private set; }
        public DirectoryEdit()
        {
            InitializeComponent();
            paths = new();
            FileStream stream = new FileStream(".\\data\\search_dir.dat", FileMode.Open, FileAccess.Read);
            StreamReader read = new StreamReader(stream);
            while (!read.EndOfStream)
            {
                paths.Add(read.ReadLine() ?? "");
            }
            read.Close();
            PathList.ItemsSource = paths;
        }
        private void AddPath(object source, RoutedEventArgs ev)
        {
            if (!String.IsNullOrWhiteSpace(PathInput.Text)) paths.Add(PathInput.Text);
        }
        private void DeletePath(object source, RoutedEventArgs ev)
        {
            paths.RemoveAt(PathList.SelectedIndex);
        }
        private void BrowsePath(object source, RoutedEventArgs ev)
        {
            OpenFolderDialog fd = new();
            if (fd.ShowDialog() != null)
            {
                PathInput.Text = fd.FolderName;
            }
        }
        private void SavePaths(object source, RoutedEventArgs ev)
        {
            FileStream stream = new FileStream(".\\data\\search_dir.dat", FileMode.Create, FileAccess.Write);
            StreamWriter write = new StreamWriter(stream);
            foreach (string path in paths) 
            {
                write.WriteLine(path);
            }
            write.Close();
        }
        private void Exit(object source, RoutedEventArgs ev)
        {
            DialogResult = true;
            Close();
        }
    }
}
