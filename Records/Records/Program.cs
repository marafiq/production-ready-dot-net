// See https://aka.ms/new-console-template for more information

using Spectre.Console;

AnsiConsole.MarkupLine("[bold green]Hi, My name is record, I was born in C# 9.[/]");
var size = 5;
var names = new Name[size];

for (var i = 0; i < size; i++) names[i] = new Name(Faker.Name.Last(), Faker.Name.First());

try
{
    //to sort implement IComparable - seen below
    foreach (var name in names.OrderByDescending(x => x))
    {
        //create copied with with
        var anotherName = name with { };
        AnsiConsole.WriteLine(anotherName.ToString());
        AnsiConsole.WriteLine(name.ToString());
    };
    
    //deconstruction
    foreach (var (lastName, firstName) in names)
    {
        AnsiConsole.WriteLine($"{firstName}, {lastName}");
    }
}
catch (Exception e)
{
    AnsiConsole.WriteException(e);
}

record Name(string LastName, string FirstName) : IComparable<Name> 
{
    public int CompareTo(Name? other)
    {
        return other is null ? 1 : string.Compare(ToString(), other.ToString(), StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return $"{LastName}, {FirstName}";
    }
}
