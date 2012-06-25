using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Design.Diagram.Nodes;

namespace System.Design.Diagram
{
    class DiagramDesigner : DesignerCanvas
    {

        private object OnPerformNodeAlignment(object item)
        {
            if (!Items.Contains(item))
                throw new InvalidProgramException();
            DesignerItem item2 = ItemContainerGenerator.ContainerFromItem(item) as DesignerItem;
            if (item2.RenderSize.Width == 0 && item2.RenderSize.Height == 0)
            {
                base.Dispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(this.OnPerformNodeAlignment), item);
                return null;
            }
            else
            {
                item2.Left = HorizontalScrollOffset + (RenderSize.Width / 2.0f) - (item2.RenderSize.Width / 2.0f);
                item2.Top = VerticalScrollOffset + (RenderSize.Height / 2.0f) - (item2.RenderSize.Height / 2.0f);
                return null;
            }
        }

        private void CommandBinding_ExecuteSelectTexture(object sender, ExecutedRoutedEventArgs e)
        {
            /*
            DesignerItem designItem = UIHelper.FindAnchestor<DesignerItem>((e.OriginalSource as DependencyObject));
            TexCoord graphItem = designItem.DataContext as TexCoord;
            if (graphItem != null)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "All supported files|*.jpeg;*.jpg;*.png;*.dds|*.jpeg|*.jpeg;*.jpg|*.png|*.png|*.dds|*.dds";

                if (dialog.ShowDialog() == true)
                {
                    graphItem.Texture = dialog.FileName;
                }
            }
            */
        }

        private void CommandBinding_ExecuteSelectCode(object sender, ExecutedRoutedEventArgs e)
        {
            /*
            DesignerItem designItem = UIHelper.FindAnchestor<DesignerItem>((e.OriginalSource as DependencyObject));
            CodeNode graphItem = designItem.DataContext as CodeNode;
            CodeDialog dialog = new CodeDialog();
            dialog.textBox1.Text = graphItem.Code;

            if (dialog.ShowDialog() == true)
            {
                graphItem.Code = dialog.textBox1.Text;
            }
            */
        }

        private void CommandBinding_ExecuteSelectColor(object sender, ExecutedRoutedEventArgs e)
        {
            /*
            DesignerItem designItem = UIHelper.FindAnchestor<DesignerItem>((e.OriginalSource as DependencyObject));
            ColorNode graphItem = designItem.DataContext as ColorNode;

            System.Windows.Forms.ColorDialog dialog = new System.Windows.Forms.ColorDialog();
            dialog.Color = System.Drawing.Color.FromArgb((int)(graphItem.Color.A * 255.0f),
                    (int)(graphItem.Color.R * 255.0f), (int)(graphItem.Color.G * 255.0f),
                    (int)(graphItem.Color.B * 255.0f));

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                graphItem.Color = new Float4() { R = dialog.Color.R / 255.0f, G = dialog.Color.G / 255.0f, B = dialog.Color.B / 255.0f, A = dialog.Color.A / 255.0f };
            }
             */
        }


        public DiagramDesigner()
        {                       
            CommandBindings.Add(new CommandBinding(GraphNodeCommands.SelectTexture, CommandBinding_ExecuteSelectTexture));
            CommandBindings.Add(new CommandBinding(GraphNodeCommands.SelectColor, CommandBinding_ExecuteSelectColor));
            CommandBindings.Add(new CommandBinding(GraphNodeCommands.SelectCode, CommandBinding_ExecuteSelectCode));
        }



