using sara_coursework.models;
using sara_coursework.Services.Repositories;
using System;
using System.Linq;
using System.Windows;

namespace sara_coursework.Views
{
    /// <summary>
    /// Interaction logic for EditSingleAwardWindow.xaml
    /// </summary>
    public partial class EditSingleAwardWindow : Window
    {
        private readonly IAwardAssignmentRepository _assignmentRepo;
        private readonly IDecreeRepository _decreeRepo;
        private readonly IAwardRepository _awardRepo;
        private readonly IAwardedRepository _awardedRepo;
        private readonly AwardAssignment _assignment;

        public EditSingleAwardWindow(
            IAwardAssignmentRepository assignmentRepo,
            IDecreeRepository decreeRepo,
            IAwardRepository awardRepo,
            IAwardedRepository awardedRepo,
            int assignmentId)
        {
            InitializeComponent();
            _assignmentRepo = assignmentRepo;
            _decreeRepo = decreeRepo;
            _awardRepo = awardRepo;
            _awardedRepo = awardedRepo;

            _assignment = _assignmentRepo.GetAwardAssignments()
                .First(a => a.Id == assignmentId);

            LoadData();
            DataContext = this;

            cmbDecree.SelectionChanged += (s, e) => {
                if (cmbDecree.SelectedItem != null) {
                    cmbDecree.ClearValue(BorderBrushProperty);
                    cmbDecree.ClearValue(BorderThicknessProperty);
                }
            };
            cmbAward.SelectionChanged += (s, e) => {
                if (cmbAward.SelectedItem != null) {
                    cmbAward.ClearValue(BorderBrushProperty);
                    cmbAward.ClearValue(BorderThicknessProperty);
                }
            };
            cmbAwarded.SelectionChanged += (s, e) => {
                if (cmbAwarded.SelectedItem != null) {
                    cmbAwarded.ClearValue(BorderBrushProperty);
                    cmbAwarded.ClearValue(BorderThicknessProperty);
                }
            };
        }

        private void LoadData()
        {
            cmbDecree.ItemsSource = _decreeRepo.GetDecrees();
            cmbAward.ItemsSource = _awardRepo.GetAwards();

            var list = _awardedRepo.GetAwarded();
            var items = new System.Collections.Generic.List<object>();
            foreach (var a in list)
            {
                items.Add(new
                {
                    a.Id,
                    DisplayName = a is Citizen citizen ? citizen.ToString() : ((Collective)a).CollectiveName
                });
            }
            cmbAwarded.ItemsSource = items;

            if (_assignment.Id != 0)
            {
                cmbDecree.SelectedValue = _assignment.DecreeId;
                cmbAward.SelectedValue = _assignment.AwardId;
                cmbAwarded.SelectedValue = _assignment.AwardedId;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                var decree = (Decree)cmbDecree.SelectedItem;
                var award = (Award)cmbAward.SelectedItem;
                dynamic awarded = cmbAwarded.SelectedItem;

                _assignment.DecreeId = decree.Id;
                _assignment.Decree = decree;
                _assignment.AwardId = award.Id;
                _assignment.Award = award;
                _assignment.AwardedId = awarded.Id;

                _assignmentRepo.SaveAwardAssignment(_assignment);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            bool isValid = true;

            if (cmbDecree.SelectedItem == null)
            {
                cmbDecree.BorderBrush = System.Windows.Media.Brushes.Red;
                cmbDecree.BorderThickness = new Thickness(1.5);
                isValid = false;
            }
            else
            {
                cmbDecree.ClearValue(BorderBrushProperty);
                cmbDecree.ClearValue(BorderThicknessProperty);
            }

            if (cmbAward.SelectedItem == null)
            {
                cmbAward.BorderBrush = System.Windows.Media.Brushes.Red;
                cmbAward.BorderThickness = new Thickness(1.5);
                isValid = false;
            }
            else
            {
                cmbAward.ClearValue(BorderBrushProperty);
                cmbAward.ClearValue(BorderThicknessProperty);
            }

            if (cmbAwarded.SelectedItem == null)
            {
                cmbAwarded.BorderBrush = System.Windows.Media.Brushes.Red;
                cmbAwarded.BorderThickness = new Thickness(1.5);
                isValid = false;
            }
            else
            {
                cmbAwarded.ClearValue(BorderBrushProperty);
                cmbAwarded.ClearValue(BorderThicknessProperty);
            }

            if (!isValid)
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля (подсвечены красным).",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return isValid;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddDecree_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditDecreeWindow(_decreeRepo, new AwardReasonRepository());
            if (dialog.ShowDialog() == true)
            {
                var list = _decreeRepo.GetDecrees();
                cmbDecree.ItemsSource = list;
                if (list.Count > 0)
                {
                    var newDecree = list.OrderByDescending(d => d.Id).First();
                    cmbDecree.SelectedValue = newDecree.Id;
                }
            }
        }

        private void AddAward_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SimpleEditWindow("Добавление награды", "Название награды:");
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var newAward = new Award { AwardName = dialog.ResultText };
                    _awardRepo.SaveAward(newAward);

                    var list = _awardRepo.GetAwards();
                    cmbAward.ItemsSource = list;
                    cmbAward.SelectedValue = newAward.Id;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления награды: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddAwarded_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditAwardedWindow(_awardedRepo, _assignmentRepo);
            if (dialog.ShowDialog() == true)
            {
                var list = _awardedRepo.GetAwarded();
                var items = new System.Collections.Generic.List<object>();
                foreach (var a in list)
                {
                    items.Add(new
                    {
                        a.Id,
                        DisplayName = a is Citizen citizen ? citizen.ToString() : ((Collective)a).CollectiveName
                    });
                }
                cmbAwarded.ItemsSource = items;

                if (list.Count > 0)
                {
                    var newAwarded = list.OrderByDescending(a => a.Id).First();
                    cmbAwarded.SelectedValue = newAwarded.Id;
                }
            }
        }
    }
}
