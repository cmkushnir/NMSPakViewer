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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//=============================================================================

namespace NMS.PAK.DDS
{
	public partial class Viewer : Image
	{
		static Viewer ()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(Viewer), new FrameworkPropertyMetadata(typeof(Viewer))
			);
		}

		protected NMS.PAK.DDS.Data m_data;

		//...........................................................

		public Viewer ()
		:	base ()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch;
			VerticalAlignment   = VerticalAlignment.Stretch;
		}

		//...........................................................

		protected static PixelFormat PixelFormat ( Pfim.IImage IMAGE )
		{
			switch( IMAGE.Format ) {
				case Pfim.ImageFormat.Rgb8:   return PixelFormats.Gray8;
				case Pfim.ImageFormat.Rgb24:  return PixelFormats.Bgr24;
				case Pfim.ImageFormat.Rgba32: return PixelFormats.Bgra32;
				case Pfim.ImageFormat.R5g5b5:
				case Pfim.ImageFormat.R5g5b5a1: return PixelFormats.Bgr555;
				case Pfim.ImageFormat.R5g6b5:   return PixelFormats.Bgr565;
			}
			return PixelFormats.Default;
		}

		//...........................................................

		public Data ViewerData {
			get {
				return m_data;
			}
			set {
				if( value == m_data ) return;
				m_data = value;

				var image  = m_data?.Image;
				if( image == null ) return;

				Source = BitmapSource.Create(
					image.Width, image.Height, 96.0, 96.0,
					PixelFormat(image), null, image.Data,
					image.Stride
				);
			}
		}
	}
}

//=============================================================================
