using System.CommandLine;
using DbUp.Builder;
using DbUp.ScriptProviders;
using Microsoft.VisualBasic;

namespace dbup.dotnet.tool;

public enum TransactionSetting
{
    None,
    PerScript,
    Single
}
class Program
{
    static void Main(string[] args)
    {
        var rootCommand = new RootCommand();
        var cnstrArgument = new Argument<string>("connection-string","Connection string for the target database.");
        var configOption = new Option<FileInfo>("--config-file",()=>new FileInfo(".dbup/config.yaml"), "The configuration yaml file.");

        rootCommand.AddArgument(cnstrArgument);
        rootCommand.AddGlobalOption(configOption);

        var upgradeCommand = new Command("upgrade","Upgrades an existing database by applying outstanding migrations.");

        var createCommand = new Command("create","Creates a database and applies migrations.");
        rootCommand.AddCommand(createCommand);
        rootCommand.AddCommand(upgradeCommand);
        rootCommand.Invoke(args);
    }
    static Task HandleUpgrade(string ConnectionString,FileInfo configFile)
    {
        var config = Configuration.CreateFromFile(configFile);
        if(!config.Provider.TryValidateConnectionString(ConnectionString)) throw new Exception($"Invalid connection string for provider {config.Provider}");

        var upgrader = new DbUp.Builder.UpgradeEngineBuilder();
        var fileScriptOptions = new FileSystemScriptOptions();
        
        var scriptProvider = new FileSystemScriptProvider(config.Scripts.First().Folder)

    }

    static Configuration LoadConfig(FileInfo configFile)
    {
        using var configReader = (configFile.Exists)?configFile.OpenText():throw new FileNotFoundException($"Could not find the config file at path: {configFile.FullName}");
        var ser = new YamlDotNet.Serialization.Deserializer();
        return ser.Deserialize<Configuration>(configReader);
    }
}
