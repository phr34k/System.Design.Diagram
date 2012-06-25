using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Threading;

namespace System.Design.Diagram
{

    public class LinkEventArgs : EventArgs
    {
        public object    Source          { get; set; }
        public object    Target          { get; set; }
        public Connector SourceConnector { get; set; }
        public Connector TargetConnector { get; set; }
    }
    public class LinkCanEstablishEventArgs : LinkEventArgs
    {
        public bool IsAllowed { get; set; }
    }
    public delegate void LinkCanEstablishEventHandler(object source, LinkCanEstablishEventArgs args);
    public delegate void LinkChangedEventHandler(object source, LinkEventArgs args);
    public delegate void LinkPriorityChangedEventHandler(object source, object item1, object item2, float priority);

    [TemplatePart(Name = "PART_ScrollViewer", Type = typeof(ScrollViewer))]
    public class DesignerCanvas : MultiSelector
    {
        #region Private Members

        private Point? point;
        private ScrollViewer scrollViewer;
        private TextBlock labelTextBlock;
        

        #endregion

        #region Public Members
        public event LinkChangedEventHandler ConnectionLinked;
        public event LinkChangedEventHandler ConnectionUnlinked;
        public event LinkPriorityChangedEventHandler ConnectionPriorityChanged;
        public event LinkCanEstablishEventHandler ConnectionCanLink;

        #endregion

        #region Public Properties

        public IEnumerable<DesignerConnection> Connections
        {
            get
            {
                foreach (DesignerConnection conn in ConnectionLayer.Children)
                    yield return conn;
            }
        }

        public Canvas ConnectionLayer
        {
            get
            {
                return GetTemplateChild("Part_Connections") as Canvas;
            }

        }

        public double HorizontalScrollOffset
        {
            get
            {
                return scrollViewer.HorizontalOffset;
            }            
        }

        public double VerticalScrollOffset
        {
            get
            {
                return scrollViewer.VerticalOffset;
            }            
        }

        public bool ShowConnections
        {
            get
            {
                return Convert.ToBoolean(GetValue(ShowConnectionsProperty));
            }
            set
            {
                SetValue(ShowConnectionsProperty, value);
            }
        }

        public bool ShowGrid
        {
            get
            {
                return Convert.ToBoolean(GetValue(ShowGridProperty));
            }
            set
            {
                SetValue(ShowGridProperty, value);
            }
        }

        public static readonly DependencyProperty ShowConnectionsProperty =
            DependencyProperty.Register("ShowConnections", typeof(bool), typeof(DesignerCanvas), new PropertyMetadata() { DefaultValue = true });
        public static readonly DependencyProperty ShowGridProperty =
            DependencyProperty.Register("ShowGrid", typeof(bool), typeof(DesignerCanvas), new PropertyMetadata() { DefaultValue = true });

        #endregion

        #region Private Methods
        
        internal object OnBringItemIntoView(object arg)
        {
            FrameworkElement element = this.ItemContainerGenerator.ContainerFromItem(arg) as FrameworkElement;
            if (element != null)
            {
                element.BringIntoView(new Rect(0, 0, (this.RenderSize.Width / 2) + (element.RenderSize.Width / 2), (this.RenderSize.Height / 2) + (element.RenderSize.Height / 2)));
            }
            return null;
        }

