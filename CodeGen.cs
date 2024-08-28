using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;

public class CodeGen : ToolTask
{
    public bool Clean { get; set; }

    [Required]
    public ITaskItem[] Inputs { get; set; }

    [Required]
    public string TargetName { get; set; }

    [Required]
    public string TLogLocation { get; set; }

    public override bool Execute()
    {
        try
        {
            bool append = false;
            foreach (var input in Inputs)
            {
                string inputPath = input.ItemSpec;
                string fullInputPath = input.GetMetadata("FullPath");
                string command = input.GetMetadata("Command");
                string outputsStr = input.GetMetadata("Outputs");
                string additionalInputsStr = input.GetMetadata("AdditionalInputs");
                string dependencyFile = input.GetMetadata("DependencyFile");

                var outputs = outputsStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var additionalInputs = additionalInputsStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                var dependencies = new List<string>();
                try
                {
                    var lines = File.ReadAllLines(dependencyFile);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length != 2)
                            continue;

                        var files = parts[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var file in files)
                            dependencies.Add(file);
                    }
                }
                catch (FileNotFoundException)
                {
                }
                catch (DirectoryNotFoundException)
                {
                }

                var allInputs = new List<string>();
                allInputs.Add(inputPath);
                allInputs.AddRange(dependencies);
                allInputs.AddRange(additionalInputs);

                if (Clean)
                {
                    File.Delete(dependencyFile);
                    foreach (var output in outputs)
                        File.Delete(output);
                }
                else
                {
                    if (OutOfDate(allInputs, outputs))
                    {
                        inputCommand = command;
                        base.Execute();
                    }

                    string commandTLogPath = Path.Combine(TLogLocation, $"{TargetName}.command.1.tlog");
                    string readTLogPath = Path.Combine(TLogLocation, $"{TargetName}.read.1.tlog");
                    string writeTLogPath = Path.Combine(TLogLocation, $"{TargetName}.write.1.tlog");

                    using (StreamWriter writer = new StreamWriter(commandTLogPath, append: append))
                    {
                        writer.WriteLine($"^{fullInputPath}");
                        writer.WriteLine(command);
                    }

                    using (StreamWriter writer = new StreamWriter(readTLogPath, append: append))
                    {
                        writer.WriteLine($"^{fullInputPath}");
                        foreach (var dependency in dependencies)
                            writer.WriteLine(Path.GetFullPath(dependency));
                        foreach (var additionalInput in additionalInputs)
                            writer.WriteLine(Path.GetFullPath(additionalInput));
                    }

                    using (StreamWriter writer = new StreamWriter(writeTLogPath, append: append))
                    {
                        writer.WriteLine($"^{fullInputPath}");
                        foreach (var output in outputs)
                            writer.WriteLine(Path.GetFullPath(output));
                    }

                    append = true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }

        return true;
    }

    private bool OutOfDate(List<string> inputFiles, string[] outputFiles)
    {
        if (outputFiles.Length == 0)
            return false;

        var newestInput = File.GetLastWriteTimeUtc(inputFiles[0]);
        foreach (var inputFile in inputFiles)
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(inputFile);
            if (lastWriteTime > newestInput)
                newestInput = lastWriteTime;
        }

        foreach (var outputFile in outputFiles)
        {
            if (!File.Exists(outputFile))
                return true;

            var lastWriteTime = File.GetLastWriteTimeUtc(outputFile);
            if (lastWriteTime < newestInput)
                return true;
        }

        return false;
    }

    private string inputCommand;

    protected override string ToolName => "cmd.exe";

    protected override string GenerateFullPathToTool() => ToolName;

    protected override string GenerateCommandLineCommands()
    {
        var command = new CommandLineBuilder();
        command.AppendSwitch("/c " + inputCommand);
        return command.ToString();
    }

    protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
    {
        base.LogEventsFromTextOutput(singleLine, MessageImportance.High);
    }
}
