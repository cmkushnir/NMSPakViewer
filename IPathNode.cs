//=============================================================================
/*
MIT License

Copyright (c) 2021 Chris Kushnir

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
//=============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

//=============================================================================

namespace NMS
{
	public interface IPathNode : IComparable<IPathNode>, IComparable<string>
	{
		string Text { get; }       // last part of Path
		string Path { get; }       // full path to this node
		object Tag  { get; set; }  // payload

		            IPathNode  Parent { get; }  // parent node, null if root
		IEnumerable<IPathNode> Items  { get; }  // children

		List<IPathNode> PathNodes { get; }  // get nodes from root to this
	}

	//=========================================================================

	public class PathNode<TAG_T, DERIVED_T> : IPathNode
	where TAG_T     : class
	where DERIVED_T : PathNode<TAG_T,DERIVED_T>
	{
		protected string m_text = "";  // directory or file name
		protected string m_path = "";  // path up to (including) this node
		protected TAG_T  m_tag;

		protected      PathNode<TAG_T,DERIVED_T>  m_parent;
		protected List<PathNode<TAG_T,DERIVED_T>> m_items;

		//...........................................................

		/// <summary>
		/// The PATH passed in starts with this node.
		/// The first segment will be parsed from PATH and used as this Text.
		/// The remainder of PATH will be passed to Insert(remainder, TAG).
		/// Only the last segment will be assigned TAG.
		/// Note: The Path property returns the path from the root node to (including) this node.
		/// </summary>
		/// <param name="PARENT">Parent node.</param>
		/// <param name="PATH">Path from this node down. Segments are delimited by '/'.</param>
		/// <param name="TAG">Payload to be attached to leaf node.</param>
		protected PathNode (
			PathNode<TAG_T,DERIVED_T> PARENT = null,
			string                    PATH   = "",
			TAG_T                     TAG    = null
		){
			m_parent = PARENT;
			if( PATH.IsNullOrEmpty() ) return;

			var slash_idx = PATH.IndexOf('/');
			m_text = slash_idx < 1 ? PATH : PATH.Substring(0, slash_idx + 1);
			m_path = m_parent?.Path + m_text;

			var remaining = PATH.Substring(m_text.Length);
			if( remaining.Length < 1 ) m_tag = TAG;
			else             Insert(remaining, TAG);
		}

		//...........................................................

		public string Path {
			get { return m_path; }
		}

		public string Text {
			get { return m_text; }
		}

		object IPathNode.Tag {
			get { return Tag; }
			set { Tag = value as TAG_T; }
		}

		public TAG_T Tag {
			get { return m_tag; }
			set {
				if( m_tag != value ) {
					m_tag  = value;
				}
			}
		}

		public IPathNode Parent {
			get { return m_parent; }
		}

		public IEnumerable<IPathNode> Items {
			get { return m_items; }
		}

		public List<IPathNode> PathNodes {
			get {
				var list  = m_parent?.PathNodes;
				if( list == null ) list = new List<IPathNode>();
				list.Add(this);
				return list;
			}
		}

		//...........................................................

		/// <summary>
		/// Insert PATH + TAG into this list of Items.
		/// Creates sub-nodes as needed.
		/// </summary>
		/// <param name="PATH">
		/// Path under this node e.g. if this node is 'a/' then PATH 
		/// would be 'b/c.x', as opposed to 'a/b/c.x'.
		/// </param>
		/// <param name="TAG">Payload to be attached to leaf node.</param>
		public void Insert ( string PATH, TAG_T TAG )
		{
			if( PATH.IsNullOrEmpty() ) return;

			var slash_idx = PATH.IndexOf('/');
			var text      = slash_idx < 1 ? PATH : PATH.Substring(0, slash_idx + 1);
			var remaining = PATH.Substring(text.Length);

			if( m_items == null ) m_items = new List<PathNode<TAG_T, DERIVED_T>>();

			if( remaining.Length < 1 ) {  // add new leaf
				var at = m_items.BinarySearchInsertIndex(text, Compare);
				m_items.Insert(at, new PathNode<TAG_T, DERIVED_T>(this, text, TAG));
			}
			else {  // add branch to new|existing node
				var item  = m_items.BinarySearchObject(text, Compare);
				if( item != null ) item.Insert(remaining, TAG);
				else {
					var at = m_items.BinarySearchInsertIndex(text, Compare);
					m_items.Insert(at, new PathNode<TAG_T, DERIVED_T>(this, PATH, TAG));
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Search for exact (not partial) PATH as a child of this node
		/// e.g. if this node is 'a/' then PATH would be 'b/c.x', as opposed to 'a/b/c.x'.
		/// </summary>
		/// <param name="PATH">
		/// Segments delimited by '/'.
		/// If looking for a directory then PATH must end with '/'.
		/// </param>
		/// <returns>Null if PATH not found.</returns>
		public PathNode<TAG_T, DERIVED_T> Search ( string PATH )
		{
			if( m_items.IsNullOrEmpty() || PATH.IsNullOrEmpty() ) return null;

			var slash_idx = PATH.IndexOf('/');
			var text      = slash_idx < 1 ? PATH : PATH.Substring(0, slash_idx + 1);

			var found = m_items?.BinarySearchObject(text, Compare);
			if( found == null ) return null;

			var remaining = PATH.Substring(text.Length);
			return remaining.IsNullOrEmpty() ? found :
				found.Search(remaining)
			;
		}

		//...........................................................

		/// <summary>
		/// </summary>
		/// <returns>Text, not Path.</returns>
		public override string ToString ()
		{
			return m_text;
		}

		//...........................................................

		/// <summary>
		/// Compare Text against RHS string.
		/// Directories (Text|RHS ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="RHS">Directory or file name.</param>
		/// <returns></returns>
		public int CompareTo ( string RHS )
		{
			if( RHS == null ) return 1;

			bool lhs_is_dir = m_text.Last() == '/',
				 rhs_is_dir = RHS   .Last() == '/';

			if( lhs_is_dir == rhs_is_dir ) {
				// this and RHS are either both directories or both files
				return string.Compare(m_text, RHS);
			}

			if( lhs_is_dir ) return -1;  // this is directory, RHS is file
			                 return  1;  // this is file, RHS is directory
		}

		/// <summary>
		/// Compare Text against RHS.Text.
		/// Directories (Text|RHS.Text ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="RHS"></param>
		/// <returns></returns>
		public int CompareTo ( IPathNode RHS )
		{
			return CompareTo(RHS?.Text);
		}

		//...........................................................

		/// <summary>
		/// Compare LHS node Text against RHS string.
		/// Directories (Text|RHS ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="LHS"></param>
		/// <param name="RHS"></param>
		/// <returns></returns>
		public static int Compare ( PathNode<TAG_T, DERIVED_T> LHS, string RHS )
		{
			if( LHS == null && RHS == null ) return  0;
			if( LHS == null )                return -1;
			return LHS.CompareTo(RHS);
		}

		/// <summary>
		/// Compare LHS node Text against RHS node Text.
		/// Directories (Text|RHS.Text ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="LHS"></param>
		/// <param name="RHS"></param>
		/// <returns></returns>
		public static int Compare ( PathNode<TAG_T, DERIVED_T> LHS, PathNode<TAG_T, DERIVED_T> RHS )
		{
			if( LHS == null && RHS == null ) return  0;
			if( LHS == null )                return -1;
			return LHS.CompareTo(RHS);
		}
	}
}

//=============================================================================
