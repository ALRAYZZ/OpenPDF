using Microsoft.Win32;
using OpenPDF.Core;
using OpenPDF.UI.Views;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace OpenPDF.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private PdfDocumentLoader _loader;
		private int _currentPage = -1; // Start with an invalid page index
		private float _zoom = 1.0f; // Default zoom level
		private ObservableCollection<ThumbnailItem> _thumbnails = new ObservableCollection<ThumbnailItem>();

		public MainWindow()
		{
			InitializeComponent();
			PdfCommon.Initialize();
			ThumbnailList.ItemsSource = _thumbnails;
		}

		private void LoadPdf(string filePath)
		{
			try
			{
				_loader?.Dispose(); // Dispose previous loader if exists
				_loader = new PdfDocumentLoader(filePath);
				_currentPage = 0; // Reset to first page
				_zoom = 1.0f; // Reset zoom level
				_thumbnails.Clear(); // Clear previous thumbnails
				for (int i = 0; i < _loader.PageCount; i++)
				{
					_thumbnails.Add(new ThumbnailItem { PageNumber = i, Thumbnail = GenerateThumbnail(i) });
				}
				StatusText.Text = $"Loaded PDF in {filePath} with {_loader.PageCount} pages";
				LoadCurrentPage();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading PDF: {ex.Message}");
				ClearUI();
			}
		}
		private void LoadCurrentPage()
		{
			if (_loader == null || _currentPage < 0 || _currentPage >= _loader.PageCount)
			{
				ClearUI();
				return;
			}

			try
			{
				using var page = _loader.GetPage(_currentPage);

				int targetDpi = 300; // Target DPI for rendering, can be adjusted as needed
				int width = (int)(page.Width * targetDpi / 72 * _zoom); // Convert points to pixels
				int height = (int)(page.Height * targetDpi / 72 * _zoom);

				// Create a bitmap to render into
				using (var bitmap = new Bitmap(width, height))
				using (var graphics = Graphics.FromImage(bitmap))
				{
					// Get device context (hdc) from graphics
					var hdc = graphics.GetHdc();
					try
					{
						// Set render flags for text and annotations
						var flags = RenderFlags.FPDF_LCD_TEXT | RenderFlags.FPDF_ANNOT | RenderFlags.FPDF_GRAYSCALE;
						// Render the page to the bitmap's hdc
						page.Render(hdc, 0, 0, width, height, PageRotate.Normal, flags);
					}
					finally
					{
						graphics.ReleaseHdc(hdc);
					}
					// Convert bitmap to WPF BitmapImage
					PdfImage.Source = BitmapToImageSource(bitmap);
				}

				
				StatusText.Text = $"Page {_currentPage + 1} of {_loader.PageCount}";
				PageInput.Text = (_currentPage + 1).ToString(); // Update page input box
				ThumbnailList.SelectedIndex = _currentPage; // Update thumbnail selection
				PageScrollViewer.ScrollToVerticalOffset(0); // Scroll to top of the page
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error rendering page: {ex.Message}");
				ClearUI();
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
		private BitmapImage GenerateThumbnail(int pageNum)
		{
			try
			{
				using var page = _loader.GetPage(pageNum);
				// Render at 100px width, maintaining aspect ratio
				int thumbWidth = 100;
				float thumbHeight = page.Height / page.Width * thumbWidth; // Calculate height based on aspect ratio
				int width = thumbWidth;
				int height = (int)thumbHeight;

				using var bitmap = new Bitmap(width, height);
				using var graphics = Graphics.FromImage(bitmap);
				{
					var hdc = graphics.GetHdc();
					try
					{
						page.Render(hdc, 0, 0, width, height, Patagames.Pdf.Enums.PageRotate.Normal, Patagames.Pdf.Enums.RenderFlags.FPDF_NONE);
					}
					finally
					{
						graphics.ReleaseHdc(hdc);
					}
					return BitmapToImageSource(bitmap); // Convert to BitmapImage
				}
			}
			catch (Exception)
			{			
				return new BitmapImage(); // Return an empty image on error
			}
		}
		private void ClearUI()
		{
			PdfImage.Source = null;
			StatusText.Text = "No PDF loaded";
			PageInput.Text = string.Empty;
			_thumbnails.Clear(); // Clear thumbnails
			ThumbnailList.SelectedIndex = -1; // Clear selection
		}



		// UI event handlers for buttons
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog { Filter = "PDF Files (*.pdf)|*.pdf" };
			if (dialog.ShowDialog() == true)
			{
				LoadPdf(dialog.FileName);
			}
		}
		private void ZoomInButton_Click(object sender, RoutedEventArgs e)
		{
			_zoom += 0.1f; // Increase zoom level by 10%
			LoadCurrentPage(); // Reload the current page with new zoom
		}
		private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
		{
			_zoom = Math.Max(0.1f, _zoom - 0.1f); // Decrease zoom level by 10%, but not below 10%
			LoadCurrentPage(); // Reload the current page with new zoom
		}
		private void NextPageButton_Click(object sender, RoutedEventArgs e)
		{
			if (_loader != null && _currentPage < _loader.PageCount - 1)
			{
				_currentPage++;
				LoadCurrentPage();
			}
		}
		private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
		{
			if (_currentPage > 0)
			{
				_currentPage--;
				LoadCurrentPage();
			}
		}
		private void ThumbnailList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (ThumbnailList.SelectedItem is ThumbnailItem item)
			{
				_currentPage = item.PageNumber;
				LoadCurrentPage();
			}
		}
		private void PageInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter && int.TryParse(PageInput.Text, out int pageNum) && pageNum >= 1 && pageNum <= _loader?.PageCount)
			{
				_currentPage = pageNum -1; // Convert to zero-based index
				LoadCurrentPage();
			}
		}
		private void PrintButton_Click(object sender, RoutedEventArgs e)
		{
			if (_loader == null)
			{
				MessageBox.Show("No PDF loaded to print.");
				return;
			}
			var printPreview = new PrintPreviewWindow(_loader);
			printPreview.ShowDialog();
		}




		public class ThumbnailItem
		{
			public int PageNumber { get; set; }
			public BitmapImage Thumbnail { get; set; }
		}
	}
}