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

using System.IO;
using System.Windows;
using libMBIN;

//=============================================================================

namespace NMS.PAK.MBIN
{
	public class Data : NMS.PAK.Entry.Data
	{
		protected MBINFile m_dom;
		protected string   m_exml;

		//...........................................................

		public Data ( NMS.PAK.Entry.Info INFO, Stream RAW )
		:	base ( INFO, RAW )
		{
		}

		//...........................................................

		public override UIElement ViewerControl {
			get { return new Viewer { Text = EXML }; }
		}

		//...........................................................

		public MBINFile DOM {
			get {
				if( m_dom == null ) {
					var dom = new MBINFile(Raw, true);
					if( dom.Load() && dom.Header.IsValid ) {
						m_dom = dom;
					}
				}
				return m_dom;
			}
		}

		public string EXML {
			get {
				if( m_exml == null && DOM != null ) {
					m_exml  = EXmlFile.WriteTemplate(DOM.GetData());
				}
				return m_exml; 
			}
		}

	}
}

//=============================================================================
