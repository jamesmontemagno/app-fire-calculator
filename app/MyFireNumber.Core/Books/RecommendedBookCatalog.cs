namespace MyFireNumber.Core.Books;

public sealed record RecommendedBook(
    string Id,
    string Title,
    string Author,
    string Description,
    string ImageName,
    Uri AmazonUri);

public interface IRecommendedBookCatalog
{
    IReadOnlyList<RecommendedBook> All { get; }
}

public sealed class RecommendedBookCatalog : IRecommendedBookCatalog
{
    private static readonly IReadOnlyList<RecommendedBook> Books =
    [
        new(
            "i-will-teach-you-to-be-rich",
            "I Will Teach You to Be Rich",
            "Ramit Sethi",
            "A practical six-week program covering banking, saving, budgeting, and investing.",
            "iwillteachyoutoberich.jpg",
            new Uri("https://amzn.to/3N1SrtP")),
        new(
            "money-for-couples",
            "Money for Couples",
            "Ramit Sethi",
            "Guidance for couples combining finances, from joint accounts to big purchases.",
            "moneyforcouples.jpg",
            new Uri("https://amzn.to/4pQ81Hn")),
        new(
            "psychology-of-money",
            "The Psychology of Money",
            "Morgan Housel",
            "Timeless lessons on how history and emotion shape financial decisions.",
            "psychologyofmoney.jpg",
            new Uri("https://amzn.to/3Y74Jn9")),
        new(
            "bogleheads-guide-to-investing",
            "The Bogleheads' Guide to Investing",
            "Taylor Larimore, Mel Lindauer, and Michael LeBoeuf",
            "A practical guide to low-cost index fund investing and long-term wealth building.",
            "bogleheads.jpg",
            new Uri("https://amzn.to/3MXrOWU")),
        new(
            "we-need-to-talk",
            "We Need to Talk: A Memoir About Wealth",
            "Jennifer Risher",
            "A candid memoir about the emotional and social complexities of sudden wealth.",
            "weneedtotalk.jpg",
            new Uri("https://amzn.to/3Y74Ij5")),
        new(
            "die-with-zero",
            "Die with Zero",
            "Bill Perkins",
            "A challenge to optimize life experiences instead of accumulating money never spent.",
            "diewithzero.jpg",
            new Uri("https://amzn.to/3LgBMlK")),
        new(
            "little-book-of-common-sense-investing",
            "The Little Book of Common Sense Investing",
            "John C. Bogle",
            "A classic case for low-cost index funds as the foundation of passive investing.",
            "littlebookofcommonsense.jpg",
            new Uri("https://amzn.to/4pdtMQq"))
    ];

    public IReadOnlyList<RecommendedBook> All => Books;
}
