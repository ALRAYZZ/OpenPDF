using OpenPDF.Core;
using Patagames.Pdf.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenPDF.UI.Views
{
	
	public partial class PrintPreviewWindow : Window
	{
		private readonly PdfDocumentLoader _loader;
		private readonly ObservableCollection<BitmapImage> _previews = new ObservableCollection<BitmapImage>();
		public PrintPreviewWindow(PdfDocumentLoader loader)
		{
			InitializeComponent();
			_loader = loader;
			PagePreviews.ItemsSource = _previews;
			GeneratePreviews();
		}

		private void GeneratePreviews()
		{
			try
			{
				for (int i = 0; i < _loader.PageCount; i++)
				{
					using var page = _loader.GetPage(i);
					int targetDpi = 300; // Target DPI for rendering, can be adjusted as needed
					int width = (int)(page.Width * targetDpi / 72); // Convert points to pixels
					int height = (int)(page.Height * targetDpi / 72); // Convert points to pixels

					using var bitmap = new Bitmap(width, height);
					using var graphics = Graphics.FromImage(bitmap);
					graphics.Clear(System.Drawing.Color.White); // Clear the bitmap with white background
					var hdc = graphics.GetHdc();
					try
					{
						var flags = RenderFlags.FPDF_LCD_TEXT | RenderFlags.FPDF_ANNOT | RenderFlags.FPDF_PRINTING; // Set render flags for text and annotations
						page.Render(hdc, 0, 0, width, height, PageRotate.Normal, flags);
					}
					finally
					{
						graphics.ReleaseHdc(hdc);
					}
					_previews.Add(BitmapToImageSource(bitmap));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error generating previews: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Close();
			}
		}

		private BitmapImage BitmapToImageSource(Bitmap bitmap)
		{
			using var memoryStream = new System.IO.MemoryStream();
			bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
			memoryStream.Position = 0;
			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memoryStream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Load the image into memory
			bitmapImage.EndInit();
			return bitmapImage;
		}


		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close(); // Close the print preview window
		}
		private void PrintButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var printDialog = new PrintDialog();
				if (printDialog.ShowDialog() == true)
				{
					foreach (var image in _previews)
					{
						var visual = new DrawingVisual();
						using (var dc = visual.RenderOpen())
						{
							dc.DrawImage(image, new Rect(0, 0, printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight));
						}
						printDialog.PrintVisual(visual, "PDF page");
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error printing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			Close(); // Close the print preview window after printing
		}
	}
}
