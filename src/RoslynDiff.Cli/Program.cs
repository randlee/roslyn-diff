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
});

return await app.RunAsync(args);
