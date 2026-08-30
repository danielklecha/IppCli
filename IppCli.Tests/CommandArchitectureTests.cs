using System.Reflection;
using SharpIpp.Protocol.Models;
using Spectre.Console.Cli;

namespace IppCli.Tests;

[TestClass]
public class CommandArchitectureTests
{
    [TestMethod]
    public void AllCommands_UseBaseSettingsGeneric()
    {
        var asm = typeof(Program).Assembly;
        var commandTypes = asm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t))
            .ToList();

        Assert.AreEqual(61, commandTypes.Count);

        foreach (var cmd in commandTypes)
        {
            var baseType = cmd.BaseType;
            Assert.IsNotNull(baseType);
            Assert.IsTrue(baseType.IsGenericType, $"{cmd.Name} should inherit from AsyncCommand<TSettings>");

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

            Assert.IsTrue(isBaseSettings, $"{settingsType.Name} used in {cmd.Name} should inherit from BaseSettings<TRequest>");
        }
    }
}
