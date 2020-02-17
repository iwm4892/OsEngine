﻿/*
 * Your rights to use code governed by this license http://o-s-a.net/doc/license_simple_engine.pdf
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OsEngine.Language;

namespace OsEngine.Entity
{
    /// <summary>
    /// Interaction logic for ParemetrsUi.xaml
    /// Логика взаимодействия для ParemetrsUi.xaml
    /// </summary>
    public partial class ParemetrsUi
    {

        private List<IIStrategyParameter> _parameters;

        public ParemetrsUi(List<IIStrategyParameter> parameters)
        {
            InitializeComponent();
            _parameters = parameters;

            CreateTable();
            PaintTable();

            ButtonAccept.Content = OsLocalization.Entity.ButtonAccept;
            Title = OsLocalization.Entity.TitleParametersUi;
        }

        private DataGridView _grid;

        private void CreateTable()
        {
            _grid = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
                DataGridViewAutoSizeRowsMode.None);

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
            cell0.Style = _grid.DefaultCellStyle;

            DataGridViewColumn column0 = new DataGridViewColumn();
            column0.CellTemplate = cell0;
            column0.HeaderText = OsLocalization.Entity.ParametersColumn1;
            column0.ReadOnly = true;
            column0.Width = 150;

            _grid.Columns.Add(column0);

            DataGridViewColumn column1 = new DataGridViewColumn();
            column1.CellTemplate = cell0;
            column1.HeaderText = OsLocalization.Entity.ParametersColumn2;
            column1.ReadOnly = false;
            column1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column1);

            _grid.Rows.Add(null, null);

            HostParametrs.Child = _grid;
        }

        private void PaintTable()
        {
            _grid.Rows.Clear();

            for (int i = 0; i < _parameters.Count; i++)
            {
                DataGridViewRow row = new DataGridViewRow();

                row.Cells.Add(new DataGridViewTextBoxCell());
                row.Cells[0].Value = _parameters[i].Name;

                if (_parameters[i].Type == StrategyParameterType.Bool)
                {
                    DataGridViewComboBoxCell cell = new DataGridViewComboBoxCell();

                    cell.Items.Add("False");
                    cell.Items.Add("True");
                    cell.Value = ((StrategyParameterBool)_parameters[i]).ValueBool.ToString();
                    row.Cells.Add(cell);
                }
                else if (_parameters[i].Type == StrategyParameterType.String)
                {
                    DataGridViewComboBoxCell cell = new DataGridViewComboBoxCell();

                    StrategyParameterString param = (StrategyParameterString)_parameters[i];

                    for (int i2 = 0; i2 < param.ValuesString.Count; i2++)
                    {
                        cell.Items.Add(param.ValuesString[i2]);
                    }
                    cell.Value = param.ValueString;
                    row.Cells.Add(cell);
                }
                else if (_parameters[i].Type == StrategyParameterType.Int)
                {
                    DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();

                    StrategyParameterInt param = (StrategyParameterInt)_parameters[i];

                    cell.Value = param.ValueInt.ToString();
                    row.Cells.Add(cell);
                }
                else if (_parameters[i].Type == StrategyParameterType.Decimal)
                {
                    DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();

                    StrategyParameterDecimal param = (StrategyParameterDecimal)_parameters[i];

                    cell.Value = param.ValueDecimal.ToString();
                    row.Cells.Add(cell);
                }



                _grid.Rows.Add(row);
            }
        }

        private void ButtonAccept_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int i = 0; i < _parameters.Count; i++)
            {
                try
                {
                    if (_parameters[i].Type == StrategyParameterType.String)
                    {
                        ((StrategyParameterString)_parameters[i]).ValueString = _grid.Rows[i].Cells[1].EditedFormattedValue.ToString();
                    }
                    else if (_parameters[i].Type == StrategyParameterType.Int)
                    {
                        ((StrategyParameterInt)_parameters[i]).ValueInt = Convert.ToInt32(_grid.Rows[i].Cells[1].EditedFormattedValue.ToString());
                    }
                    else if (_parameters[i].Type == StrategyParameterType.Bool)
                    {
                        ((StrategyParameterBool)_parameters[i]).ValueBool = Convert.ToBoolean(_grid.Rows[i].Cells[1].EditedFormattedValue.ToString());
                    }
                    else if (_parameters[i].Type == StrategyParameterType.Decimal)
                    {
                        ((StrategyParameterDecimal)_parameters[i]).ValueDecimal = _grid.Rows[i].Cells[1].EditedFormattedValue.ToString().ToDecimal();
                    }
                }
                catch
                {
                    MessageBox.Show("Error. One of field have note valid param");
                    return;
                }

            }

            Close();
        }
    }
}
