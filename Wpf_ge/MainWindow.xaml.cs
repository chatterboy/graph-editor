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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;

namespace Wpf_ge
{
	public class Figaro
	{
		private string ImportInstruction = "import com.cra.figaro.algorithm.factored._\r\n"+
											"import com.cra.figaro.language._\r\n"+
											"import com.cra.figaro.library.compound._\r\n";

		private string objectIns = "object ";
		private string maincode = "def main(args: Array[String]) {\r\n\t}";

		//private int Numofblock = 0;

		private string MakeCode(List<Node> createNode)
		{
			string FigaroLanguage = ImportInstruction;
			FigaroLanguage += "\r\n" + objectIns + char.ToUpper(createNode[0].Label[0]) + createNode[0].Label.Substring(1) + "{\r\n\t";						
			FigaroLanguage += "Universe.createNew()\r\n\t";

			for (int i = 0; i < createNode.Count; i++)
			{
				// Rows count <= 1
				if (createNode[i].ProbabilityTable.Rows.Count <= 1)
				{
					FigaroLanguage += "private val " + createNode[i].Label + " = Select(";
					for(int j=0;j< createNode[i].ProbabilityTable.Columns.Count;j++){
						FigaroLanguage += createNode[i].ProbabilityTable.Rows[0].ItemArray[j].ToString() + " -> " + createNode[i].ProbabilityTable.Columns[j].ColumnName;
						if (j + 1 < createNode[i].ProbabilityTable.Columns.Count) FigaroLanguage += ", ";
					}
					FigaroLanguage += ")\r\n\t";
				}
				else
				{
					FigaroLanguage += "private val " + createNode[i].Label + " = CPD(";
					for (int j = 0; j < createNode[i].LinkedSourceNode.Count; j++)
					{
						FigaroLanguage += createNode[i].LinkedSourceNode[j].Label + ", ";					
					}
					FigaroLanguage += "\r\n\t\t";
					for (int j = 0; j < createNode[i].ProbabilityTable.Rows.Count; j++)
					{
						FigaroLanguage += "(" + createNode[i].RowHeaders[j] + ") -> Select(";
						for (int p = 0; p < createNode[i].ProbabilityTable.Columns.Count; p++)
						{
							FigaroLanguage += createNode[i].ProbabilityTable.Rows[j].ItemArray[p].ToString() + " -> " + createNode[i].ProbabilityTable.Columns[p].ColumnName;
							if (p + 1 < createNode[i].ProbabilityTable.Columns.Count) FigaroLanguage += ", ";
						}
						FigaroLanguage += ")";
						if (j + 1 < createNode[i].ProbabilityTable.Rows.Count)
							FigaroLanguage += ",\r\n\t\t";
					}
					FigaroLanguage += ")\r\n\t";
				}				
			}
			FigaroLanguage += "\r\n\t" + maincode;
			FigaroLanguage += "\r\n}";
			return FigaroLanguage;
		}

		public void Export(List<Node> list)
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
			dlg.FileName = "";
			dlg.DefaultExt = ".scala";
			dlg.Filter = "scala Files (.scala)|*.scala";

			Nullable<bool> result = dlg.ShowDialog();
			if (result == false) return;

