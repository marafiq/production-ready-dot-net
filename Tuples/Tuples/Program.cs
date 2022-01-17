﻿using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Spectre.Console;

Console.WriteLine("Hello, World! I am Tuples.");
var bookService = new BookService();
//get me max, min prices of all books belong to a category.
var thrillerPriceAggregates = bookService.CalculatePriceAggregatesBy(BookCategory.Thriller);

//Notice how we can use properties of tuple by Name hence Named Tuples
Console.WriteLine(
    $"{nameof(thrillerPriceAggregates)} is {thrillerPriceAggregates.MaxPrice} {thrillerPriceAggregates.MinPrice}");

//tuples support deconstruction - notice variable does not need to match, and types in inferred 
var (maxPriceOfBook, minPriceOfBook) = thrillerPriceAggregates;
Console.WriteLine($"Value Tuple Deconstruction {maxPriceOfBook} {minPriceOfBook}");

//Discard one value but project first value
var (maxPrice, _) = thrillerPriceAggregates;
Console.WriteLine($"Value Tuple Deconstruction with discard {maxPrice}");

//Use existing variable and project value on it
double x = 500.00;
double y = 100.00;
(x, y) = thrillerPriceAggregates;
Console.WriteLine($"Project tuple values on existing variables {x + y}");

unsafe
{
    var size = sizeof((double, double));
    Console.WriteLine(
        $"Price aggregates of Thriller are (Max,Min): {thrillerPriceAggregates}  And size of tuple is {size} bytes");
    Console.WriteLine("I have 100 books. Lets measure by using old StopWatch");
}


StopWatchBenchmark stopWatchBenchmark = new();
AnsiConsole.WriteLine("xx");
AnsiConsole.Write(new Markup(
    $"[bold yellow]Stopwatch Info: {nameof(Stopwatch.Frequency)} = {Stopwatch.Frequency}, {nameof(Stopwatch.GetTimestamp)} = {Stopwatch.GetTimestamp()}, {nameof(Stopwatch.IsHighResolution)} = {Stopwatch.IsHighResolution}[/] "));
AnsiConsole.WriteLine();

stopWatchBenchmark.UseAnonymousToGroupBooksWithAggregatesByCategory();
stopWatchBenchmark.UseValueTuplesToGroupBooksWithAggregatesByCategory();
stopWatchBenchmark.UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory();
stopWatchBenchmark.Print();

Console.WriteLine("Do you want to run DotnetBenchmark, then type yes?");
var input = Console.ReadLine();
if (input?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true)
{
    BenchmarkRunner.Run<Benchmarks>();
    Console.WriteLine("Press any key, so program can exit.");
    Console.ReadKey();
}

/// <summary>
/// Benchmark with StopWatch is bad
/// </summary>
class StopWatchBenchmark
{
    BookService _bookService = new BookService();

    Table _table = new Table();

    public StopWatchBenchmark()
    {
        _table.Title = new TableTitle("[maroon]Group 1000 Books With Aggregates[/]");
        _table.AddColumn("Benchmark Name");
        _table.AddColumn("Total ms");
    }


    public void UseAnonymousToGroupBooksWithAggregatesByCategory()
    {
        
        

        var stopWatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var booksWithAggregatesByCategory = _bookService.UseAnonymousToGroupBooksWithAggregatesByCategory();
            foreach (var bookGroup in booksWithAggregatesByCategory)
            {
                Console.WriteLine($"Anonymous Type Group : {bookGroup} ");
            }
        }

