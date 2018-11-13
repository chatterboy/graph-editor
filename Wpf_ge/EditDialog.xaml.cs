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
using System.Data;
using System.Security;

namespace Wpf_ge
{
	public partial class EditDialog : Window
	{
		private Node EditingNode = null;
		public DataTable EditDatatable;


		public EditDialog()
		{
			InitializeComponent();		
		}
		
		private void Grid1_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			// current node					
			var id = e.Row.GetIndex();
			try { 
				e.Row.Header = this.EditingNode.RowHeaders[id];
			}
			catch (Exception ex)
			{
				e.Row.Header = " ";
			}
		}

		[SecurityCritical]
		public bool? ShowDialog(Node node)
		{
			this.EditingNode = node;
			this.TextBox_name.Text = this.EditingNode.Label;
			this.EditDatatable = new DataTable();
			this.EditDatatable = this.EditingNode.ProbabilityTable.Copy();			
			datagrid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = this.EditDatatable });			
			return this.ShowDialog();
		}

		#region btnClickHandler
		private void check_btn_Click(object sender, RoutedEventArgs e)
		{
			if (this.EditingNode != null)
			{
				this.EditingNode.Label = this.TextBox_name.Text;
				this.EditingNode.ProbabilityTable = this.EditDatatable;
			}
			this.Close();
		}

		private void Cancel_btn_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void AddColumn_btn_Click(object sender, RoutedEventArgs e)
		{						
			// Add Column Data
			this.EditDatatable.Columns.Add(new DataColumn(this.TextBox_name.Text.Substring(0,2) + this.EditDatatable.Columns.Count, typeof(string)));			
			
			// Refresh DataGrid Items
			var RefreshSource =this.datagrid.ItemsSource;
			this.datagrid.ItemsSource = null;
			this.datagrid.ItemsSource = RefreshSource;
		}
		#endregion

		// verify double data type 
		private void datagrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{				
			try{
				Convert.ToDouble((e.Column.GetCellContent(e.Row) as TextBox).Text);
			}catch(Exception ex){
				MessageBox.Show("The data entered is not a valid. Put double type data in");
				(e.Column.GetCellContent(e.Row) as TextBox).Text = null;
			}			
		}


	}
}
