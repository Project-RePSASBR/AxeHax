using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace AxeHax.CLI;

#nullable disable

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class CommandLineOptions
{
    [Option('i', "input", Required = true, HelpText = "The input EBOOT.elf to patch")]
    public string InputFile { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output EBOOT.elf to save")]
    public string OutputFile { get; set; }
}