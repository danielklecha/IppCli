using System.Reflection;
using SharpIpp.Protocol.Models;
using Spectre.Console.Cli;
using Xunit;
using Xunit.Abstractions;

namespace IppCli.Tests;

public class CommandArchitectureTests
{
    private readonly ITestOutputHelper _output;

    public CommandArchitectureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AllCommands_UseBaseSettingsGeneric()
    {
        var asm = typeof(Program).Assembly;
        var commandTypes = asm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t))
            .ToList();

        Assert.Equal(61, commandTypes.Count);

        foreach (var cmd in commandTypes)
        {
            var baseType = cmd.BaseType;
            Assert.NotNull(baseType);
            Assert.True(baseType.IsGenericType, $"{cmd.Name} should inherit from AsyncCommand<TSettings>");

            var settingsType = baseType.GetGenericArguments()[0];
            var settingsBase = settingsType.BaseType;

            var isBaseSettings = false;
            while (settingsBase != null)
            {
                if (settingsBase.IsGenericType && settingsBase.Name == "BaseSettings`1")
                {
                    isBaseSettings = true;
                    break;
                }
                settingsBase = settingsBase.BaseType;
            }

            Assert.True(isBaseSettings, $"{settingsType.Name} used in {cmd.Name} should inherit from BaseSettings<TRequest>");
        }
    }
}
