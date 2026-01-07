using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GameAssistantMVP.Views
{
    /// <summary>
    /// ScreenSelectionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenSelectionWindow : Window
    {
        private Point _startPoint;
        public Rect? SelectedRegion { get; private set; }
        public ScreenSelectionWindow()
        {
            InitializeComponent();

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            this.Left = 0;
            this.Top = 0;
            this.Width = screenWidth;
            this.Height = screenHeight;

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _startPoint = e.GetPosition(this);
                SelectionRectangle.Width = 0;
                SelectionRectangle.Height = 0;
                Canvas.SetLeft(SelectionRectangle, _startPoint.X);
                Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            }
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(this);
                var x = Math.Min(_startPoint.X, current.X);
                var y = Math.Min(_startPoint.Y, current.Y);
                var width = Math.Abs(current.X - _startPoint.X);
                var height = Math.Abs(current.Y - _startPoint.Y);

                Canvas.SetLeft(SelectionRectangle, x);
                Canvas.SetTop(SelectionRectangle, y);
                SelectionRectangle.Width = width;
                SelectionRectangle.Height = height;
            }
        }
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (SelectionRectangle.Width >= 10 && SelectionRectangle.Height >= 10)
                {
                    SelectedRegion = new Rect(
                        Canvas.GetLeft(SelectionRectangle),
                        Canvas.GetTop(SelectionRectangle),
                        SelectionRectangle.Width,
                        SelectionRectangle.Height
                    );
                    DialogResult = true;
                }
                else
                {
                    DialogResult = false;
                }
                Close();
            }
        }
    }
}
