using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace System.Design.Diagram.Nodes
{
    public interface IPathNormalize
    {
        void Normalize(string path);
        void Denormalize(string path);
        bool ValidatePaths();
    }


    [Serializable]
    public struct Symbol
    {
        public string Name { get; set; }
        public string FriendlyName { get; set; }
    }

    [Serializable]
    public class Field
    {
        public Symbol Symbol { get; set; }
        public GraphNode Owner { get; set; }
        public Field Linked { get; set; }

        public bool CanLink(Field ExpressionInput)
        {
            if (Linked == null)
            {
                return true;
            }

            return false;
        }
        public bool CanUnlink(Field ExpressionInput)
        {
            if (Linked == ExpressionInput)
            {
                return true;
            }

            return false;
        }
        public bool Link(Field ExpressionInput)
        {
            if (Linked == null)
            {
                Linked = ExpressionInput;
                return true;
            }

            return false;
        }
        public bool Unlink(Field ExpressionInput)
        {
            if (Linked == ExpressionInput)
            {
                Linked = null;
                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class GraphNode
    {
        public string Name { get; set; }
        [Browsable(false)]
        public Field[] Outputs { get; protected set; }
        [Browsable(false)]
        public Field[] Inputs { get; protected set; }
        protected Field[] BuildFieldStore(Symbol[] Symbols)
        {
            Field[] fs = new Field[Symbols.Length];
            for (int i = 0; i < fs.Length; i++)
            {
                fs[i] = new Field();
                fs[i].Symbol = Symbols[i];
                fs[i].Owner = this;
            }

            return fs;
        }

        static Symbol[] MetaInputSymbols = new Symbol[] 
        {
            new Symbol(){ Name = "Input", FriendlyName = "Input" },           
        };

        static Symbol[] MetaOutputSymbols = new Symbol[] 
        {
            new Symbol(){ Name = "Output", FriendlyName = "Output" },
        };

        public GraphNode()
        {
            Inputs = BuildFieldStore(MetaInputSymbols);
            Outputs = BuildFieldStore(MetaOutputSymbols);
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class GraphNodeCategoryAttribute : Attribute
    {
        public GraphNodeCategoryAttribute(string Category)
        {
            Name = Category;
        }

        public string Name
        {
            get;
            set;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class GraphNodeFriendlyNameAttribute : Attribute
    {
        public GraphNodeFriendlyNameAttribute(string Name)
        {
            this.Name = Name;
        }

        public string Name
        {
            get;
            set;
        }
    }
}