        private T GetTemplateChild<T>(string childName) where T : DependencyObject
        {

            T childControl = this.GetTemplateChild(childName) as T;

            if (childControl == null)
            {

                throw new Exception(string.Format("Couldn't find {0}", childName));

            }

            return childControl;

        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is DesignerItem);
        }
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DesignerItem();
        }
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            DesignerItem realItem = element as DesignerItem;


            var a = (from DesignerConnection connection in ConnectionLayer.Children
                     where (connection.Source.ParentDesignerItem == realItem || connection.Sink.ParentDesignerItem == realItem)
                     select connection).ToArray();

            foreach (DesignerConnection connection in a)
            {
                if (connection.Source.ParentDesignerItem == element)
                {
                    object item2 = ItemContainerGenerator.ItemFromContainer(connection.Sink.ParentDesignerItem);
                    LinkEventArgs args = new LinkEventArgs();
                    args.Source = item;
                    args.Target = item2;
                    args.SourceConnector = connection.Source;
                    args.TargetConnector = connection.Sink;
                    if (ConnectionUnlinked != null) ConnectionUnlinked.Invoke(this, args);
                }
                else
                {
                    object item2 = ItemContainerGenerator.ItemFromContainer(connection.Source.ParentDesignerItem);
                    LinkEventArgs args = new LinkEventArgs();
                    args.Source = item2;
                    args.Target = item;
                    args.SourceConnector = connection.Sink;
                    args.TargetConnector = connection.Source;
                    if (ConnectionUnlinked != null) ConnectionUnlinked.Invoke(this, args);
                }



                //NotifyConnectionUnlink(connection.Source, connection.Sink);
                ConnectionLayer.Children.Remove(connection);
            }


            base.ClearContainerForItemOverride(element, item);
        }
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            DesignerItem contentitem = element as DesignerItem;



            contentitem.SetBinding(ResizeableCanvas.LeftProperty, new Binding("Left") { Mode = BindingMode.TwoWay, Source = contentitem });
            contentitem.SetBinding(ResizeableCanvas.TopProperty, new Binding("Top") { Mode = BindingMode.TwoWay, Source = contentitem });
            base.PrepareContainerForItemOverride(element, item);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            SelectedItem = null;
        }
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                point = e.GetPosition(this);
            base.OnMouseRightButtonDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.RightButton != MouseButtonState.Pressed)
                point = null;
            if (point.HasValue)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    SelectionAdorner adorner = new SelectionAdorner(this);
                    if (adorner != null)
                    {
                        adornerLayer.Add(adorner);
                        e.Handled = true;
                    }
                }

                point = null;
            }

            base.OnMouseMove(e);
        }


        internal void NotifyDesignerItemClicked(DesignerItem designerItem, MouseButton mouseButton)
        {
            object item = base.ItemContainerGenerator.ItemFromContainer(designerItem);
            if (item != null)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    SelectedItems.Add(item);
                }
                else if (!SelectedItems.Contains(item))
                {
                    SelectedItem = item;
                }
            }
        }
        internal static bool UiGetIsSelectable(DependencyObject o)
        {
            if (o != null)
            {
                return true;
            }
            return false;
        }

        internal void NotifyConnectionLink(Connector source, Connector sink)
        {

            object item1 = ItemContainerGenerator.ItemFromContainer((source.ParentDesignerItem as DesignerItem));
            object item2 = ItemContainerGenerator.ItemFromContainer((sink.ParentDesignerItem as DesignerItem));

            try
            {
                LinkEventArgs args = new LinkEventArgs();
                args.Source = item1;
                args.SourceConnector = source;
                args.Target = item2;
                args.TargetConnector = sink;
                if (ConnectionLinked != null) ConnectionLinked.Invoke(this, args);
            }
            catch (Exception ex)
            {

            }
        }
        internal void NotifyConnectionUnlink(Connector source, Connector sink)
        {
            object item1 = ItemContainerGenerator.ItemFromContainer((source.ParentDesignerItem as DesignerItem));
            object item2 = ItemContainerGenerator.ItemFromContainer((sink.ParentDesignerItem as DesignerItem));

            try
            {
                LinkEventArgs args = new LinkEventArgs();
                args.Source = item1;
                args.SourceConnector = source;
                args.Target = item2;
                args.TargetConnector = sink;
                if (ConnectionUnlinked != null) ConnectionUnlinked.Invoke(this, args);
            }
            catch (Exception ex)
            {

            }
        }
        internal void NotifyConnectionPriority(DesignerItem source, DesignerItem sink)
        {
            object item1 = ItemContainerGenerator.ItemFromContainer(source);
            object item2 = ItemContainerGenerator.ItemFromContainer(sink);

            try
            {
                if (ConnectionPriorityChanged != null)
                {
                    double left = ResizeableCanvas.GetLeft(source);
                    ConnectionPriorityChanged.Invoke(this, item1, item2, (float)left);
                }
            }
            catch (Exception ex)
            {

            }
        }
        internal bool IsLinkable(Connector source, Connector sink)
        {
            object item1 = ItemContainerGenerator.ItemFromContainer((source.ParentDesignerItem as DesignerItem));
            object item2 = ItemContainerGenerator.ItemFromContainer((sink.ParentDesignerItem as DesignerItem));

            try
            {
                LinkCanEstablishEventArgs args = new LinkCanEstablishEventArgs();
                args.Source = item1;
                args.SourceConnector = source;
                args.Target = item2;
                args.TargetConnector = sink;
                args.IsAllowed = true;
                if (ConnectionCanLink != null) ConnectionCanLink.Invoke(this, args);
                return args.IsAllowed;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

        #region Public Methods

        public void ScrollIntoView(object item)
        {
            if (base.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                OnBringItemIntoView(item);
            }
            else
            {
                base.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(this.OnBringItemIntoView), item);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.scrollViewer = this.GetTemplateChild<ScrollViewer>("PART_ScrollViewer");
        }


        public object[] FindConnections(object item)
        {
            List<object> connections = new List<object>();
            foreach( DesignerConnection connection in  Connections )
            {
                DesignerItem item2 = UIHelper.FindAnchestor<DesignerItem>(connection.Source);
                DesignerItem item3 = UIHelper.FindAnchestor<DesignerItem>(connection.Sink);
                object c = ItemContainerGenerator.IndexFromContainer( item2 );
                object d = ItemContainerGenerator.IndexFromContainer(item2);

                if (c == item )
                {
                    connections.Add(d);    
                }

                if (d == item)
                {
                    connections.Add(c);
                }
            }

            return connections.Distinct().ToArray();
        }



        #endregion

        #region Ctor/Dtor

        static DesignerCanvas()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DesignerCanvas), new FrameworkPropertyMetadata(typeof(DesignerCanvas)));
            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ApplicationCommands.Delete, CommandDeleteHandler));
            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ApplicationCommands.Copy, CommandCopyHandler));
            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ApplicationCommands.Cut, CommandCutHandler));
            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ApplicationCommands.Paste, CommandPasteHandler));


            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ComponentCommands.MoveDown, CommandMoveDownHandler));
            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ComponentCommands.MoveUp, CommandCopyHandler));
            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ComponentCommands.MoveLeft, CommandCutHandler));
            CommandManager.RegisterClassCommandBinding(typeof(DesignerCanvas), new CommandBinding(ComponentCommands.MoveRight, CommandPasteHandler));


        }


        private static void CommandMoveDownHandler(object target, ExecutedRoutedEventArgs e)
        {
            DesignerCanvas canvas = (e.Source as DesignerCanvas);
            object obj = canvas.SelectedItem;
            if (obj != null)
            {
                DesignerItem designItem = canvas.ItemContainerGenerator.ContainerFromItem(obj) as DesignerItem;
                designItem.SetValue(DesignerItem.LeftProperty, Convert.ToDouble(designItem.GetValue(DesignerItem.LeftProperty)) + 10);
                designItem.SetValue(DesignerItem.TopProperty, Convert.ToDouble(designItem.GetValue(DesignerItem.TopProperty)) + 10);
            }
            
        }

        private static void CommandDeleteHandler(object target, ExecutedRoutedEventArgs e)
        {
            DesignerCanvas canvas = (e.Source as DesignerCanvas);
            object obj = canvas.SelectedItem;
            canvas.Items.Remove(obj);
        }

        private static void CommandCopyHandler(object target, ExecutedRoutedEventArgs e)
        {
            DesignerCanvas canvas = (e.Source as DesignerCanvas);            
            DesignerItem designItem = canvas.ItemContainerGenerator.ContainerFromItem(canvas.SelectedItem) as DesignerItem;
            MetaData meta = new MetaData();
            meta.left = (double)(designItem.GetValue(DesignerItem.LeftProperty));
            meta.top = (double)(designItem.GetValue(DesignerItem.TopProperty));
            meta.item = canvas.SelectedItem;
            Clipboard.SetData("DesignerCanvas", meta );
        }

        private static void CommandCutHandler(object target, ExecutedRoutedEventArgs e)
        {            
            CommandCopyHandler(target, e);

            DesignerCanvas canvas = (e.Source as DesignerCanvas);            
            canvas.Items.Remove(canvas.SelectedItem);
        }


        private static void CommandPasteHandler(object target, ExecutedRoutedEventArgs e)
        {
            object o = Clipboard.GetData("DesignerCanvas");
            if (o != null)
            {
                DesignerCanvas canvas = (e.Source as DesignerCanvas);               
                MetaData meta = (MetaData)o;                
                canvas.Items.Add(meta.item);
                (canvas.ItemContainerGenerator.ContainerFromItem(meta.item) as DesignerItem).SetValue(DesignerItem.LeftProperty, meta.left + 10);
                (canvas.ItemContainerGenerator.ContainerFromItem(meta.item) as DesignerItem).SetValue(DesignerItem.TopProperty, meta.top + 10);
                canvas.SelectedItem = meta.item;
            }
        }

        public DesignerCanvas()
        {

        }

        #endregion

        #region Nested

        [Serializable()]
        struct MetaData
        {
            public double left;
            public double top;
            public object item;
        }

        #endregion
    }

    public class DesignerConnection : Control, INotifyPropertyChanged
    {
        #region Private Members

        private Connector source;
        private Connector sink;
        private PathGeometry pathGeometry;
        private static LeftConverter LeftConverterField = new LeftConverter();
        private static TopConverter TopConverterField = new TopConverter();

        #endregion

        #region Public Members

        public static readonly DependencyProperty SourceAnchorTopProperty;
        public static readonly DependencyProperty SourceAnchorLeftProperty;
        public static readonly DependencyProperty SourceAnchorAngleProperty;
        public static readonly DependencyProperty SinkAnchorTopProperty;
        public static readonly DependencyProperty SinkAnchorLeftProperty;
        public static readonly DependencyProperty SinkAnchorAngleProperty;

        #endregion

        #region Public Properties

        public Connector Source
        {
            get
            {
                return source;
            }
            set
            {
                if (source != value)
                {
                    DependencyPropertyDescriptor dpd1 = DependencyPropertyDescriptor.FromProperty(DesignerItem.LeftProperty, typeof(DesignerItem));
                    DependencyPropertyDescriptor dpd2 = DependencyPropertyDescriptor.FromProperty(DesignerItem.TopProperty, typeof(DesignerItem));
                    if (dpd1 != null)
                    {
                        if (source != null)
                            dpd1.RemoveValueChanged(source.ParentDesignerItem, OnDesignerItemPositionChanged);
                        if (value != null)
                            dpd1.AddValueChanged(value.ParentDesignerItem, OnDesignerItemPositionChanged);
                    }
                    if (dpd2 != null)
                    {
                        if (source != null)
                            dpd2.RemoveValueChanged(source.ParentDesignerItem, OnDesignerItemPositionChanged);
                        if (value != null)
                            dpd2.AddValueChanged(value.ParentDesignerItem, OnDesignerItemPositionChanged);
                    }
                    source = value;

                    /*
                    SetBinding(DesignerConnection.SourceAnchorTopProperty, new Binding("Left") { Mode = BindingMode.OneWay, Source = value.ParentDesignerItem });
                    SetBinding(DesignerConnection.SourceAnchorLeftProperty, new Binding("Top") { Mode = BindingMode.OneWay, Source = value.ParentDesignerItem });                    
                    */
                    UpdatePathGeometry();
                }
            }
        }
        public Connector Sink
        {
            get
            {
                return sink;
            }
            set
            {
                if (sink != value)
                {
                    DependencyPropertyDescriptor dpd1 = DependencyPropertyDescriptor.FromProperty(DesignerItem.LeftProperty, typeof(DesignerItem));
                    DependencyPropertyDescriptor dpd2 = DependencyPropertyDescriptor.FromProperty(DesignerItem.TopProperty, typeof(DesignerItem));
                    if (dpd1 != null) { if (sink != null) dpd1.RemoveValueChanged(sink.ParentDesignerItem, OnDesignerItemPositionChanged); if (value != null) dpd1.AddValueChanged(value.ParentDesignerItem, OnDesignerItemPositionChanged); }
                    if (dpd2 != null) { if (sink != null) dpd2.RemoveValueChanged(sink.ParentDesignerItem, OnDesignerItemPositionChanged); if (value != null) dpd2.AddValueChanged(value.ParentDesignerItem, OnDesignerItemPositionChanged); }
                    sink = value;

                    //SetBinding(DesignerConnection.SinkAnchorTopProperty, new Binding("Left") { Mode = BindingMode.OneWay, Source = value.ParentDesignerItem });
                    //SetBinding(DesignerConnection.SinkAnchorLeftProperty, new Binding("Top") { Mode = BindingMode.OneWay, Source = value.ParentDesignerItem });
                    UpdatePathGeometry();
                }
            }
        }
        public PathGeometry PathGeometry
        {
            get { return pathGeometry; }
            set
            {
                if (pathGeometry != value)
                {
                    pathGeometry = value;
                    if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PathGeometry"));
                }
            }
        }

        public double SourceAnchorAngle
        {
            get { return (double)GetValue(DesignerConnection.SourceAnchorAngleProperty); }
            set { SetValue(DesignerConnection.SourceAnchorAngleProperty, value); }
        }

        public double SinkAnchorAngle
        {
            get { return (double)GetValue(DesignerConnection.SinkAnchorAngleProperty); }
            set { SetValue(DesignerConnection.SinkAnchorAngleProperty, value); }
        }

        public double SourceAnchorTop
        {
            get { return (double)GetValue(DesignerConnection.SourceAnchorTopProperty); }
            set { SetValue(DesignerConnection.SourceAnchorTopProperty, value); }
        }

        public double SourceAnchorLeft
        {
            get { return (double)GetValue(DesignerConnection.SourceAnchorLeftProperty); }
            set { SetValue(DesignerConnection.SourceAnchorLeftProperty, value); }
        }

        public double SinkAnchorTop
        {
            get { return (double)GetValue(DesignerConnection.SinkAnchorTopProperty); }
            set { SetValue(DesignerConnection.SinkAnchorTopProperty, value); }
        }

        public double SinkAnchorLeft
        {
            get { return (double)GetValue(DesignerConnection.SinkAnchorLeftProperty); }
            set { SetValue(DesignerConnection.SinkAnchorLeftProperty, value); }
        }


        #endregion

        #region Private Methods

        private static void OnPositioningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as DesignerConnection).UpdatePathGeometry();
            (d as DesignerConnection).UpdateAnchorPosition();

            DesignerCanvas canvas = FindAnchestor<DesignerCanvas>((d as DesignerConnection));
            if (canvas != null)
            {
                canvas.NotifyConnectionPriority((d as DesignerConnection).Source.ParentDesignerItem, (d as DesignerConnection).Sink.ParentDesignerItem);
            }
        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void DesignerConnection_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePathGeometry();
            UpdateAnchorPosition();
        }

        private void OnConnectorPositionChanged(object sender, PropertyChangedEventArgs e)
        {
            /*
            if (e.PropertyName.Equals("Position"))
            {
                UpdatePathGeometry();
                UpdateAnchorPosition();
            }
            */
        }

        private void UpdatePathGeometry()
        {
            if (Source != null && Sink != null)
            {
                PathGeometry geometry = new PathGeometry();
                List<Point> linePoints = PathFinder.GetConnectionLine(Source, Sink, true);

                SourceAnchorLeft = linePoints[0].Y;
                SourceAnchorTop = linePoints[0].X;
                SinkAnchorLeft = linePoints[linePoints.Count - 1].Y;
                SinkAnchorTop = linePoints[linePoints.Count - 1].X;

                if (linePoints.Count > 0)
                {
                    PathFigure figure = new PathFigure();
                    figure.StartPoint = linePoints[0];
                    linePoints.Remove(linePoints[0]);
                    figure.Segments.Add(new PolyLineSegment(linePoints, true));
                    geometry.Figures.Add(figure);

                    this.PathGeometry = geometry;
                }


            }
        }

        private void UpdateAnchorPosition()
        {
            try
            {
                Point pathStartPoint, pathTangentAtStartPoint;
                Point pathEndPoint, pathTangentAtEndPoint;
                Point pathMidPoint, pathTangentAtMidPoint;

                if (PathGeometry == null)
                    return;

                // the PathGeometry.GetPointAtFractionLength method gets the point and a tangent vector 
                // on PathGeometry at the specified fraction of its length
                this.PathGeometry.GetPointAtFractionLength(0, out pathStartPoint, out pathTangentAtStartPoint);
                this.PathGeometry.GetPointAtFractionLength(1, out pathEndPoint, out pathTangentAtEndPoint);
                this.PathGeometry.GetPointAtFractionLength(0.5, out pathMidPoint, out pathTangentAtMidPoint);

                // get angle from tangent vector
                this.SourceAnchorAngle = Math.Atan2(-pathTangentAtStartPoint.Y, -pathTangentAtStartPoint.X) * (180 / Math.PI);
                this.SinkAnchorAngle = Math.Atan2(pathTangentAtEndPoint.Y, pathTangentAtEndPoint.X) * (180 / Math.PI);

                // add some margin on source and sink side for visual reasons only
                pathStartPoint.Offset(-pathTangentAtStartPoint.X * 5, -pathTangentAtStartPoint.Y * 5);
                pathEndPoint.Offset(pathTangentAtEndPoint.X * 5, pathTangentAtEndPoint.Y * 5);
            }
            catch (Exception ex)
            {
            }
        }

        private void OnDesignerItemPositionChanged(object o, EventArgs e)
        {
            UpdatePathGeometry();
            UpdateAnchorPosition();

            DesignerCanvas canvas = FindAnchestor<DesignerCanvas>(this);
            if (canvas != null)
            {
                canvas.NotifyConnectionPriority(Source.ParentDesignerItem, Sink.ParentDesignerItem);
            }
        }

        #endregion 

        #region Ctor/Dtor

        static DesignerConnection()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DesignerConnection), new FrameworkPropertyMetadata(typeof(DesignerConnection)));
            SourceAnchorTopProperty = DependencyProperty.Register("SourceAnchorTop", typeof(double), typeof(DesignerConnection),
                new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPositioningChanged)));
            SourceAnchorLeftProperty = DependencyProperty.Register("SourceAnchorLeft", typeof(double), typeof(DesignerConnection),
                new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPositioningChanged)));
            SinkAnchorTopProperty = DependencyProperty.Register("SinkAnchorTop", typeof(double), typeof(DesignerConnection),
                new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPositioningChanged)));
            SinkAnchorLeftProperty = DependencyProperty.Register("SinkAnchorLeft", typeof(double), typeof(DesignerConnection),
                new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPositioningChanged)));

            SourceAnchorAngleProperty = DependencyProperty.Register("SourceAnchorAngle", typeof(double), typeof(DesignerConnection),
                new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.None));
            SinkAnchorAngleProperty = DependencyProperty.Register("SinkAnchorAngle", typeof(double), typeof(DesignerConnection),
                new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.None));
        }

        public DesignerConnection()
        {
            Loaded += new RoutedEventHandler(DesignerConnection_Loaded);
        }

        #endregion 

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Nested

        private class LeftConverter : IValueConverter
        {
            #region IValueConverter Members

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return ((Connector)parameter).Position.X;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        private class TopConverter : IValueConverter
        {
            #region IValueConverter Members

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return ((Connector)parameter).Position.Y;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #endregion 
    }

    public class DesignerItem : ContentControl
    {
        #region Private Members

        private Connector sink;
        private Connector source;

        #endregion

        #region Public Members

        public static readonly DependencyProperty TopProperty;
        public static readonly DependencyProperty LeftProperty;
        public static readonly DependencyProperty IsSelectedProperty;
        public static readonly RoutedEvent SelectedEvent;
        public static readonly RoutedEvent UnselectedEvent;

        #endregion

        #region Private Properties

        private DesignerCanvas ParentDesigner
        {
            get
            {
                return (this.ParentSelector as DesignerCanvas);
            }
        }

        internal Selector ParentSelector
        {
            get
            {
                return (ItemsControl.ItemsControlFromItemContainer(this) as Selector);
            }
        }

        #endregion

        #region Public Properties

        public Connector Sink
        {
            get
            {

                return sink;
            }
        }
        public Connector Source
        {
            get
            {
                return source;
            }
        }

        public double Top
        {
            get { return (double)GetValue(DesignerItem.TopProperty); }
            set { SetValue(DesignerItem.TopProperty, value); }
        }

        public double Left
        {
            get { return (double)GetValue(DesignerItem.LeftProperty); }
            set { SetValue(DesignerItem.LeftProperty, value); }
        }

        public bool IsSelected
        {
            get
            {
                return (bool)base.GetValue(IsSelectedProperty);
            }
            set
            {
                base.SetValue(IsSelectedProperty, value);
            }
        }

        public event RoutedEventHandler Selected
        {
            add
            {
                base.AddHandler(SelectedEvent, value);
            }
            remove
            {
                base.RemoveHandler(SelectedEvent, value);
            }
        }

        public event RoutedEventHandler Unselected
        {
            add
            {
                base.AddHandler(UnselectedEvent, value);
            }
            remove
            {
                base.RemoveHandler(UnselectedEvent, value);
            }
        }


        #endregion

        #region Private Methods

        protected virtual void OnSelected(RoutedEventArgs e)
        {
            this.HandleIsSelectedChanged(true, e);
        }

        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            this.HandleIsSelectedChanged(false, e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                this.HandleMouseButtonDown(MouseButton.Left);
            }
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                this.HandleMouseButtonDown(MouseButton.Right);
            }
            base.OnMouseRightButtonDown(e);
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DesignerItem container = d as DesignerItem;
            bool newValue = (bool)e.NewValue;
            Selector parentSelector = container.ParentSelector;

            if (newValue)
            {
                container.OnSelected(new RoutedEventArgs(Selector.SelectedEvent, container));
            }
            else
            {
                container.OnUnselected(new RoutedEventArgs(Selector.UnselectedEvent, container));
            }
        }

        private void HandleIsSelectedChanged(bool newValue, RoutedEventArgs e)
        {
            base.RaiseEvent(e);
        }

        private void HandleMouseButtonDown(MouseButton mouseButton)
        {
            if (DesignerCanvas.UiGetIsSelectable(this) && base.Focus())
            {
                DesignerCanvas parentDesigner = this.ParentDesigner;
                if (parentDesigner != null)
                {
                    parentDesigner.NotifyDesignerItemClicked(this, mouseButton);
                }
            }
        }

        #endregion

        #region Public Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            sink = GetTemplateChild("Part_Sink") as Connector;
            source = GetTemplateChild("Part_Source") as Connector;
        }

        #endregion

        #region Ctor/Dtor

        static DesignerItem()
        {
            try
            {
                DefaultStyleKeyProperty.OverrideMetadata(typeof(DesignerItem), new FrameworkPropertyMetadata(typeof(DesignerItem)));
                IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(typeof(DesignerItem),
                    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                        new PropertyChangedCallback(DesignerItem.OnIsSelectedChanged)));
                SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(DesignerItem));
                UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(DesignerItem));
                TopProperty = DependencyProperty.Register("Top", typeof(double), typeof(DesignerItem),
                    new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
                LeftProperty = DependencyProperty.Register("Left", typeof(double), typeof(DesignerItem),
                    new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

            }
            catch (Exception ex)
            {

            }
        }

        public DesignerItem()
        {
        }

        #endregion

    }

    public class Connector : Control
    {
        #region Private Members

        private DesignerItem parentDesignerItem;
        // drag start point, relative to the DesignerCanvas
        private Point? dragStartPoint = null;        
        // center position of this Connector relative to the DesignerCanvas
        private Point position;

        #endregion

        #region Public Members

        public static readonly DependencyProperty NormalProperty = DependencyProperty.RegisterAttached("Normal", typeof(Vector?), typeof(Connector));

        public Point Position
        {
            get { return position; }
            set
            {
                if (position != value)
                {
                    position = value;
                }
            }
        }

        public Vector? Normal
        {
            get { return GetValue(NormalProperty) as Vector?; }
            set
            {
                SetValue(NormalProperty, value);
            }
        }

        public DesignerItem ParentDesignerItem
        {
            get
            {
                if (parentDesignerItem == null)
                    parentDesignerItem = FindAnchestor<DesignerItem>(this);

                return parentDesignerItem;
            }
        }

        #endregion

        #region Private Methods

        // when the layout changes we update the position property
        void Connector_LayoutUpdated(object sender, EventArgs e)
        {
            ScrollViewer viewer = FindAnchestor<ScrollViewer>(this);
            DesignerCanvas designer = GetDesignerCanvas(this);
            if (designer != null)
            {
                //get centre position of this Connector relative to the DesignerCanvas
                this.Position = this.TransformToAncestor(viewer).Transform(new Point(this.Width / 2, this.Height / 2));
                this.Position -= new Vector(-(double)viewer.GetValue(ScrollViewer.HorizontalOffsetProperty),
                    -(double)viewer.GetValue(ScrollViewer.VerticalOffsetProperty));
            }
        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        // iterate through visual tree to get parent DesignerCanvas
        private DesignerCanvas GetDesignerCanvas(DependencyObject element)
        {
            while (element != null && !(element is DesignerCanvas))
                element = VisualTreeHelper.GetParent(element);
            return element as DesignerCanvas;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DesignerCanvas canvas = GetDesignerCanvas(this);
            if (canvas != null)
            {
                // position relative to DesignerCanvas
                this.dragStartPoint = new Point?(e.GetPosition(canvas));
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // if mouse button is not pressed we have no drag operation, ...
            if (e.LeftButton != MouseButtonState.Pressed)
                this.dragStartPoint = null;

            // but if mouse button is pressed and start point value is set we do have one
            if (this.dragStartPoint.HasValue)
            {
                // create connection adorner 
                DesignerCanvas canvas = GetDesignerCanvas(this);
                if (canvas != null)
                {
                    AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                    if (adornerLayer != null)
                    {
                        ConnectorAdorner adorner = new ConnectorAdorner(canvas, this);
                        if (adorner != null)
                        {
                            adornerLayer.Add(adorner);
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Ctor/Dtor

        static Connector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Connector), new FrameworkPropertyMetadata(typeof(Connector)));
        }

        public Connector()
        {
            // fired when layout changes
            base.LayoutUpdated += new EventHandler(Connector_LayoutUpdated);
        }

        #endregion
    }

    public class MoveThumb : Thumb
    {
        #region Private Methods

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            e.Handled = false;
        }
        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            DesignerItem item = this.DataContext as DesignerItem;
            if (item != null)
            {
                //item.Left += e.HorizontalChange;
                //item.Top += e.VerticalChange;




                DesignerCanvas canvas = FindAnchestor<DesignerCanvas>(this);
                foreach (object o in canvas.SelectedItems)
                {
                    DesignerItem item2 = canvas.ItemContainerGenerator.ContainerFromItem(o) as DesignerItem;
                    double left = ResizeableCanvas.GetLeft(item2);
                    double top = ResizeableCanvas.GetTop(item2);
                    ResizeableCanvas.SetLeft(item2, left + e.HorizontalChange);
                    ResizeableCanvas.SetTop(item2, top + e.VerticalChange);
                }



            }
        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        #endregion

        #region Ctor / Dtor

        static MoveThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MoveThumb), new FrameworkPropertyMetadata(typeof(MoveThumb)));
        }

        public MoveThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);

        }

        #endregion
    }

    public class PathFinder
    {
        public static List<Point> GetConnectionLine(Connector Source, Connector Sink, bool something)
        {
            List<Point> points = new List<Point>();
            points.Add(Source.Position);
            if (Source.Normal.HasValue)
            {
                points.Add(Source.Position + Source.Normal.Value);
            }

            if (Sink.Normal.HasValue)
            {
                points.Add(Sink.Position + Sink.Normal.Value);
            }
            points.Add(Sink.Position);
            return points;
        }
    }

    public class DesignerConnectionAdorner : Adorner
    {
        #region Private Members

        private readonly VisualCollection _collection;
        private readonly Canvas _canvas;

        #endregion 

        #region Private Methods

        protected override Visual GetVisualChild(int index)
        {
            return _collection[index];
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return _collection.Count;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _canvas.Arrange(new Rect(0, 0, _canvas.ActualWidth, _canvas.ActualHeight));
            return finalSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {

        }

        #endregion

        #region Ctor / Dtor 

        public DesignerConnectionAdorner(UIElement boundElement)
            : base(boundElement)
        {
            _collection = new VisualCollection(this);
            _canvas = new Canvas();
            _canvas.Background = Brushes.Red;
            _collection.Add(_canvas);
        }

        #endregion
    }

    public class ConnectorAdorner : Adorner
    {
        #region Private Members

        private DesignerCanvas designerCanvas;
        private Connector sourceConnector;
        private Pen drawingPen;

        private DesignerItem hitDesignerItem;
        private DesignerItem HitDesignerItem
        {
            get { return hitDesignerItem; }
            set
            {
                if (hitDesignerItem != value)
                {

                    /*
                    if (hitDesignerItem != null)
                        hitDesignerItem.IsDragConnectionOver = false;
                    */


                    hitDesignerItem = value;

                    /*
                    if (hitDesignerItem != null)
                        hitDesignerItem.IsDragConnectionOver = true;
                     */
                }
            }
        }

        private Connector hitConnector;
        private Connector HitConnector
        {
            get { return hitConnector; }
            set
            {
                if (hitConnector != value)
                {
                    hitConnector = value;
                }
            }
        }

        #endregion

        #region Ctor / Dtor

        public ConnectorAdorner(DesignerCanvas designer, Connector sourceConnector)
            : base(designer)
        {
            this.designerCanvas = designer;
            this.sourceConnector = sourceConnector;
            drawingPen = new Pen(Brushes.LightSlateGray, 1);
            drawingPen.LineJoin = PenLineJoin.Round;
            this.Cursor = Cursors.Cross;
        }

        #endregion

        #region Private Methods

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (HitConnector != null)
            {
                /*
                
                Connection newConnection = new Connection(sourceConnector, sinkConnector);
                */

                Connector sourceConnector = this.sourceConnector;
                Connector sinkConnector = this.HitConnector;
                //this.sourceConnector.ParentDesignerItem;




                Canvas canvas = designerCanvas.ConnectionLayer;
                int count = (from DesignerConnection c in canvas.Children
                             where ( c.Source == sourceConnector && c.Sink == sinkConnector ) || 
                                   ( c.Source == sinkConnector && c.Sink == sourceConnector ) 
                             select c).Count();
                if (count == 0)
                {
                    if (designerCanvas.IsLinkable(sourceConnector, sinkConnector))
                    {
                        canvas.Children.Add(new DesignerConnection()
                        {
                            Source = sourceConnector,
                            Sink = sinkConnector
                        });

                        designerCanvas.NotifyConnectionLink(sourceConnector, sinkConnector);
                    }
                }
                else if (count == 1 && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    var array = (from DesignerConnection c in canvas.Children
                                 where (c.Source == sourceConnector && c.Sink == sinkConnector) ||
                                       (c.Source == sinkConnector && c.Sink == sourceConnector) 
                                 select c).ToArray();
                    foreach (DesignerConnection connection in array)
                    {
                        canvas.Children.Remove(connection);
                        designerCanvas.NotifyConnectionUnlink(connection.Source, connection.Sink);
                    }
                }
            }
            if (HitDesignerItem != null)
            {
                //this.HitDesignerItem.IsDragConnectionOver = false;
            }

            if (this.IsMouseCaptured) this.ReleaseMouseCapture();

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this.designerCanvas);
            if (adornerLayer != null)
            {
                adornerLayer.Remove(this);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!this.IsMouseCaptured) this.CaptureMouse();
                HitTesting(e.GetPosition(this));
                this.InvalidateVisual();
            }
            else
            {
                if (this.IsMouseCaptured) this.ReleaseMouseCapture();
            }
        }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // without a background the OnMouseMove event would not be fired
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        }
        private void HitTesting(Point hitPoint)
        {
            bool hitConnectorFlag = false;

            Cursor = Cursors.Cross;
            DependencyObject hitObject = designerCanvas.InputHitTest(hitPoint) as DependencyObject;
            while (hitObject != null &&
                   hitObject != sourceConnector.ParentDesignerItem &&
                   hitObject.GetType() != typeof(DesignerCanvas))
            {
                if (hitObject is Connector)
                {
                    HitConnector = hitObject as Connector;
                    hitConnectorFlag = true;

                    if (!designerCanvas.IsLinkable(sourceConnector, hitObject as Connector))
                    {
                        Cursor = Cursors.No;
                    }
                }

                if (hitObject is DesignerItem)
                {
                    HitDesignerItem = hitObject as DesignerItem;
                    if (!hitConnectorFlag)
                        HitConnector = null;
                    return;
                }
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }

            HitConnector = null;
            HitDesignerItem = null;
        }

        #endregion

    }

    public class SelectionAdorner : Adorner
    {
        #region Private Members

        private DesignerCanvas designerCanvas;
        private Point startPoint;
        private Pen drawingPen;
        private Brush bg;

        #endregion

        #region Ctor/Dtor

        public SelectionAdorner(DesignerCanvas designer) : base(designer)
        {
            this.designerCanvas = designer;
            startPoint = Mouse.GetPosition(designer);           
            drawingPen = new Pen(Brushes.LightSlateGray, 0.5f);
            drawingPen.LineJoin = PenLineJoin.Round;
            bg = new SolidColorBrush(Colors.LightSlateGray);
            bg.Opacity = 0.1f;
            Cursor = Cursors.Cross;
            Mouse.Capture(this, CaptureMode.Element);
        }

        #endregion

        #region Private Methods

        protected override void OnMouseMove(MouseEventArgs e)
        {
            InvalidateVisual();
            base.OnMouseMove(e);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            Point endPoint = Mouse.GetPosition(designerCanvas);
            Mouse.Capture(null, CaptureMode.None);

            //designerCanvas

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(designerCanvas);
            if (adornerLayer != null)
            {
                adornerLayer.Remove(this);
            }

            float offseth = Convert.ToSingle( designerCanvas.HorizontalScrollOffset );
            float offsetv = Convert.ToSingle( designerCanvas.VerticalScrollOffset );                     
             Rect rect = new Rect(startPoint, endPoint);
             for( int i = 0; i < designerCanvas.Items.Count; i++ )
             {
                 UIElement obj = designerCanvas.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                 if( obj != null )
                 {
                     Rect rect2 = VisualTreeHelper.GetDescendantBounds(obj);
                     rect2.Offset(VisualTreeHelper.GetOffset(obj));
                     rect2.Offset(-offseth, -offsetv);

                     if (rect.IntersectsWith(rect2))
                     {
                         designerCanvas.SelectedItems.Add(designerCanvas.Items[i]);
                     }
                 }
             }
            

        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);            
            Point endPoint = Mouse.GetPosition(designerCanvas);                      
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
            dc.DrawRectangle(bg, drawingPen, new Rect(startPoint, endPoint));
        }

        #endregion
    }
}
