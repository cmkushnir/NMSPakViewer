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
using System.IO;
using System.Windows;

//=============================================================================

namespace NMS.PAK.Entry
{
	public class Data : IComparable<Data>
	{
		public readonly NMS.PAK.Entry.Info Info;
		protected Stream                 m_raw;

		//...........................................................

		protected Data ( NMS.PAK.Entry.Info INFO, Stream RAW )
		{
			Info  = INFO;
			m_raw = RAW;
		}

		//...........................................................

		public static Data New ( NMS.PAK.Entry.Info INFO )
		{
			if( INFO == null ) return null;

			var raw = INFO.Extract();

			switch( INFO.Extension ) {
				case ".TXT":  return new TXT .Data(INFO, raw);
				case ".CSV":  return new CSV .Data(INFO, raw);
				case ".JSON": return new JSON.Data(INFO, raw);
				case ".XML":  return new XML .Data(INFO, raw);
				case ".LUA":  return new LUA .Data(INFO, raw);
				case ".MBIN": return new MBIN.Data(INFO, raw);
				case ".SPV":  return new SPV .Data(INFO, raw);
				case ".DDS":  return new DDS .Data(INFO, raw);
			}
			if( INFO.Path.EndsWith(".MBIN.PC") ) {
				return new MBIN.Data(INFO, raw);
			}

			return new Data(INFO, raw);
		}

		//...........................................................

		public virtual UIElement ViewerControl { get { return null; } }

		//...........................................................

		public Stream Raw {
			get { return m_raw; }
			set { m_raw = value; }
		}

		//...........................................................

		public bool SaveTo ( string PATH )
		{
			if( Info == null || Info.Path.IsNullOrEmpty() ) return false;

			var stream = System.IO.File.Create(PATH, 0,
				System.IO.FileOptions.SequentialScan |
				System.IO.FileOptions.WriteThrough
			);
			m_raw?.CopyTo(stream);
			stream.Close();

			return System.IO.File.Exists(PATH);
		}

		//...........................................................

		public override string ToString ()
		{
			return Info?.Name;
		}

		int IComparable<Data>.CompareTo ( Data RHS )
		{
			if( Info == RHS?.Info ) return  0;
			if( Info == null )      return -1;
			return Info.CompareTo(RHS?.Info);
		}
	}
}

//=============================================================================
