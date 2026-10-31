using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public int Year { get; set; }

    [JsonIgnore]
    public decimal Price { get; set; }
}

[JsonSerializable(typeof(List<Book>))]
public partial class BookJsonContext : JsonSerializerContext
{
}

public static class Program
{
    private const string FilePath = "books.json";

    public static void Main()
    {
        List<Book> books = new()
        {
            new Book { Title = "Vepkhistkaosani", Author = "Shota Rustaveli", Year = 1200, Price = 25.50m },
            new Book { Title = "Clean Code", Author = "Robert C. Martin", Year = 2008, Price = 40.00m },
            new Book { Title = "The Pragmatic Programmer", Author = "Andrew Hunt", Year = 1999, Price = 35.75m }
        };

        JsonSerializerOptions options = new() { WriteIndented = true, TypeInfoResolver = BookJsonContext.Default };
        string json = JsonSerializer.Serialize(books, options);
        File.WriteAllText(FilePath, json);
        Console.WriteLine($"Saved {books.Count} books to {FilePath}");
        Console.WriteLine();

        string fileContent = File.ReadAllText(FilePath);
        List<Book> loadedBooks = JsonSerializer.Deserialize<List<Book>>(fileContent, options);

        Console.WriteLine("Books loaded from file:");
        foreach (Book book in loadedBooks)
        {
            Console.WriteLine($"{book.Title} by {book.Author}, {book.Year}, Price: {book.Price}");
        }

        Console.WriteLine();

        Console.WriteLine($"Price after reading: {loadedBooks[0].Price}");
    }
}