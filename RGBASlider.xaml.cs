using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nonograms.NET
{
    /// <summary>
    /// This control uses sliders to select a 32-bit RGBA color.
    /// </summary>
    public partial class RGBASlider : UserControl
    {
        /// <value>
        /// The selected color of the control.
        /// </value>
        public Color Color 
        { 
            get => _color; 
            set {
                _color = value;
                Red.Value = (double)value.R;
                Green.Value = (double)value.G;
                Blue.Value = (double)value.B;
                Alpha.Value = (double)value.A;
                Disp.Fill = new SolidColorBrush(value);
            } 
        }
        /// <summary>
        /// The field for the <see cref="Color"/> property.
        /// </summary>
        private Color _color;
        /// <summary>
        /// An event delegate with a Color object.
        /// </summary>
        /// <param name="c">The color submitted by the event</param>
        public delegate void ColoredEvent(Color c);
        /// <summary>
        /// A <see cref="ColoredEvent"/> that is raised when the selected color is changed
        /// </summary>
        public event ColoredEvent? ColorChanged;
        public RGBASlider()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Internal handler for changes in the selected color. Raises the user-defined <see cref="ColorChanged"/> event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ev"></param>
        private void ColorChange(object source, RoutedEventArgs ev)
        {
            Color = Color.FromArgb((byte)Alpha.Value, (byte)Red.Value, (byte)Green.Value, (byte)Blue.Value);
            Disp.Fill = new SolidColorBrush(Color); // Updates color of the left portion of the control
            if (ColorChanged != null) ColorChanged(Color); //Raise user-defined ColorChanged event
        }
    }
}
