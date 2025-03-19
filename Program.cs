using System.CommandLine;

namespace VoldeligCLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Voldelig CLI tool");

            var initCommand = new Command("init", "Initialize a new Voldelig project");

            initCommand.SetHandler(() =>
            {
                Console.WriteLine("Initializing Voldelig project...");
                // Add your initialization logic here
                Console.WriteLine("Voldelig project initialized successfully!");
            });

            rootCommand.AddCommand(initCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
