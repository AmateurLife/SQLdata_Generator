using System;
using System.Windows;
using System.Windows.Input;
namespace SQLdata_Generator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip)
                {
                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal : WindowState.Maximized;
                }
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.Content = "❐";
                MaximizeButton.ToolTip = "还原";
            }
            else
            {
                MaximizeButton.Content = "□";
                MaximizeButton.ToolTip = "最大化";
            }
        }
    }
}
