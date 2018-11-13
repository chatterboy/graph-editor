using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wpf_ge
{	
	public class Node : Thumb
	{
		public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(Node), new UIPropertyMetadata(""));
		public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(string), typeof(Node), new UIPropertyMetadata(""));
		public List<LineGeometry> EndLines { get; private set; }
		public List<LineGeometry> StartLines { get; private set; }
		// To Directed Mode
		public List<LineGeometry> ArrowLines { get; set; }
		// Endline needs the source Point
		public List<LineGeometry> EndSourceLine { get; set; }
		// Save Linked Node because UpdateLinks method changes only Draged Node's Arrows.
		public List<Node> LinkedNode { get; set; }
		public List<Node> LinkedSourceNode { get; set; }
		// Save Datatable in each nodes for using Probabilitical Model
		public DataTable ProbabilityTable { get; set; }
		public List<string> RowHeaders;

		// Only use in Node Class	ContextMenu contextmenu;		
		private double radius = 20;
		private double ArrowTheta = 30, ArrowLength=8;


		public static bool isdirectmode = true;
		// The number of Create Node
		public static int NodeNum = 1;

		// This property will hanlde the content of the textblock element taken from control template
		public string Label
		{
			get { return (string)GetValue(LabelProperty); }
			set { SetValue(LabelProperty, value); }
		}

		// This property will handle the content of the image element taken from control template
		public string ImageSource
		{
			get { return (string)GetValue(ImageSourceProperty); }
			set { SetValue(ImageSourceProperty, value); }
		}

		#region Constructors
		public Node()
			: base()
		{			
			this.Name = "Node" + NodeNum++;
			this.StartLines = new List<LineGeometry>();
			this.EndLines = new List<LineGeometry>();
			this.ArrowLines = new List<LineGeometry>();
			this.EndSourceLine = new List<LineGeometry>();
			this.LinkedNode = new List<Node>();
			this.LinkedSourceNode = new List<Node>();
			this.ProbabilityTable = new DataTable();
			this.ProbabilityTable.Rows.Add(this.ProbabilityTable.NewRow());
			this.ProbabilityTable.TableName = this.Label;
			this.RowHeaders = new List<string>();
		}

		public Node(ControlTemplate template, string Label, string imageSource, Point position) 
			: this()
		{
			this.Template = template;
			this.Label = (Label != null) ? Label : string.Empty;
			this.ImageSource = (imageSource != null) ? imageSource : string.Empty;
			this.SetPosition(position);
		}

		public Node(ControlTemplate template, string Label, string imageSource, Point position,
			RoutedEventHandler AddEdge, RoutedEventHandler Edit, RoutedEventHandler Delete_Node, DragDeltaEventHandler dragDelta)
			: this(template, Label, imageSource, position)
		{						
			ContextMenu contextmenu = new ContextMenu();
			MenuItem item1 = new MenuItem(), item2 = new MenuItem(), item3 = new MenuItem();
			item1.Click += AddEdge;
			item1.Header = "New Edge";
			item2.Click += Edit;
			item2.Header = "Edit";
			item3.Click += Delete_Node;
			item3.Header = "Delete";

			contextmenu.Items.Add(item1);
			contextmenu.Items.Add(item2);
			contextmenu.Items.Add(item3);

			this.ContextMenu = contextmenu;
			this.DragDelta += dragDelta;
		}
		
		#endregion

		public void SetPosition(Point value)
		{
			Canvas.SetLeft(this, value.X);
			Canvas.SetTop(this, value.Y);
		}

		// move Edge from center of Circle to Border
		private Tuple<Point, Point> EdgeIntoBorder(Point Source, Point Target)
		{
			Point SourcePoint = new Point();
			Point TargetPoint = new Point();
			SourcePoint = Source;
			TargetPoint = Target;

			double rad = Math.PI / 180;
			double sx = Source.X, sy = Source.Y;
			double dx = Target.X, dy = Target.Y;
			// Distance of X
			double Xdistance = Math.Abs(sx - dx);
			// Distance of Y
			double Ydistance = Math.Abs(sy - dy);
			// Get Length from Startpoint to Endpoint
			double Length = Math.Sqrt((Xdistance * Xdistance) + (Ydistance * Ydistance));			
			// Get Theta based X-cordinate by using Acos 
			double theta = Math.Acos(Xdistance / Length) / rad;
			// X,Y Distance that Edge Point should be move to Border
			double XDiff = (radius * Xdistance) / Length;
			double YDiff = (radius * Ydistance) / Length;

			// if StartNode is in Left side
			if (sx <= dx)
			{
				// if StartNode's y is above Endnode's y
				// 2-coordinate
				if (sy <= dy)
				{
					SourcePoint = new Point(Source.X + XDiff, Source.Y + YDiff);
					TargetPoint = new Point(Target.X - XDiff, Target.Y - YDiff);
				}
				// 3-coordinate
				else
				{
					SourcePoint = new Point(Source.X + XDiff, Source.Y - YDiff);
					TargetPoint = new Point(Target.X - XDiff, Target.Y + YDiff);
				}
			}
			// StartNode in Right side
			else
			{
				// if StartNode's y is above Endnode's y
				// 1-coordinate
				if (sy <= dy)
				{
					SourcePoint = new Point(Source.X - XDiff, Source.Y + YDiff);
					TargetPoint = new Point(Target.X + XDiff, Target.Y - YDiff);
				}
				// 4-coordinate
				else
				{
					SourcePoint = new Point(Source.X - XDiff, Source.Y - YDiff);
					TargetPoint = new Point(Target.X + XDiff, Target.Y + YDiff);
				}
			}

			return Tuple.Create(SourcePoint, TargetPoint);
		}

		#region Linking logic
		// Arrow1 is always Left Line of Main Line, and 2 is right
		// EndPoint is TargetNode's Position to be attached Arrow Lines		
		private void ArrowEndPoint(LineGeometry Arrow1, LineGeometry Arrow2, Point StartPoint, Point EndPoint)
		{			
			double rad = Math.PI / 180;
			double sx = StartPoint.X, sy = StartPoint.Y;
			double dx = EndPoint.X, dy = EndPoint.Y;
			// Distance of X
			double Xdistance = Math.Abs(sx - dx);
			// Distance of Y
			double Ydistance = Math.Abs(sy - dy);
			// Get Length from Startpoint to Endpoint
			double Length = Math.Sqrt((Xdistance * Xdistance) + (Ydistance * Ydistance));			
			// Get Theta based X-cordinate by using Acos 
			double theta = Math.Acos(Xdistance / Length) / rad;
			// X,Y Distance between Endpoint and The Point that crossed two EndPoint of Arrows on Main line 
			double XCrossDiff = (ArrowLength * Math.Cos(ArrowTheta * rad)) * Math.Cos(theta * rad);
			double YCrossDiff = (ArrowLength * Math.Cos(ArrowTheta * rad)) * Math.Sin(theta * rad);
			// X,Y Distance between Arrow's Endpoint and The Cross Point that get using above values
			double XEndDiff = (ArrowLength * Math.Sin(ArrowTheta * rad)) * Math.Cos((90 - theta) * rad);
			double YEndDiff = (ArrowLength * Math.Sin(ArrowTheta * rad)) * Math.Sin((90 - theta) * rad);


			Point ThisThumb = EndPoint;
			Arrow1.StartPoint = ThisThumb;
			Arrow2.StartPoint = ThisThumb;

			// if StartNode is in Left side
			if (sx <= dx)	
			{
				// if StartNode's y is above Endnode's y
				// 2-coordinate
				if (sy <= dy)
				{
					Arrow1.EndPoint = new Point(ThisThumb.X - XCrossDiff + XEndDiff, ThisThumb.Y - YCrossDiff - YEndDiff);
					Arrow2.EndPoint = new Point(ThisThumb.X - XCrossDiff - XEndDiff, ThisThumb.Y - YCrossDiff + YEndDiff);					
				}
				// 3-coordinate
				else
				{
					Arrow1.EndPoint = new Point(ThisThumb.X - XCrossDiff - XEndDiff, ThisThumb.Y + YCrossDiff - YEndDiff);
					Arrow2.EndPoint = new Point(ThisThumb.X - XCrossDiff + XEndDiff, ThisThumb.Y + YCrossDiff + YEndDiff);
				}
			}
			// StartNode in Right side
			else
			{
				// if StartNode's y is above Endnode's y
				// 1-coordinate
				if (sy <= dy)
				{
					Arrow1.EndPoint = new Point(ThisThumb.X + XCrossDiff + XEndDiff, ThisThumb.Y - YCrossDiff + YEndDiff);
					Arrow2.EndPoint = new Point(ThisThumb.X + XCrossDiff - XEndDiff, ThisThumb.Y - YCrossDiff - YEndDiff);
				}
				// 4-coordinate
				else
				{
					Arrow1.EndPoint = new Point(ThisThumb.X + XCrossDiff - XEndDiff, ThisThumb.Y + YCrossDiff + YEndDiff);
					Arrow2.EndPoint = new Point(ThisThumb.X + XCrossDiff + XEndDiff, ThisThumb.Y + YCrossDiff - YEndDiff);
				}
			}		
		}

		// This method makes two Lines for Directed Mode
		// Arrow Lines are saved in Target Node's ArrowLines.
		public Tuple<LineGeometry, LineGeometry> MakeArrow(Node Source)
		{
			Point SourcePoint = new Point(Canvas.GetLeft(Source) + Source.ActualWidth / 2, Canvas.GetTop(Source) + Source.ActualHeight / 2 - 8);
			Point TargetPoint = new Point(Canvas.GetLeft(this) + this.ActualWidth / 2, Canvas.GetTop(this) + this.ActualHeight / 2 - 8);

			// move edge to border
			Tuple<Point, Point> tmp = EdgeIntoBorder(SourcePoint, TargetPoint);
			SourcePoint = tmp.Item1;
			TargetPoint = tmp.Item2;

			// Save two Arrow line for Directed Mode 
			// Arrow's StartPoint is line's endpoint 
			LineGeometry Arrow1 = new LineGeometry();
			Arrow1.StartPoint = TargetPoint;
			Arrow1.EndPoint = TargetPoint;
			LineGeometry Arrow2 = new LineGeometry();
			Arrow2.StartPoint = TargetPoint;
			Arrow2.EndPoint = TargetPoint;
			// Arrows are Saved in target node
			this.ArrowLines.Add(Arrow1);
			this.ArrowLines.Add(Arrow2);

			// Deside the Arrow's EndPoint
			this.ArrowEndPoint(Arrow1, Arrow2, SourcePoint, TargetPoint);

			return Tuple.Create(Arrow1, Arrow2);
		}

		// This method establishes a link between current Node and target Node using a predefined line geometry		
		public bool LinkTo(Node target, LineGeometry line)
		{			
			// Save Linked Node
			if(!IsLinked(target))
				this.LinkedNode.Add(target);
			target.LinkedSourceNode.Add(this);

			// Save as starting line for current thumb
			this.StartLines.Add(line);
			// Save as ending line for target thumb
			target.EndLines.Add(line);			
			// Save StartPoint for Drawing Arrow in target node
			target.EndSourceLine.Add(line);

			// Ensure both tumbs the latest layout
			this.UpdateLayout();
			target.UpdateLayout();
			// Update line position
			line.StartPoint = new Point(Canvas.GetLeft(this) + this.ActualWidth / 2, Canvas.GetTop(this) + this.ActualHeight / 2 - 8);
			
			// Update line position
			line.EndPoint = new Point(Canvas.GetLeft(target) + target.ActualWidth / 2, Canvas.GetTop(target) + target.ActualHeight / 2 - 8);

			// move edge to border
			Tuple<Point, Point> tmp = EdgeIntoBorder(line.StartPoint, line.EndPoint);
			line.StartPoint = tmp.Item1;
			line.EndPoint = tmp.Item2;
			
			return true;
		}
		

		#endregion

		#region UpdateLink
		// This method updates all the starting and ending lines assigned for the given thumb 
		// according to the latest known thumb position on the canvas
		public void UpdateLinks()
		{
			double left = Canvas.GetLeft(this);
			double top = Canvas.GetTop(this);
			Tuple<Point, Point> tmp;
			for (int i = 0; i < this.StartLines.Count; i++)
			{				
				this.StartLines[i].StartPoint = new Point(left + this.ActualWidth / 2, top + this.ActualHeight / 2 - 8);
					
				tmp = EdgeIntoBorder(this.StartLines[i].StartPoint, 
					new Point(Canvas.GetLeft(LinkedNode[i]) + LinkedNode[i].ActualWidth/2, Canvas.GetTop(LinkedNode[i]) + LinkedNode[i].ActualHeight/2 - 8));
				this.StartLines[i].StartPoint = tmp.Item1;
				this.StartLines[i].EndPoint = tmp.Item2;
			}

			for (int i = 0; i < this.EndLines.Count; i++)
			{
				this.EndLines[i].EndPoint = new Point(left + this.ActualWidth / 2, top + this.ActualHeight / 2 - 8);
				tmp = EdgeIntoBorder(new Point(Canvas.GetLeft(LinkedSourceNode[i]) + LinkedSourceNode[i].ActualWidth/2,
					Canvas.GetTop(LinkedSourceNode[i]) + LinkedSourceNode[i].ActualHeight/2 -8)
					, this.EndLines[i].EndPoint);
				this.EndLines[i].StartPoint = tmp.Item1;
				this.EndLines[i].EndPoint = tmp.Item2;
			}
			
			// Update Arrows Position
			for (int i = 0; i < this.ArrowLines.Count; i += 2)			
				this.ArrowEndPoint(ArrowLines[i], ArrowLines[i + 1], EndSourceLine[i / 2].StartPoint, EndSourceLine[i / 2].EndPoint);								
		}

		public void UpdateArrows()
		{
			// Update Arrows Position
			for (int i = 0; i < this.ArrowLines.Count; i += 2)
				this.ArrowEndPoint(ArrowLines[i], ArrowLines[i + 1], EndSourceLine[i / 2].StartPoint, EndSourceLine[i / 2].EndPoint);								
		}

		#endregion

		// Upon applying template we apply the "Label" and "ImageSource" properties to the template elements.
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Access the textblock element of template and assign it if Title property defined
			if (this.Label != string.Empty)
			{
				TextBlock txt = this.Template.FindName("tplTextBlock", this) as TextBlock;
				if (txt != null)
					txt.Text = Label;
			}

			// Access the image element of our custom template and assign it if ImageSource property defined
			if (this.ImageSource != string.Empty)
			{
				Image img = this.Template.FindName("tplImage", this) as Image;
				if (img != null)
					img.Source = new BitmapImage(new Uri(this.ImageSource, UriKind.Relative));
			}
		}

		public void MakeRows()
		{
			int cnt = 1;
			for (int i = 0; i < this.LinkedSourceNode.Count; i++)
				cnt *= this.LinkedSourceNode[i].ProbabilityTable.Columns.Count;
			for (int i = this.ProbabilityTable.Rows.Count; i < cnt; i++)
				this.ProbabilityTable.Rows.Add(this.ProbabilityTable.NewRow());

			// if other linkedsource node make new column, then update number of rows and names
			if (cnt != this.RowHeaders.Count)
				this.MakeRowHeaders();
		}

		public void MakeRowHeaders()
		{
			int cnt = 1;
			this.RowHeaders.Clear();
			for (int i = 0; i < this.LinkedSourceNode.Count; i++)
				cnt *= this.LinkedSourceNode[i].ProbabilityTable.Columns.Count;
			for (int i = this.RowHeaders.Count; i < cnt; i++)
				this.RowHeaders.Add("");


			int idx = 0, lcnt=1;
			for (int i = 0; i < this.LinkedSourceNode.Count; i++)
			{
				lcnt = lcnt * this.LinkedSourceNode[i].ProbabilityTable.Columns.Count;
				for (int j = 0,jj=0; j < cnt; j++,jj++)
				{
					if (cnt / lcnt == jj) { idx++; jj = 0; }
					if (idx >= this.LinkedSourceNode[i].ProbabilityTable.Columns.Count) idx = 0;
					this.RowHeaders[j] += this.LinkedSourceNode[i].Label.Substring(0,2) + idx;
					if (i + 1 < this.LinkedSourceNode.Count)
						this.RowHeaders[j] += ",";
				}
				idx = 0;
			}
		}

		public bool IsLinked(Node target)
		{
			for(int i=0;i<this.LinkedNode.Count;i++)
			{
				if (this.LinkedNode[i] == target)
					return true;
			}
			return false;
		}


	}
}