        stopWatch.Stop();
        AddTableRow("UseAnonymousToGroupBooksWithAggregatesByCategory", stopWatch.ElapsedMilliseconds);
    }

    public void UseValueTuplesToGroupBooksWithAggregatesByCategory()
    {
        var stopWatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            foreach (var valueTuple in _bookService.UseValueTuplesToGroupBooksWithAggregatesByCategory())
            {
                Console.WriteLine($"Value Tuple Group: {valueTuple}");
            }
        }

        stopWatch.Stop();
        AddTableRow("UseValueTuplesToGroupBooksWithAggregatesByCategory", stopWatch.ElapsedMilliseconds);
    }

    public void UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory()
    {
        

        var stopWatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            foreach (var refTuple in _bookService.UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory())
            {
                Console.WriteLine($"Reference Tuple Group: {refTuple}");
            }
        }

        stopWatch.Stop();
        AddTableRow("UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory", stopWatch.ElapsedMilliseconds);
    }

    private void AddTableRow(string benchmarkName, long elapsedTicks)
    {
        _table.AddRow($"[green]{benchmarkName}[/]",
            $"[green]{elapsedTicks}[/]");
    }

    public void Print()
    {
        AnsiConsole.Write(_table);
    }
}

[MemoryDiagnoser()]
public class Benchmarks
{
    private readonly Consumer _consumer = new Consumer();
    private BookService _bookService;
    private int _sizeOfBooksList = 5000;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _bookService = new BookService(_sizeOfBooksList);
    }

    [Benchmark]
    public void UseAnonymousToGroupBooksWithAggregatesByCategory()
    {
        var booksWithAggregatesByCategory = _bookService.UseAnonymousToGroupBooksWithAggregatesByCategory();
        booksWithAggregatesByCategory.Consume(_consumer);
    }

    [Benchmark]
    public void UseValueTuplesToGroupBooksWithAggregatesByCategory()
    {
        var valueTuple = _bookService.UseValueTuplesToGroupBooksWithAggregatesByCategory();
        valueTuple.Consume(_consumer);
    }

    [Benchmark]
    public void UseValueTuplesToGroupBooksWithAggregatesByCategoryAndBoxIt()
    {
        var valueTuple = _bookService.UseValueTuplesToGroupBooksWithAggregatesByCategory();
        foreach (var tuple in valueTuple)
        {
            object o = tuple.Category;
            object o1 = tuple.MaxPrice;
            _consumer.Consume(o);
        }
    }

    [Benchmark(Baseline = true)]
    public void UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory()
    {
        var refTuple = _bookService.UseReferenceTypeTuplesToGroupBooksWithAggregatesByCategory();
        refTuple.Consume(_consumer);
    }
}

public class BookService
{
    private readonly int _sizeOfBooksList;
    private readonly List<Book> _books = new();

    public BookService(int sizeOfBooksList = 1000)
    {
        _sizeOfBooksList = sizeOfBooksList;
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
                    Category = category,
                    MinPrice = groupedBooks.Min(b => b.Price),
                    AvgPrice = groupedBooks.Average(b => b.Price),
                    MaxPrice = groupedBooks.Max(b => b.Price),
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
                    Category: category,
                    MinPrice: groupedBooks.Min(b => b.Price),
                    AvgPrice: groupedBooks.Average(b => b.Price),
                    MaxPrice: groupedBooks.Max(b => b.Price),
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
                    category,
                    groupedBooks.Min(b => b.Price),
                    groupedBooks.Average(b => b.Price),
                    groupedBooks.Max(b => b.Price),
                    groupedBooks
                );
            });

        return booksWithAggregatesByCategory;
    }


    List<Book> SeedBooks()
    {
        List<Book> books = new();
        for (var i = 0; i < _sizeOfBooksList; i++)
        {
            Book book = new($"100-100-{i}", BookCategory.Thriller, "about", "abstract", 100.99 + i);
            books.Add(book);
        }

        return books;
    }
}


public class Book
{
    public string Isbn { get; set; }
    public BookCategory BookCategory { get; set; }
    public string About { get; set; }
    public string AbstractDescription { get; set; }
    public double Price { get; set; }

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

/// <summary>
/// Common Stats of a Book
/// </summary>
struct BookStats
{
    public BookStats(int totalChapters, int totalPages)
    {
        TotalChapters = totalChapters;
        TotalPages = totalPages;
    }

    public int TotalChapters { get; set; }
    public int TotalPages { get; set; }

    public override string ToString()
    {
        return $"{nameof(TotalChapters)}: {TotalChapters}, {nameof(TotalPages)}:{TotalPages}";
    }
}