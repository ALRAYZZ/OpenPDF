using Patagames.Pdf.Net;

namespace OpenPDF.Core
{
	public class PdfDocumentLoader : IDisposable
	{
		private PdfDocument _document;

		public PdfDocumentLoader(string filePath)
		{
			try
			{
				_document = PdfDocument.Load(filePath);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to load PDF: {ex.Message}", ex);
			}
		}

		// PageCount property to get the number of pages in the PDF document if null we set it to 0
		public int PageCount => _document?.Pages.Count ?? 0;

		public PdfPage GetPage(int pageNumber)
		{
			if (_document == null || pageNumber < 0 || pageNumber >= _document.Pages.Count)
			{
				throw new ArgumentException("Invalid page number.");
			}
			return _document.Pages[pageNumber];
		}
		public void Dispose()
		{
			_document?.Dispose();
		}
	}
}
