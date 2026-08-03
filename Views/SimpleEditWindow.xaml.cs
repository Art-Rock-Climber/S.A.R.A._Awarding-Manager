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
using System.Windows.Shapes;

namespace sara_coursework.Views
{
    /// <summary>
    /// Логика взаимодействия для SimpleEditWindow.xaml
    /// </summary>
    public partial class SimpleEditWindow : Window
    {
        public string ResultText => txtValue.Text;

        public SimpleEditWindow(string title, string prompt, string initialValue = "")
        {
            InitializeComponent();
            Title = title;
            DataContext = this;
            lblPrompt.Content = prompt;
            txtValue.Text = initialValue;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtValue.Text))
            {
                MessageBox.Show("Поле не может быть пустым", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