			using (StreamWriter file = new StreamWriter(dlg.FileName))
			{
				file.WriteLine(MakeCode(list));
			}
		}



	}

	public partial class MainWindow : Window
	{
		bool isAddNewAction = false;
		bool isAddNewLink = false;
		// flag that indicates that the link drawing with a mouse started
		bool isLinkStarted = false;
		// flag that indicates that Directed or Undirected Mode 
		bool isdirectmode = true;
		// Line drawn by the mouse before connection established
		LineGeometry link;
		// variable to hold the thumb drawing started from
		Node linkedThumb;
		Node CurrentClickedNode=null;
		private List<Node> createdNode;


		public MainWindow()
		{
			// init createNode for storing maked all nodes
			createdNode = new List<Node>();
			
			//System.Data.datatable			
			InitializeComponent();				
		}

		#region EventHandlers
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.createdNode.Add(Node1);
			this.createdNode.Add(Node2);
			this.createdNode.Add(Node3);
			this.createdNode.Add(Node4);
			this.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Window1_PreviewMouseLeftButtonDown);
			this.PreviewMouseMove += new MouseEventHandler(Window1_PreviewMouseMove);
			this.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(Window1_PreviewMouseLeftButtonUp);
		}		

		private void onDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			// Exit dragging operation during adding new link
			if (isAddNewLink) return;

			Node thumb = e.Source as Node;

			Canvas.SetLeft(thumb, Canvas.GetLeft(thumb) + e.HorizontalChange);
			Canvas.SetTop(thumb, Canvas.GetTop(thumb) + e.VerticalChange);

			// Update links' layouts for active Node
			thumb.UpdateLinks();
			for (int i = 0; i < thumb.LinkedNode.Count; i++)
				thumb.LinkedNode[i].UpdateArrows();
		}

		private void Add_Node(object sender, RoutedEventArgs e)
		{
			isAddNewAction = true;
			Mouse.OverrideCursor = Cursors.Hand;
			canvas.ContextMenu.IsEnabled = false;
		}

		private void Add_Edge(object sender, RoutedEventArgs e)
		{
			isAddNewLink = true;
			Mouse.OverrideCursor = Cursors.Cross;
			//canvas.ContextMenu.IsEnabled = false;
		}

		private void Edit_Dialog(object sender, RoutedEventArgs e)
		{
			if (CurrentClickedNode != null)
			{
				EditDialog edit = new EditDialog();				
				// Update Rows of DataTable
				this.CurrentClickedNode.MakeRows();				
				// Show Edit Dailog
				edit.ShowDialog(CurrentClickedNode);
				// Update Templates
				this.CurrentClickedNode.OnApplyTemplate();
				CurrentClickedNode = null;
			}
		}

		void Window1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (isAddNewAction)
			{
				System.Windows.Point npoint = new System.Windows.Point(e.GetPosition(this).X - 20, e.GetPosition(this).Y - 45);
				Node newThumb = new Node(
					Application.Current.Resources["CustomEllipse"] as ControlTemplate,
					"Node",
					"/Images/Ellipse.png",															
					npoint,
					Add_Edge,
					Edit_Dialog,
					Delete_Node,
					onDragDelta);

				// Event for Right Mouse Clicked to save current Node
				newThumb.ContextMenuOpening += Node_ContextMenuOpening;
				// For Saving all Nodes created to serialize
				createdNode.Add(newThumb);
	
				
				// Put newly created thumb on the canvas
				canvas.Children.Add(newThumb);				
				// resume common layout for application
				isAddNewAction = false;
				Mouse.OverrideCursor = null;
				canvas.ContextMenu.IsEnabled = true;				
				e.Handled = true;
			}

			// Is adding new link and a Node object is clicked...
			if (isAddNewLink && e.Source.GetType() == typeof(Node))
			{
				if (!isLinkStarted)
				{
					if (link == null || link.EndPoint != link.StartPoint)
					{
						// arrow line drawing in here for mouse version


						System.Windows.Point position = e.GetPosition(this);
						position.Y -= 25;
						link = new LineGeometry(position, position);
						connectors.Children.Add(link);
						isLinkStarted = true;
						linkedThumb = e.Source as Node;
						e.Handled = true;
					}
				}
			}			
		}

		// Handles the mouse move event when dragging/drawing the new connection link
		void Window1_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (isAddNewLink && isLinkStarted)
			{
				System.Windows.Point position = e.GetPosition(this);
				position.Y -= 25;
				// Set the new link end point to current mouse position
				link.EndPoint = position;
				e.Handled = true;
			}
		}

		// Handles the mouse up event applying the new connection link or resetting it
		void Window1_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// If "Add Edge" mode enabled and line drawing started
			if (isAddNewLink && isLinkStarted)
			{
				// declare the linking state
				bool linked = false;
				// released the button on Node object
				if (e.Source.GetType() == typeof(Node))
				{
					Node targetThumb = e.Source as Node;
					// define the final endpoint of line
					link.EndPoint = e.GetPosition(this);
					// if any line was drawn (avoid just clicking on the thumb)					
					if (link.EndPoint != link.StartPoint && linkedThumb != targetThumb && !linkedThumb.IsLinked(targetThumb))
					{
						// establish connection
						linkedThumb.LinkTo(targetThumb, link);
						// set linked state to true
						linked = true;

						#region arrowline
						if (this.isdirectmode)
						{
							Tuple<LineGeometry, LineGeometry> arrows = targetThumb.MakeArrow(linkedThumb);
							connectors.Children.Add(arrows.Item1);
							connectors.Children.Add(arrows.Item2);
						}
						#endregion
					}
				}
				// if we didn't manage to approve the linking state				
				if (!linked)
				{
					// remove line from the canvas
					connectors.Children.Remove(link);
					// clear the link variable
					link = null;
				}

				// exit link drawing mode
				isLinkStarted = isAddNewLink = false;
				// configure GUI				
				canvas.ContextMenu.IsEnabled = true;

				Mouse.OverrideCursor = null;
				e.Handled = true;
			}			
		}
		#endregion

		#region SerializeData
		public void SerializeObject<T>(T serializableObject, string fileName)
		{
			if (serializableObject == null) { return; }

			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
				using (MemoryStream stream = new MemoryStream())
				{
					serializer.Serialize(stream, serializableObject);
					stream.Position = 0;
					xmlDocument.Load(stream);
					xmlDocument.Save(fileName);
					stream.Close();
				}
			}
			catch (Exception ex)
			{
				//Log exception here
			}
		}

		public T DeSerializeObject<T>(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) { return default(T); }

			T objectOut = default(T);

			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(fileName);
				string xmlString = xmlDocument.OuterXml;

				using (StringReader read = new StringReader(xmlString))
				{
					Type outType = typeof(T);

					XmlSerializer serializer = new XmlSerializer(outType);
					using (XmlReader reader = new XmlTextReader(read))
					{
						objectOut = (T)serializer.Deserialize(reader);
						reader.Close();
					}

					read.Close();
				}
			}
			catch (Exception ex)
			{
				//Log exception here
			}

			return objectOut;
		}

		private void LoadUIElementFromXml(List<NodeClass> Nodedata)
		{
			// Clear Current Data
			// if user wants to Save Current State,,, ??
			NewForm(null, null);


			this.isdirectmode = Nodedata[0].isdirectmode;

			// Get all Nodes and Lines Data using NodeData
			for (int i = 0; i < Nodedata.Count; i++)
			{
				//System.Windows.Point npoint = new System.Windows.Point(Nodedata[i].X - 20, Nodedata[i].Y - 45);
				System.Windows.Point npoint = new System.Windows.Point(Nodedata[i].X, Nodedata[i].Y);
				Node newNode = new Node(
					Application.Current.Resources["CustomEllipse"] as ControlTemplate,
					Nodedata[i].Label,
					"/Images/Ellipse.png",
					npoint,
					Add_Edge,
					Edit_Dialog,
					Delete_Node,
					onDragDelta);
				// Event for Right Mouse Clicked to save current Node
				newNode.ContextMenuOpening += Node_ContextMenuOpening;				
				newNode.Name = Nodedata[i].Name;
				

				//@@ Get All DataTable 				
				for (int j = 0; j < Nodedata[i].RowHeaders.Count; j++)
					newNode.RowHeaders.Add(Nodedata[i].RowHeaders[j]);

				for (int j = 0; j < Nodedata[i].DataColumns.Count; j++)
					newNode.ProbabilityTable.Columns.Add(new DataColumn(Nodedata[i].DataColumns[j], typeof(string)));
				newNode.ProbabilityTable.Rows.Clear();
				for (int j = 0; j < Nodedata[i].DataRows.Count; j++)
				{
					DataRow row = newNode.ProbabilityTable.NewRow();
					for (int p = 0; p < Nodedata[i].DataRows[j].Count();p++)
						row[p] = Nodedata[i].DataRows[j][p];
					newNode.ProbabilityTable.Rows.Add(row);					
				}

				// Add newNode to canvas
				canvas.Children.Add(newNode);
				createdNode.Add(newNode);
			}


			
			// Get LinkedNode Data typeof Node class
			for (int i = 0; i < Nodedata.Count; i++)
			{
				for (int p = 0; p < Nodedata[i].LinkedNodeName.Count; p++)
				{
					for (int j = 0; j < createdNode.Count; j++)
					{
						if (Nodedata[i].LinkedNodeName[p] == createdNode[j].Name)
						{
							createdNode[i].LinkedNode.Add(createdNode[j]);
						}
					}
				}
			}

			for (int i = 0; i < createdNode.Count; i++)
			{
				//Node[] linkedNodelist = new Node[createdNode[i].LinkedNode.Count];
				//createdNode[i].LinkedNode.CopyTo(linkedNodelist);
				//createdNode[i].LinkedNode.Clear();
				//it isn't Reference value
				for (int j = 0; j < Nodedata[i].StartLines.Count; j++)
				{
					createdNode[i].LinkTo(createdNode[i].LinkedNode[j], Nodedata[i].StartLines[j]);
					connectors.Children.Add(Nodedata[i].StartLines[j]);

					#region arrowline
					if (this.isdirectmode)
					{
						Tuple<LineGeometry, LineGeometry> arrows = createdNode[i].LinkedNode[j].MakeArrow(createdNode[i]);
						connectors.Children.Add(arrows.Item1);
						connectors.Children.Add(arrows.Item2);
					}
					#endregion											
				}					
			}
			



		}

		private NodeClass MakeNodeData(Node savenode)
		{
			NodeClass tmp = new NodeClass();

			tmp.isdirectmode = this.isdirectmode;

			tmp.Name = savenode.Name;
			tmp.Label = savenode.Label;
			tmp.X = Canvas.GetLeft(savenode);
			tmp.Y = Canvas.GetTop(savenode);

			for (int i = 0; i < savenode.RowHeaders.Count; i++)
				tmp.RowHeaders.Add(savenode.RowHeaders[i]);

			for (int i = 0; i < savenode.ProbabilityTable.Columns.Count; i++)
				tmp.DataColumns.Add(savenode.ProbabilityTable.Columns[i].ColumnName);

			for (int i = 0; i < savenode.ProbabilityTable.Rows.Count; i++)
			{
				int itemlength = savenode.ProbabilityTable.Rows[i].ItemArray.Count();
				double[] rows = new double[itemlength];
				for (int j = 0; j < itemlength; j++)
					rows[j] = Convert.ToDouble(savenode.ProbabilityTable.Rows[i].ItemArray[j]);
				//rows = Array.ConvertAll<object, double>(savenode.ProbabilityTable.Rows[i].ItemArray, x => (double)x); //doesn't work
				tmp.DataRows.Add(rows);
			}			

			for (int i = 0; i < savenode.EndLines.Count; i++)
				tmp.EndLines.Add(savenode.EndLines[i]);

			for (int i = 0; i < savenode.StartLines.Count; i++)
				tmp.StartLines.Add(savenode.StartLines[i]);

			for (int i = 0; i < savenode.ArrowLines.Count; i++)
				tmp.ArrowLines.Add(savenode.ArrowLines[i]);

			for (int i = 0; i < savenode.EndSourceLine.Count; i++)
				tmp.EndSourceLine.Add(savenode.EndSourceLine[i]);

			for (int i = 0; i < savenode.LinkedNode.Count; i++)
				tmp.LinkedNodeName.Add(savenode.LinkedNode[i].Name);

			return tmp;
		}

		private void saveasxml(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
			dlg.FileName = "";
			dlg.DefaultExt = ".xml";
			dlg.Filter = "Xml Files (.xml)|*.xml";
			Nullable<bool> result = dlg.ShowDialog();
			if (result == false) return;
			
			// For Serializing Node Data
			List<NodeClass> NodeData = new List<NodeClass>();
			for (int i = 0; i < createdNode.Count; i++)
				NodeData.Add(MakeNodeData(createdNode[i]));

			SerializeObject<List<NodeClass>>(NodeData, dlg.FileName);
		}

		private void openxml(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.FileName = "";
			dlg.DefaultExt = ".xml";
			dlg.Filter = "Xml Files (.xml)|*.xml";

			Nullable<bool> result = dlg.ShowDialog();
			if (result == false) return;
			// For Serializing Node Data
			List<NodeClass> NodeData = new List<NodeClass>();

			// Get All Stored Node Data 
			NodeData = DeSerializeObject<List<NodeClass>>(dlg.FileName);
			
			// Update into Windows
			LoadUIElementFromXml(NodeData);
		}
		#endregion

		#region otherMenuitems
		private void NewForm(object sender, RoutedEventArgs e)
		{
			// Clear All LineGeometry
			connectors.Children.Clear();			

			// Clear All Nodes attached in canvas
			canvas.Children.Clear();

			// Add the connectors to canvas
			System.Windows.Shapes.Path pathdata = new System.Windows.Shapes.Path();
			pathdata.Stroke = System.Windows.Media.Brushes.Black;
			pathdata.StrokeThickness = 1;
			pathdata.Data = connectors;
			canvas.Children.Add(pathdata);

			// Clear Maked Nodes
			createdNode.Clear();
			//GC.Collect();

			// UI Update  (updated) Do not need
			//canvas.UpdateLayout();
			//canvas.UpdateDefaultStyle();


			// The number of maked nodes initialized
			Node.NodeNum = 1;

			// Init All member variable 
			isAddNewAction = false;
			isAddNewLink = false;
			isLinkStarted = false;
			link = null;
			linkedThumb = null;
			CurrentClickedNode = null;
			isdirectmode = true;
		}

		private void saveasimagedialog(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
			dlg.FileName = "";
			dlg.DefaultExt = ".png";
			dlg.Filter = "Images (.png)|*.png";
			Nullable<bool> result = dlg.ShowDialog();
			if (result == false) return;
			String filename = dlg.FileName;
			RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
					(int)mywindow.Width,
					(int)mywindow.ActualHeight,
					96d,
					96d,
					PixelFormats.Pbgra32);
			// needed otherwise the image output is black
			canvas.Measure(new System.Windows.Size((int)mywindow.ActualWidth, (int)mywindow.ActualHeight));
			canvas.Arrange(new Rect(new System.Windows.Size((int)mywindow.Width, (int)mywindow.ActualHeight)));
			renderBitmap.Render(canvas);

			//JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			PngBitmapEncoder encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

			using (FileStream file = File.Create(filename))
			{
				encoder.Save(file);
			}			
		}

		private void ExitWindow(object sender, RoutedEventArgs e)
		{
			// if user needs to save currents state...
			mywindow.Close();
		}

		#endregion

		private void Node_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			this.CurrentClickedNode = sender as Node;		
		}

		private void Delete_Node(object sender, RoutedEventArgs e)
		{
			// 현재 삭제할 노드의 내부 데이터는 삭제할 필요 없고, connector에있는 내용들만 삭제하면 됌
			// 또한 xml 저장시 저장할 내용이 사실 많이 필요 없고, linkednode, startline 정도만 있어도 복구 가능
			Node node = new Node();
			node = this.CurrentClickedNode;
			int length = node.StartLines.Count;
			for (int i = 0; i < length;i++)			
				connectors.Children.Remove(node.StartLines[i]);				
			
			for (int i = 0; i < length; i++)
				node.StartLines.RemoveAt(0);

			length =  node.EndLines.Count;
			for (int i = 0; i < length; i++)
				connectors.Children.Remove(node.EndLines[i]);

			for (int i = 0; i < length; i++)
				node.EndLines.RemoveAt(0);

			
			// Arrow
			for (int i = 0; i < node.LinkedNode.Count; i++)
			{
				int dump = 0;
				for (int j = 0; j < node.LinkedNode[i].LinkedSourceNode.Count; j++)
				{
					if (node.LinkedNode[i].LinkedSourceNode[j] == node)
					{
						if (this.isdirectmode)
						{
							connectors.Children.Remove(node.LinkedNode[i].ArrowLines[j + 1 - dump]);
							node.LinkedNode[i].ArrowLines.RemoveAt(j + 1 - dump);
							connectors.Children.Remove(node.LinkedNode[i].ArrowLines[j - dump]);
							node.LinkedNode[i].ArrowLines.RemoveAt(j - dump);
							dump += 2;
						}
						node.LinkedNode[i].EndLines.RemoveAt(j);
						node.LinkedNode[i].EndSourceLine.RemoveAt(j);
						
					}					
				}				
			}
			for (int i = 0; i < node.LinkedNode.Count; i++)
				node.LinkedNode[i].LinkedSourceNode.Remove(node);

			// endline
			for (int i = 0; i < node.LinkedSourceNode.Count; i++)
			{
				int leng = node.LinkedSourceNode[i].LinkedNode.Count;
				int dump = 0;
				for (int j = 0; j < leng; j++)
				{
					if (node.LinkedSourceNode[i].LinkedNode[j] == node)
					{
						node.LinkedSourceNode[i].StartLines.RemoveAt(j - dump);
						dump++;
					}
				}
			}
			for (int i = 0; i < node.LinkedSourceNode.Count; i++)
				node.LinkedSourceNode[i].LinkedNode.Remove(node);

			length = node.ArrowLines.Count;
			for (int i = 0; i < length; i++)
				connectors.Children.Remove(node.ArrowLines[i]);
			for (int i = 0; i < length; i++)
				node.ArrowLines.RemoveAt(0);

			canvas.Children.Remove(node);
			createdNode.Remove(node);			
			node = null;

			// For erasing some UI Error
			for (int i = 0; i < createdNode.Count; i++)
				createdNode[i].UpdateLinks();
		}

		private void IsDirectMode(object sender, RoutedEventArgs e)
		{
			this.isdirectmode = !this.isdirectmode;
		}

		private void ExportToFigaro(object sender, RoutedEventArgs e)
		{
			Figaro fi = new Figaro();
			fi.Export(this.createdNode);
		}



	}
}
