using System;
using System.Collections.Generic;
using System.Linq;

public record Grade(string Course, int Score);

public class Gradebook
{
    private const int HistorySize = 5;

    private readonly Dictionary<string, List<Grade>> _students =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _courses =
        new(StringComparer.OrdinalIgnoreCase) { ".NET", "DB", "Math" };

    private readonly Stack<(string Student, Grade Grade)> _undoStack = new();
    private readonly Queue<string> _history = new();

    public string AddScore(string name, string course, int score)
    {
        name = name.Trim();
        course = course.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return "Invalid name";

        string canonicalCourse = FindCourse(course);
        if (canonicalCourse == null)
            return $"Unknown course: {course}";

        if (score < 0 || score > 100)
            return $"Invalid score: {score}";

        string studentKey = FindStudentKey(name) ?? name;
        if (!_students.ContainsKey(studentKey))
            _students[studentKey] = new List<Grade>();

        var grade = new Grade(canonicalCourse, score);
        _students[studentKey].Add(grade);

        _undoStack.Push((studentKey, grade));
        Log($"add {studentKey} {canonicalCourse} {score}");

        return $"Added: {studentKey} {canonicalCourse} {score}";
    }

    public string Undo()
    {
        if (_undoStack.Count == 0)
            return "Nothing to undo";

        var (student, grade) = _undoStack.Pop();
        List<Grade> list = _students[student];

        int index = list.FindLastIndex(g => g == grade);
        if (index >= 0)
            list.RemoveAt(index);

        if (list.Count == 0)
            _students.Remove(student);

        Log($"undo {student} {grade.Course} {grade.Score}");
        return $"Undone: {student} {grade.Course} {grade.Score}";
    }

    public void PrintTable()
    {
        Console.WriteLine($"{"Name",-8}{"Count",6}{"Average",9}{"Max",5}  Last 3");

        foreach (var (name, grades) in _students.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            int[] scores = grades.Select(g => g.Score).ToArray();
            decimal avg = Average(scores);
            int max = scores.Max();
            int[] last = scores[^Math.Min(3, scores.Length)..];

            Console.WriteLine($"{name,-8}{scores.Length,6}{avg,9:F2}{max,5}  {string.Join(" ", last)}");
        }
    }

    public void PrintStudent(string name)
    {
        string key = FindStudentKey(name.Trim());
        if (key == null)
        {
            Console.WriteLine($"{name}: not found");
            return;
        }

        var parts = _students[key]
            .GroupBy(g => g.Course)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => $"{g.Key}={Average(g.Select(x => x.Score)):F2}");

        Console.WriteLine($"{key}: {string.Join(", ", parts)}");
    }

    public void PrintTop3()
    {
        var top = _students
            .Select(kv => (Name: kv.Key, Avg: Average(kv.Value.Select(g => g.Score))))
            .OrderByDescending(x => x.Avg)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Take(3);

        Console.WriteLine("Top 3:");
        foreach (var t in top)
            Console.WriteLine($"  {t.Name,-8}{t.Avg,5:F2}");
    }

    public void PrintCourseReport()
    {
        foreach (string course in _courses.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
        {
            var entries = _students
                .Select(kv => (Name: kv.Key, Scores: kv.Value
                    .Where(g => g.Course == course)
                    .Select(g => g.Score)
                    .ToList()))
                .Where(x => x.Scores.Count > 0)
                .ToList();

            if (entries.Count == 0)
            {
                Console.WriteLine($"{course,-6} students: 0");
                continue;
            }

            decimal overall = Average(entries.SelectMany(x => x.Scores));

            var best = entries
                .Select(x => (x.Name, Avg: Average(x.Scores)))
                .OrderByDescending(x => x.Avg)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .First();

            Console.WriteLine(
                $"{course,-6} students: {entries.Count}, average: {overall:F2}, best: {best.Name} ({best.Avg:F2})");
        }
    }

    public void PrintHistory()
    {
        if (_history.Count == 0)
        {
            Console.WriteLine("History is empty.");
            return;
        }

        Console.WriteLine("History:");
        foreach (string entry in _history)
            Console.WriteLine($"  {entry}");
    }

    public void PrintDistribution()
    {
        var distribution = new SortedDictionary<char, int>
        {
            ['A'] = 0, ['B'] = 0, ['C'] = 0, ['D'] = 0, ['E'] = 0, ['F'] = 0
        };

        foreach (int score in _students.Values.SelectMany(list => list.Select(g => g.Score)))
            distribution[LetterFor(score)]++;

        Console.WriteLine("Distribution: " +
            string.Join(", ", distribution.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    private static char LetterFor(int score) => score switch
    {
        >= 91 => 'A',
        >= 81 => 'B',
        >= 71 => 'C',
        >= 61 => 'D',
        >= 51 => 'E',
        _ => 'F'
    };

    private static decimal Average(IEnumerable<int> values)
    {
        List<int> list = values.ToList();
        return (decimal)list.Sum() / list.Count;
    }

    private string FindCourse(string course) =>
        _courses.FirstOrDefault(c => string.Equals(c, course, StringComparison.OrdinalIgnoreCase));

    private string FindStudentKey(string name) =>
        _students.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));

    private void Log(string entry)
    {
        _history.Enqueue(entry);
        if (_history.Count > HistorySize)
            _history.Dequeue();
    }
}

public static class Program
{
    public static void Main()
    {
        var gradebook = new Gradebook();
        LoadTestData(gradebook);

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1 - Add score");
            Console.WriteLine("2 - Table");
            Console.WriteLine("3 - Student search");
            Console.WriteLine("4 - Top 3 students");
            Console.WriteLine("5 - Undo");
            Console.WriteLine("6 - Course report");
            Console.WriteLine("7 - History (last 5)");
            Console.WriteLine("0 - Exit");
            Console.Write("Choice: ");

            string choice = (Console.ReadLine() ?? "").Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    string name = Ask("Name: ");
                    string course = Ask("Course: ");
                    if (!int.TryParse(Ask("Score: "), out int score))
                    {
                        Console.WriteLine("Invalid number.");
                        break;
                    }
                    Console.WriteLine(gradebook.AddScore(name, course, score));
                    break;

                case "2":
                    gradebook.PrintTable();
                    break;

                case "3":
                    gradebook.PrintStudent(Ask("Student name: "));
                    break;

                case "4":
                    gradebook.PrintTop3();
                    break;

                case "5":
                    Console.WriteLine(gradebook.Undo());
                    break;

                case "6":
                    gradebook.PrintCourseReport();
                    break;

                case "7":
                    gradebook.PrintHistory();
                    break;

                case "0":
                    gradebook.PrintDistribution();
                    return;

                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private static string Ask(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? "";
    }

    private static void LoadTestData(Gradebook gradebook)
    {
        string[] seed =
        {
            "Ana;.NET;95", "Ana;DB;88", "Ana;Math;100", "Gio;.NET;72", "Gio;DB;81",
            "luka;.NET;65", "Nino;Math;91", "Nino;.NET;67", "Nino;DB;79", "Ana;.NET;98"
        };

        foreach (string line in seed)
        {
            string[] parts = line.Split(';');
            gradebook.AddScore(parts[0], parts[1], int.Parse(parts[2]));
        }
    }
}