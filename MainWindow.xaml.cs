using sara_coursework.models;
using sara_coursework.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace sara_coursework
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(User currentUser)
        {
            InitializeComponent();
            _viewModel = new MainViewModel(currentUser);
            this.DataContext = _viewModel;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1 && !e.Handled && Keyboard.Modifiers == ModifierKeys.None)
            {
                _viewModel.OpenHelpCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject child)
            {
                while (child != null && !(child is TextBox))
                {
                    child = VisualTreeHelper.GetParent(child);
                }
                if (child is TextBox textBox)
                {
                    textBox.Text = string.Empty;
                }
            }
        }
    }
}