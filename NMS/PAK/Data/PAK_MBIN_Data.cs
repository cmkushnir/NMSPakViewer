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

namespace cmk.NMS.PAK.MBIN
{
	public class Data : cmk.NMS.PAK.Item.Data
	{
		protected MBINFile m_mbin;
		protected string   m_exml;

		//...........................................................

		public Data ( cmk.NMS.PAK.Item.Info INFO, Stream RAW )
		:	base ( INFO, RAW )
		{
		}

		//...........................................................

		protected override UIElement ViewerControl_ {
			get {
				var game_data  = GameData as Data;
				if( game_data != null ) return new Differ(game_data.EXML, EXML);
				else                    return new Viewer{Text = EXML};
			}
		}

		//...........................................................

		public MBINFile MBIN {
			get {
				if( m_mbin == null ) {
					var mbin = new MBINFile(Raw, true);
					if( mbin.Load() && mbin.Header.IsValid ) {
						m_mbin = mbin;
					}
				}
				return m_mbin;
			}
		}

		public string EXML {
			get {
				if( m_exml == null && MBIN != null ) {
					m_exml  = EXmlFile.WriteTemplate(MBIN.GetData());
				}
				return m_exml; 
			}
		}

	}
}

//=============================================================================
