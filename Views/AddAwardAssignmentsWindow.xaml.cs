using sara_coursework.models;
using sara_coursework.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace sara_coursework.Views
{
    public class AwardedItemViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interaction logic for AddAwardAssignmentsWindow.xaml
    /// </summary>
    public partial class AddAwardAssignmentsWindow : Window
    {
        private readonly IAwardAssignmentRepository _assignmentRepo;
        private readonly IAwardRepository _awardRepo;
        private readonly IDecreeRepository _decreeRepo;
        private readonly IAwardedRepository _awardedRepo;

        private List<AwardedItemViewModel> _allAwardedItems = new();
        private HashSet<int> _selectedAwardedIds = new();
        private bool _isUpdatingSelection = false;

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

            lbAwarded.SelectionMode = SelectionMode.Multiple;
            lbAwarded.DisplayMemberPath = "DisplayName";

            InitializeData();

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
        }

        private void InitializeData()
        {
            var list = _awardedRepo.GetAwarded();
            _allAwardedItems = list.Select(a => new AwardedItemViewModel
            {
                Id = a.Id,
                DisplayName = a is Citizen citizen ? citizen.ToString() : ((Collective)a).CollectiveName
            }).ToList();

            ApplyAwardedFilter();

            cmbAward.ItemsSource = _awardRepo.GetAwards();
            cmbAward.DisplayMemberPath = "AwardName";
            cmbDecree.ItemsSource = _decreeRepo.GetDecrees();
            cmbDecree.DisplayMemberPath = "DisplayText";
        }

        private void LbAwarded_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            foreach (AwardedItemViewModel added in e.AddedItems)
            {
                _selectedAwardedIds.Add(added.Id);
            }
            foreach (AwardedItemViewModel removed in e.RemovedItems)
            {
                _selectedAwardedIds.Remove(removed.Id);
            }

            UpdateSelectedCount();

            if (_selectedAwardedIds.Count > 0)
            {
                lbAwarded.ClearValue(BorderBrushProperty);
                lbAwarded.ClearValue(BorderThicknessProperty);
            }
        }

        private void UpdateSelectedCount()
        {
            tbSelectedCount.Text = $"Выбрано: {_selectedAwardedIds.Count}";
        }

        private void TxtSearchAwarded_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyAwardedFilter();
        }

        private void ClearSearchAwarded_Click(object sender, RoutedEventArgs e)
        {
            txtSearchAwarded.Text = string.Empty;
        }

        private void ApplyAwardedFilter()
        {
            string query = txtSearchAwarded.Text.Trim().ToLower();

            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allAwardedItems
                : _allAwardedItems.Where(i => i.DisplayName.ToLower().Contains(query)).ToList();

            _isUpdatingSelection = true;
            lbAwarded.ItemsSource = filtered;

            lbAwarded.SelectedItems.Clear();
            foreach (var item in filtered)
            {
                if (_selectedAwardedIds.Contains(item.Id))
                {
                    lbAwarded.SelectedItems.Add(item);
                }
            }
            _isUpdatingSelection = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;
            try
            {
                int decreeId = GetSelectedDecreeId();
                int awardId = GetSelectedAwardId();

                foreach (int awardedId in _selectedAwardedIds)
                {
                    var assignment = new AwardAssignment
                    {
                        AwardedId = awardedId,
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

        private int GetSelectedDecreeId()
        {
            if (cmbDecree.SelectedValue is int id) return id;
            if (cmbDecree.SelectedItem is Decree d) return d.Id;
            throw new InvalidOperationException("Не выбрано постановление.");
        }

        private int GetSelectedAwardId()
        {
            if (cmbAward.SelectedValue is int id) return id;
            if (cmbAward.SelectedItem is Award a) return a.Id;
            throw new InvalidOperationException("Не выбрана награда.");
        }

        private bool ValidateInput()
        {
            bool isValid = true;

            if (cmbDecree.SelectedItem == null && cmbDecree.SelectedValue == null)
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

            if (cmbAward.SelectedItem == null && cmbAward.SelectedValue == null)
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

            if (_selectedAwardedIds.Count == 0)
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
                _allAwardedItems = list.Select(a => new AwardedItemViewModel
                {
                    Id = a.Id,
                    DisplayName = a is Citizen citizen ? citizen.ToString() : ((Collective)a).CollectiveName
                }).ToList();

                if (list.Count > 0)
                {
                    var newAwarded = list.OrderByDescending(a => a.Id).First();
                    _selectedAwardedIds.Add(newAwarded.Id);
                }

                ApplyAwardedFilter();
                UpdateSelectedCount();
            }
        }
    }
}