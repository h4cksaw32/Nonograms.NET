using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for CreateDialog.xaml
    /// </summary>
    public partial class CreateDialog : Window
    {
        public CreateDialog()
        {
            InitializeComponent();
        }
        public byte PuzzleWidth { get => (byte)WidthSel.Value; }
        public byte PuzzleHeight { get => (byte)HeightSel.Value; }
        public bool IsColored { get => ModeSel.IsChecked ?? false; }
        private void Submit(object source, RoutedEventArgs ev)
        {
            DialogResult = true;
        }
    }
}
