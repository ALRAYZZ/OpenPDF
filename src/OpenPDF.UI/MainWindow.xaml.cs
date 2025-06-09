using OpenPDF.Core;
using Patagames.Pdf.Net;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenPDF.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			PdfCommon.Initialize();
			LoadPdf(@"C:\Users\Usuario\Desktop\testOpenPDF.pdf");
		}

		private void LoadPdf(string filePath)
		{
			try
			{
				using (var loader = new PdfDocumentLoader(filePath))
				{
					if (loader.PageCount > 0)
					{
						using (var page = loader.GetPage(0))
						{
							// Calculate rendering size based on page dimensions and 96 DPI
							int width = (int)(page.Width * 96 / 72); // Convert points to pixels
							int height = (int)(page.Height * 96 / 72);

							// Create a bitmap to render into
							using (var bitmap = new Bitmap(width, height))
							using (var graphics = Graphics.FromImage(bitmap))
							{
								// Get device context (hdc) from graphics
								var hdc = graphics.GetHdc();
								try
								{
									// Render the page to the bitmap's hdc
									page.Render(hdc, 0, 0, width, height, Patagames.Pdf.Enums.PageRotate.Normal, Patagames.Pdf.Enums.RenderFlags.FPDF_NONE);
								}
								finally
								{
									graphics.ReleaseHdc(hdc);
								}
								// Convert bitmap to WPF BitmapImage
								PdfImage.Source = BitmapToImageSource(bitmap);
							}

						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading PDF: {ex.Message}");
			}
		}

		private BitmapImage BitmapToImageSource(Bitmap bitmap)
		{
			using (var memoryStream = new System.IO.MemoryStream())
			{
								bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
				memoryStream.Position = 0;
				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memoryStream;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				return bitmapImage;
			}
		}
		
	}
}