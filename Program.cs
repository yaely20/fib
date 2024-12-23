using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
  static async Task<int> Main(string[] args)
  {
    var rootCommand = new RootCommand("CLI tool for bundling code files and creating response files.");

    // Command: bundle
    var languageOption = new Option<string>(new[] { "-l", "--language" }, "Comma-separated programming languages to include (e.g., cs, py). Use 'all' for all languages.") { IsRequired = true };
    var outputOption = new Option<string>(new[] { "-o", "--output" }, "Path to the output file where the bundle will be saved.") { IsRequired = true };
    var noteOption = new Option<bool>(new[] { "-n", "--note" }, "Include file source as comments.");
    var sortOption = new Option<string>(new[] { "-s", "--sort" }, () => "name", "Sort files by 'name' or 'type' (default: 'name').");
    var removeEmptyOption = new Option<bool>(new[] { "-r", "--remove-empty-lines" }, "Remove empty lines from source files.");
    var authorOption = new Option<string>(new[] { "-a", "--author" }, "Add author's name as a comment in the output file.");

    var bundleCommand = new Command("bundle", "Combine multiple code files into one.")
        {
            languageOption,
            outputOption,
            noteOption,
            sortOption,
            removeEmptyOption,
            authorOption
        };

    bundleCommand.SetHandler(async (language, output, note, sort, removeEmptyLines, author) =>
    {
      await Task.Run(() => HandleBundle(language, output, note, sort, removeEmptyLines, author));
    },
    languageOption, outputOption, noteOption, sortOption, removeEmptyOption, authorOption);

    // Command: create-rsp
    var rspOutputOption = new Option<string>(new[] { "-o", "--output" }, "Path to save the response file.") { IsRequired = true };

    var createRspCommand = new Command("create-rsp", "Create a response file with pre-configured options for the bundle command.")
        {
            rspOutputOption
        };

    createRspCommand.SetHandler(async (output) =>
    {
      await Task.Run(() => HandleCreateRsp(output));
    },
    rspOutputOption);

    rootCommand.AddCommand(bundleCommand);
    rootCommand.AddCommand(createRspCommand);

    return await rootCommand.InvokeAsync(args);
  }

  private static void HandleBundle(string language, string output, bool note, string sort, bool removeEmptyLines, string author)
  {
    try
    {
      Console.WriteLine("Running bundle command...");

      var languages = language.Split(',').Select(lang => lang.Trim()).ToList();
      if (!languages.Contains("all") && languages.Any(lang => !new[] { "cs", "py", "java", "js", "go" }.Contains(lang)))
      {
        Console.WriteLine("Error: Invalid language specified.");
        return;
      }

      if (!Directory.Exists(Directory.GetCurrentDirectory()))
      {
        Console.WriteLine("Error: Source directory does not exist.");
        return;
      }

      var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
          .Where(file => !file.Contains("bin") && !file.Contains("debug"));

      if (!languages.Contains("all"))
      {
        var extensions = languages.Select(lang => $".{lang}");
        files = files.Where(file => extensions.Contains(Path.GetExtension(file)));
      }

      files = sort == "type" ? files.OrderBy(file => Path.GetExtension(file)) : files.OrderBy(file => Path.GetFileName(file));

      var sb = new StringBuilder();
      if (!string.IsNullOrEmpty(author))
      {
        sb.AppendLine($"// Author: {author}");
      }

      foreach (var file in files)
      {
        if (note)
        {
          sb.AppendLine($"// Source: {file}");
        }

        var content = File.ReadAllLines(file);
        if (removeEmptyLines)
        {
          content = content.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        }

        sb.AppendLine(string.Join(Environment.NewLine, content));
        sb.AppendLine(); // Add spacing between files
      }

      File.WriteAllText(output, sb.ToString());
      Console.WriteLine($"Bundle created at {output}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
  }

  private static void HandleCreateRsp(string output)
  {
    try
    {
      var options = new[]
      {
                "--language",
                "--output",
                "--note",
                "--sort",
                "--remove-empty-lines",
                "--author"
            };

      var responses = new StringBuilder();
      foreach (var option in options)
      {
        Console.WriteLine($"Enter value for {option}:");
        var value = Console.ReadLine();
        responses.AppendLine($"{option} {value}");
      }

      File.WriteAllText(output, responses.ToString());
      Console.WriteLine($"Response file created at {output}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error writing response file: {ex.Message}");
    }
  }
}
