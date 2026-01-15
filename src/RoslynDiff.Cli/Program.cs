using RoslynDiff.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("roslyn-diff");
    config.SetApplicationVersion("0.1.0");

    config.AddCommand<DiffCommand>("diff")
        .WithDescription("Compare two files or directories and display the differences")
        .WithExample("diff", "old.cs", "new.cs")
        .WithExample("diff", "--format", "json", "before/", "after/")
        .WithExample("diff", "--ignore-whitespace", "file1.cs", "file2.cs");

    config.AddCommand<ClassCommand>("class")
        .WithDescription("Compare specific classes between two files")
        .WithExample("class", "old.cs:Foo", "new.cs:Foo")
        .WithExample("class", "old.cs:Foo", "new.cs:Bar")
        .WithExample("class", "old.cs:Foo", "new.cs", "--match-by", "similarity")
        .WithExample("class", "old.cs", "new.cs", "--match-by", "interface", "--interface", "IRepository")
        .WithExample("class", "old.cs:Foo", "new.cs:Foo", "--output", "json")
        .WithExample("class", "old.cs:Foo", "new.cs:Foo", "--out-file", "result.html", "--output", "html");
});

return await app.RunAsync(args);
