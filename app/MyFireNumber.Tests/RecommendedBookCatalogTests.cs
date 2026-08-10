using MyFireNumber.Core.Books;

namespace MyFireNumber.Tests;

public class RecommendedBookCatalogTests
{
    [Fact]
    public void Catalog_MirrorsTheSevenWebRecommendationsWithSecureLinksAndLocalImages()
    {
        var books = new RecommendedBookCatalog().All;

        Assert.Equal(7, books.Count);
        Assert.All(books, book =>
        {
            Assert.False(string.IsNullOrWhiteSpace(book.Title));
            Assert.False(string.IsNullOrWhiteSpace(book.Author));
            Assert.False(string.IsNullOrWhiteSpace(book.Description));
            Assert.EndsWith(".jpg", book.ImageName, StringComparison.Ordinal);
            Assert.True(book.AmazonUri.IsAbsoluteUri);
            Assert.Equal(Uri.UriSchemeHttps, book.AmazonUri.Scheme);
        });
    }
}
