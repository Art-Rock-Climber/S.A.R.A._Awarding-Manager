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
    /// Interaction logic for EditAwardedWindow.xaml
    /// </summary>
    public partial class EditAwardedWindow : Window
    {
        private readonly IAwardedRepository _awardedRepo;
        private readonly IAwardAssignmentRepository _assignmentRepo;
        private readonly Awarded? _editingAwarded;
        private List<Citizen> _citizens = new();
        private List<Collective> _collectives = new();

        public EditAwardedWindow(
            IAwardedRepository awardedRepo,
            IAwardAssignmentRepository assignmentRepo,
            int? awardedId = null)
        {
            InitializeComponent();
            _awardedRepo = awardedRepo;
            _assignmentRepo = assignmentRepo;

            var allAwarded = _awardedRepo.GetAwarded();
            _citizens = allAwarded.OfType<Citizen>().ToList();
            _collectives = allAwarded.OfType<Collective>().ToList();

            if (awardedId.HasValue)
            {
                var list = _awardedRepo.GetAwarded();
                var citizenList = list.OfType<Citizen>().ToList();
                var collectiveList = list.OfType<Collective>().ToList();

                foreach (var col in collectiveList)
                {
                    col.Members = citizenList.Where(c => c.CollectiveId == col.Id).ToList();
                }

                _editingAwarded = list.FirstOrDefault(a => a.Id == awardedId.Value);
                Title = "Редактирование награждаемого";
            }
            else
            {
                _editingAwarded = new Citizen();
                Title = "Добавление награждаемого";
            }

            cmbCollective.ItemsSource = _collectives;
            lstMembers.ItemsSource = _citizens;

            LoadData();

            txtLastName.TextChanged += (s, e) => txtLastName.ClearValue(BorderBrushProperty);
            txtFirstName.TextChanged += (s, e) => txtFirstName.ClearValue(BorderBrushProperty);
            txtPosition.TextChanged += (s, e) => txtPosition.ClearValue(BorderBrushProperty);
            txtCollectiveName.TextChanged += (s, e) => txtCollectiveName.ClearValue(BorderBrushProperty);
        }

        private void LoadData()
        {
            if (_editingAwarded is Citizen citizen)
            {
                cmbType.SelectedIndex = 0;
                txtLastName.Text = citizen.LastName;
                txtFirstName.Text = citizen.FirstName;
                txtMiddleName.Text = citizen.MiddleName;
                txtPosition.Text = citizen.Position;

                cmbCollective.SelectedValue = citizen.CollectiveId;
            }
            else if (_editingAwarded is Collective collective)
            {
                cmbType.SelectedIndex = 1;
                txtCollectiveName.Text = collective.CollectiveName;

                foreach (var member in collective.Members)
                {
                    var item = _citizens.FirstOrDefault(c => c.Id == member.Id);
                    if (item != null)
                    {
                        lstMembers.SelectedItems.Add(item);
                    }
                }
            }
        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbType == null || spCitizen == null || spCollective == null)
                return;

            spCitizen.Visibility = cmbType.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            spCollective.Visibility = cmbType.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                int oldId = _editingAwarded?.Id ?? 0;

                if (cmbType.SelectedIndex == 0) // Гражданин
                {
                    var citizen = _editingAwarded as Citizen ?? new Citizen();

                    citizen.LastName = txtLastName.Text;
                    citizen.FirstName = txtFirstName.Text;
                    citizen.MiddleName = txtMiddleName.Text;
                    citizen.Position = txtPosition.Text;

                    if (cmbCollective.SelectedValue is int collectiveId)
                    {
                        citizen.CollectiveId = collectiveId;
                    }
                    else
                    {
                        citizen.CollectiveId = null;
                    }

                    _awardedRepo.SaveAwarded(citizen);

                    if (oldId > 0 && _editingAwarded is Collective)
                    {
                        var related = _assignmentRepo.GetAwardAssignments().Where(aa => aa.AwardedId == oldId).ToList();
                        foreach (var a in related)
                        {
                            a.AwardedId = citizen.Id;
                            _assignmentRepo.SaveAwardAssignment(a);
                        }
                        _awardedRepo.DeleteAwarded(oldId);
                    }
                }
                else // Коллектив
                {
                    var collective = _editingAwarded as Collective ?? new Collective();

                    collective.CollectiveName = txtCollectiveName.Text;
                    collective.Members.Clear();
                    foreach (Citizen selected in lstMembers.SelectedItems)
                    {
                        collective.Members.Add(selected);
                    }

                    _awardedRepo.SaveAwarded(collective);

                    if (oldId > 0 && _editingAwarded is Citizen)
                    {
                        var related = _assignmentRepo.GetAwardAssignments().Where(aa => aa.AwardedId == oldId).ToList();
                        foreach (var a in related)
                        {
                            a.AwardedId = collective.Id;
                            _assignmentRepo.SaveAwardAssignment(a);
                        }
                        _awardedRepo.DeleteAwarded(oldId);
                    }
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
            if (cmbType.SelectedIndex == 0) // Гражданин
            {
                if (string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    txtLastName.BorderBrush = System.Windows.Media.Brushes.Red;
                    isValid = false;
                }
                else
                {
                    txtLastName.ClearValue(BorderBrushProperty);
                }

                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    txtFirstName.BorderBrush = System.Windows.Media.Brushes.Red;
                    isValid = false;
                }
                else
                {
                    txtFirstName.ClearValue(BorderBrushProperty);
                }

                if (string.IsNullOrWhiteSpace(txtPosition.Text))
                {
                    txtPosition.BorderBrush = System.Windows.Media.Brushes.Red;
                    isValid = false;
                }
                else
                {
                    txtPosition.ClearValue(BorderBrushProperty);
                }
            }
            else // Коллектив
            {
                if (string.IsNullOrWhiteSpace(txtCollectiveName.Text))
                {
                    txtCollectiveName.BorderBrush = System.Windows.Media.Brushes.Red;
                    isValid = false;
                }
                else
                {
                    txtCollectiveName.ClearValue(BorderBrushProperty);
                }
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
    }
}
