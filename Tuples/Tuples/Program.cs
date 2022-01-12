unsafe
{
    var bookService = new BookService();
    //get me max, min prices of all books belong to a category.
    var thrillerPriceAggregates = bookService.CalculatePriceAggregatesBy(BookCategory.Thriller);
    Console.WriteLine(
        $"Price aggregates of Thriller are (Max,Min): {thrillerPriceAggregates}  And size of tuple is {sizeof((BookCategory, double, double))} bytes");
    var booksWithAggregatesByCategory = bookService.UseAnonymousToGroupBooksWithAggregatesByCategory();
    foreach (var bookGroup in booksWithAggregatesByCategory)
    {
        Console.WriteLine($"Anonymous Type Group : {bookGroup} ");
    }
    
    foreach (var valueTuple in bookService.UseValueTuplesToGroupBooksWithAggregatesByCategory())
    {
        Console.WriteLine($"Value Tuple Group: {valueTuple}");
    }
    
    foreach (var refTuple in bookService.UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory())
    {
        Console.WriteLine($"Value Tuple Group: {refTuple}");
    }

    Console.WriteLine("Hello, World! I am tuples.");
}

public class BookService
{
    private readonly List<Book> _books;

    public BookService()
    {
        _books = SeedBooks();
    }

    public (double MaxPrice, double MinPrice) CalculatePriceAggregatesBy(BookCategory bookCategory)
    {
        var categoryBooks = _books.Where(b => b.BookCategory == bookCategory).ToList();
        return (categoryBooks.Max(b => b.Price), categoryBooks.Min(b => b.Price));
    }

    public IEnumerable<dynamic> UseAnonymousToGroupBooksWithAggregatesByCategory()
    {
        var booksWithAggregatesByCategory = _books.GroupBy(g => g.BookCategory,
            (category, books) =>
            {
                var groupedBooks = books.ToList();
                return new
                {
                    Category = category, MinPrice = groupedBooks.Min(b => b.Price),
                    MaxPrice = groupedBooks.Max(b => b.Price),
                    AvgPrice = groupedBooks.Average(b => b.Price),
                    Books = groupedBooks
                };
            });

        return booksWithAggregatesByCategory;
    }

    public IEnumerable<(BookCategory Category, double MinPrice, double MaxPrice, double AvgPrice, List<Book> Books)>
        UseValueTuplesToGroupBooksWithAggregatesByCategory()
    {
        var booksWithAggregatesByCategory = _books.GroupBy(g => g.BookCategory,
            (category, books) =>
            {
                var groupedBooks = books.ToList();
                return (
                    Category: category, MinPrice: groupedBooks.Min(b => b.Price),
                    MaxPrice: groupedBooks.Max(b => b.Price),
                    AvgPrice: groupedBooks.Average(b => b.Price),
                    Books: groupedBooks
                );
            });

        return booksWithAggregatesByCategory;
    }

    public IEnumerable<System.Tuple<BookCategory, double, double, double, List<Book>>>
        UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory()
    {
        var booksWithAggregatesByCategory = _books.GroupBy(g => g.BookCategory,
            (category, books) =>
            {
                var groupedBooks = books.ToList();
                return Tuple.Create(
                    category, groupedBooks.Min(b => b.Price),
                    groupedBooks.Max(b => b.Price),
                    groupedBooks.Average(b => b.Price),
                    groupedBooks
                );
            });

        return booksWithAggregatesByCategory;
    }

    List<Book> SeedBooks()
    {
        List<Book> books = new();
        for (var i = 0; i < 1000; i++)
        {
            Book book = new($"100-100-{i}", BookCategory.Thriller, "about", "abstract", 100.99 + i);
            books.Add(book);
        }

        return books;
    }
}


public class Book
{
    public string Isbn { get; }
    public BookCategory BookCategory { get; }
    public string About { get; }
    public string AbstractDescription { get; }
    public double Price { get; }

    public Book(string isbn, BookCategory bookCategory, string about, string abstractDescription, double price)
    {
        Isbn = isbn;
        BookCategory = bookCategory;
        About = about;
        AbstractDescription = abstractDescription;
        Price = price;
    }
}

public enum BookCategory
{
    Thriller,
    Horror,
    Comedy,
    Nerdy
}