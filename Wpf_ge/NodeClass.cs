using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Wpf_ge
{
	[Serializable]
	public class NodeClass
	{
		public List<LineGeometry> EndLines { get; private set; }
		public List<LineGeometry> StartLines { get; private set; }		
		public List<LineGeometry> ArrowLines { get; set; }
		public List<LineGeometry> EndSourceLine { get; set; }		
		public List<string> LinkedNodeName { get; set; }
		//public DataTable NodeDataTable { get; set; }
		public List<string> DataColumns { get; set; }
		public List<string> RowHeaders { get; set; }
		public List<double[]> DataRows { get; set; }
		public string Name;
		public string Label;
		public double X, Y;
		public bool isdirectmode;

		public NodeClass()
		{
			EndLines = new List<LineGeometry>();
			StartLines  = new List<LineGeometry>();
			ArrowLines = new List<LineGeometry>();
			EndSourceLine = new List<LineGeometry>();
			LinkedNodeName = new List<string>();
			//NodeDataTable = new DataTable();
			//NodeDataTable.TableName = Name;
			RowHeaders = new List<string>();
			DataColumns = new List<string>();
			DataRows = new List<double[]>();
			isdirectmode = true;
		}
	}

	


}