        public void LoadStream(Stream stream)
        {
            object[] graphRoot = null;
            BinaryFormatter _formatter = new BinaryFormatter();            
            graphRoot = _formatter.Deserialize(stream) as object[];


            bool validated = true;
            foreach (GraphNode node in graphRoot[0] as List<GraphNode>)
            {
                IPathNormalize normalizeable = node as IPathNormalize;
                if (normalizeable != null)
                {
                    normalizeable.Denormalize(IO.Path.GetDirectoryName(Reflection.Assembly.GetEntryAssembly().Location));
                    if (!normalizeable.ValidatePaths())
                    {
                        validated = false;
                    }
                }
            }

            if (validated == false)
            {
                MessageBox.Show("Cannot open graph, can't locate dependecies");
                return;
            }


            Items.Clear();
            foreach (GraphNode node in graphRoot[0] as List<GraphNode>)
            {
                Items.Add(node);
            }


            //Deserialize the Position meta-data with the newly generated designer-items. 
            List<Point> l = graphRoot[1] as List<Point>; int i = 0;
            foreach (GraphNode node in graphRoot[0] as List<GraphNode>)
            {
                (ItemContainerGenerator.ContainerFromItem(node) as DesignerItem).SetValue(DesignerItem.LeftProperty, l[i].X);
                (ItemContainerGenerator.ContainerFromItem(node) as DesignerItem).SetValue(DesignerItem.TopProperty, l[i].Y);
                i++;
            }

            //Force the dispatcher to finish generating all items after we loaded them to ensure the state after this occurs problem-less
            //without complex many-wait-for-all scenario's. Essentionally we reroute the dispatcher logic untill at a point we say it's 
            //okay to continue processing the logic, this point of time should be when the ItemContainerGenerator finishes generating the 
            //items.
            DispatcherFrame frame = new DispatcherFrame(false);
            EventHandler handler = delegate(object obj, EventArgs args) { frame.Continue = false; };
            LayoutUpdated += handler;
            Dispatcher.PushFrame(frame);
            LayoutUpdated -= handler;

            //From each node iterate over each of the field to see if the field was linked to another field. If the field is linked we can 
            //infer from that to data to whom it was linked. However knowing who isn't enough we also need 'connector' to identify the two
            //points to draw a line from. Without the connector we would be unable to draw the line, since there would be no Point-data.
            foreach (GraphNode node in graphRoot[0] as List<GraphNode>)
            {
                /*
                //List all fields in the node to infer connection that should be visually represented.
                foreach (Field field in node.Inputs)
                {
                    //If the node has a link estabilished..
                    if (field.Linked != null)
                    {
                        //Find the connector elements i.e. the physical representation of 'link-able' fields for both the source and the sink
                        Connector source = FromNode(field);
                        Connector sink = FromNode(field.Linked);
                        ConnectionLayer.Children.Add(new DesignerConnection()
                        {
                            Source = source,
                            Sink = sink
                        });
                    }
                }
                */
            }
        }

        public void SaveStream(Stream stream)
        {
            List<GraphNode> nodes = new List<GraphNode>();
            List<Point> meta = new List<Point>();
            //List<KeyValuePair<Field, Field>> connections = new List<KeyValuePair<Field, Field>>();

            object[] graphRoot = new object[] { nodes, meta/*, connections*/ };
            foreach (GraphNode node in Items)
            {
                nodes.Add(node);
            }




            foreach (GraphNode node in graphRoot[0] as List<GraphNode>)
            {
                double left = (double)((ItemContainerGenerator.ContainerFromItem(node) as DesignerItem).GetValue(DesignerItem.LeftProperty));
                double top = (double)((ItemContainerGenerator.ContainerFromItem(node) as DesignerItem).GetValue(DesignerItem.TopProperty));
                meta.Add(new Point(left, top));
            }


   
            foreach (GraphNode node in nodes)
            {
                IPathNormalize normalizeable = node as IPathNormalize;
                if (normalizeable != null)
                {
                    normalizeable.Normalize(IO.Path.GetDirectoryName(Reflection.Assembly.GetEntryAssembly().Location));
                }
            }

            BinaryFormatter _formatter = new BinaryFormatter();            
            _formatter.Serialize(stream, graphRoot);
            foreach (GraphNode node in nodes)
            {
                IPathNormalize normalizeable = node as IPathNormalize;
                if (normalizeable != null)
                {
                    normalizeable.Denormalize(IO.Path.GetDirectoryName(Reflection.Assembly.GetEntryAssembly().Location));
                }
            }            
        }
    

        public void PerformNodeAlignment(object item)
        {
            if (base.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                OnPerformNodeAlignment(item);
            }
            else
            {
                base.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(this.OnPerformNodeAlignment), item);
            }
        }
    }
}
