using System;
using System.Collections;

namespace Framework.Collections
{
	public interface ITreeNode
	{
		object NodeData
		{
			get;
			set;
		}
		ICollection Nodes
		{
			get;
		}
		ITreeNode Parent
		{
			get;
		}

		void addChild(ITreeNode node);
		void removeChild(ITreeNode node);
	}

	public class TreeNodeBase : ITreeNode
	{
		object _data;
		IList _nodes = new List();
		ITreeNode _parent;

		#region ITreeNode Members

		public object NodeData
		{
			get
			{
				return _data;
			}
			set
			{
				_data = value;
			}
		}

		public ICollection Nodes
		{
			get
			{
				return _nodes;
			}
		}

		public ITreeNode Parent
		{
			get
			{
				return _parent;
			}
		}
		public void addChild(ITreeNode node)
		{
			if(node == null)
				throw new ArgumentNullException("node");
			if( Object.ReferenceEquals(node.Parent, this) )
				return;
			if( node.Parent != null )
				node.Parent.removeChild(node);

			_nodes.Add(node);
			((TreeNodeBase)node)._parent = this;
		}

		public void removeChild(ITreeNode node)
		{
			if(node == null)
				throw new ArgumentNullException("node");
			_nodes.Remove(node);
			((TreeNodeBase)node)._parent = null;
		}

		#endregion
	}

	public interface IShellNodeKey
	{
		string Key { get; }
	}
}