using sara_coursework.models;
using sara_coursework.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace sara_coursework.Views
{
    /// <summary>
    /// Interaction logic for AddAwardAssignmentsWindow.xaml
    /// </summary>
    public partial class AddAwardAssignmentsWindow : Window
    {
        private readonly IAwardAssignmentRepository _assignmentRepo;
        private readonly IAwardRepository _awardRepo;
        private readonly IDecreeRepository _decreeRepo;
        private readonly IAwardedRepository _awardedRepo;

        public AddAwardAssignmentsWindow(
            IAwardAssignmentRepository assignmentRepo,
            IAwardRepository awardRepo,
            IDecreeRepository decreeRepo,
            IAwardedRepository awardedRepo)
        {
            InitializeComponent();
            _assignmentRepo = assignmentRepo;
            _awardRepo = awardRepo;
            _decreeRepo = decreeRepo;
            _awardedRepo = awardedRepo;
            Title = "Добавление награждений";
            InitializeMultiSelect();

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
            lbAwarded.SelectionChanged += (s, e) => {
                if (lbAwarded.SelectedItems.Count > 0) {
                    lbAwarded.ClearValue(BorderBrushProperty);
                    lbAwarded.ClearValue(BorderThicknessProperty);
                }
            };
        }

        private void InitializeMultiSelect()
        {
            lbAwarded.SelectionMode = SelectionMode.Multiple;
            lbAwarded.DisplayMemberPath = "DisplayName";

            // Load data through repositories
            var list = _awardedRepo.GetAwarded();
            var items = new List<object>();
            foreach (var a in list)
            {
                items.Add(new
                {
                    a.Id,
                    DisplayName = a is Citizen citizen ? citizen.ToString() : ((Collective)a).CollectiveName
                });
            }
            lbAwarded.ItemsSource = items;

            cmbAward.ItemsSource = _awardRepo.GetAwards();
            cmbAward.DisplayMemberPath = "AwardName";
            cmbDecree.ItemsSource = _decreeRepo.GetDecrees();
            cmbDecree.DisplayMemberPath = "DisplayText";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;
            try
            {
                var decreeId = (int)cmbDecree.SelectedValue;
                var awardId = (int)cmbAward.SelectedValue;

                foreach (dynamic awarded in lbAwarded.SelectedItems)
                {
                    var assignment = new AwardAssignment
                    {
                        AwardedId = awarded.Id,
                        AwardId = awardId,
                        DecreeId = decreeId
                    };
                    _assignmentRepo.SaveAwardAssignment(assignment);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

            if (lbAwarded.SelectedItems.Count == 0)
            {
                lbAwarded.BorderBrush = System.Windows.Media.Brushes.Red;
                lbAwarded.BorderThickness = new Thickness(1.5);
                isValid = false;
            }
            else
            {
                lbAwarded.ClearValue(BorderBrushProperty);
                lbAwarded.ClearValue(BorderThicknessProperty);
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

        private void LbAwarded_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbSelectedCount.Text = $"Выбрано: {lbAwarded.SelectedItems.Count}";
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
                var items = new List<object>();
                foreach (var a in list)
                {
                    items.Add(new
                    {
                        a.Id,
                        DisplayName = a is Citizen citizen ? citizen.ToString() : ((Collective)a).CollectiveName
                    });
                }
                lbAwarded.ItemsSource = items;

                if (list.Count > 0)
                {
                    var newAwarded = list.OrderByDescending(a => a.Id).First();
                    dynamic matchingItem = items.FirstOrDefault(i => ((dynamic)i).Id == newAwarded.Id);
                    if (matchingItem != null)
                    {
                        lbAwarded.SelectedItems.Add(matchingItem);
                    }
                }
            }
        }
    }
}