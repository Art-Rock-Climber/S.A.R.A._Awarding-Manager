using sara_coursework.models;
using sara_coursework.Services.Repositories;
using System;
using System.Linq;
using System.Windows;

namespace sara_coursework.Views
{
    /// <summary>
    /// Interaction logic for EditDecreeWindow.xaml
    /// </summary>
    public partial class EditDecreeWindow : Window
    {
        private readonly IDecreeRepository _decreeRepo;
        private readonly IAwardReasonRepository _reasonRepo;
        private readonly Decree _decree;

        public EditDecreeWindow(
            IDecreeRepository decreeRepo,
            IAwardReasonRepository reasonRepo,
            int? decreeId = null)
        {
            InitializeComponent();
            _decreeRepo = decreeRepo;
            _reasonRepo = reasonRepo;

            // Загрузка оснований награждения
            var reasons = _reasonRepo.GetAwardReasons();
            cmbReason.ItemsSource = reasons;

            if (decreeId.HasValue)
            {
                _decree = _decreeRepo.GetDecrees().FirstOrDefault(d => d.Id == decreeId.Value) ?? new Decree();
            }
            else
            {
                _decree = new Decree { Date = DateTime.Today };
            }

            // Привязка данных
            txtNumber.Text = _decree.Number;
            dpDate.SelectedDate = _decree.Date;

            if (_decree.AwardReasonId > 0)
            {
                cmbReason.SelectedItem = reasons.FirstOrDefault(r => r.Id == _decree.AwardReasonId);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                // Обновление объекта
                _decree.Number = txtNumber.Text.Trim();
                _decree.Date = dpDate.SelectedDate ?? DateTime.Today;

                var selectedReason = (AwardReason)cmbReason.SelectedItem;
                _decree.AwardReasonId = selectedReason.Id;
                _decree.AwardReason = selectedReason;

                _decreeRepo.SaveDecree(_decree);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtNumber.Text))
            {
                MessageBox.Show("Введите номер постановления", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату постановления", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbReason.SelectedItem == null)
            {
                MessageBox.Show("Выберите основание награждения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
