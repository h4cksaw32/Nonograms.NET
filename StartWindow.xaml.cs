using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nonograms.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }
        private void GotoSolve(object source, RoutedEventArgs e)
        {
            SolveMode w = new SolveMode();
            w.Show();
            Close();
        }
        private void GotoCreate(object source, RoutedEventArgs e)
        {
            CreateMode w = new CreateMode();
            w.Show();
            Close();
        }
    }
}