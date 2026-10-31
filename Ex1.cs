using System;
using System.Collections.Generic;

public interface IDigital
{
    string Url { get; }
}

public interface IBorrowable
{
    bool IsBorrowed { get; }
    bool Borrow();
    void Return();
}

public abstract class LibraryItem
{
    public string Title { get; }

    protected LibraryItem(string title)
    {
        Title = title;
    }

    public virtual string Info() => Title;
}

public abstract class PhysicalItem : LibraryItem, IBorrowable
{
    public bool IsBorrowed { get; private set; }

    protected PhysicalItem(string title) : base(title) { }

    public abstract int LoanDays { get; }

    public bool Borrow()
    {
        if (IsBorrowed)
            return false;

        IsBorrowed = true;
        return true;
    }

    public void Return()
    {
        IsBorrowed = false;
    }

    public override string Info()
    {
        string info = $"{Title}: {LoanDays} days";
        return IsBorrowed ? info + " [borrowed]" : info;
    }
}

public class Book : PhysicalItem
{
    public Book(string title) : base(title) { }

    public override int LoanDays => 21;
}

public class Dvd : PhysicalItem
{
    public Dvd(string title) : base(title) { }

    public override int LoanDays => 7;

    public override string Info() => base.Info() + " (DVD)";
}

public class EBook : LibraryItem, IDigital
{
    public string Url { get; }

    public EBook(string title, string url) : base(title)
    {
        Url = url;
    }

    public override string Info() => Title + ": online only";
}

public static class Demo
{
    public static void Run()
    {
        List<LibraryItem> items =
        [
            new Book("Clean Code"),
            new Dvd("Inception"),
            new EBook("C# Basics", "https://books.example.com/csharp")
        ];

        foreach (LibraryItem item in items)
            Console.WriteLine(item.Info());

        Console.WriteLine();
        foreach (LibraryItem item in new[] { items[0], items[0], items[2], items[1] })
        {
            if (item is IBorrowable b)
                Console.WriteLine(b.Borrow()
                    ? $"{item.Title}: borrowed"
                    : $"{item.Title}: already borrowed");
            else
                Console.WriteLine($"{item.Title}: cannot be borrowed");
        }

        Console.WriteLine();
        foreach (LibraryItem item in items)
            Console.WriteLine(item.Info());

        ((IBorrowable)items[0]).Return();
        Console.WriteLine($"After return: {items[0].Info()}");

        foreach (LibraryItem item in items)
            if (item is IDigital d)
                Console.WriteLine($"Download: {d.Url}");
    }
}

public static class Program
{
    public static void Main()
    {
        Demo.Run();
    }
}