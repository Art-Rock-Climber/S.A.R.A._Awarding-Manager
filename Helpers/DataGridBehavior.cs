using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace sara_coursework.Helpers
{
    public static class DataGridBehavior
    {
        public static readonly DependencyProperty ShowRowNumbersProperty =
            DependencyProperty.RegisterAttached(
                "ShowRowNumbers",
                typeof(bool),
                typeof(DataGridBehavior),
                new PropertyMetadata(false, OnShowRowNumbersChanged));
        public static bool GetShowRowNumbers(DependencyObject obj) => (bool)obj.GetValue(ShowRowNumbersProperty);
        public static void SetShowRowNumbers(DependencyObject obj, bool value) => obj.SetValue(ShowRowNumbersProperty, value);
        private static void OnShowRowNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.LoadingRow += DataGrid_LoadingRow;
                }
                else
                {
                    dataGrid.LoadingRow -= DataGrid_LoadingRow;
                }
            }
        }
        private static void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Устанавливает номер строки (индекс + 1) в качестве заголовка строки (Row Header)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }

}
