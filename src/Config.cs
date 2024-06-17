using System.Data;
using System.Text;
using DbUp.Engine;
using DbUp.ScriptProviders;

namespace dbup.dotnet.tool;
public enum DatabaseProvider
{
    sqlserver,
    postgresql,
    mysql,
    sqlite
}
public enum TransactionConfiguration
{
    Single,
    PerScript,
    None
}
public record class ScriptConfiguration
(
    string Folder,
    bool Recursive,
    bool RunAlways,
    string Filter,
    bool MatchFullPath,
    int Order = 100,
    string Encoding = "utf-8"
)
{
}


public record class NamingConfiguration(
    bool UseOnlyFileName,
    bool IncludeBaseFolderName,
    string Prefix
);
public record class JournalConfiguration(
    string Schema,
    string Table
);
public record class Configuration(
    DatabaseProvider Provider,
    string ConnectionString,
    TimeSpan ConnectionTimeout,
    bool DisableVars,
    TransactionConfiguration Transaction,
    ScriptConfiguration[] Scripts,
    NamingConfiguration? Naming,
    JournalConfiguration? JournalTo,
    Dictionary<string,string>? Variables
){
    public static Configuration CreateFromFile(FileInfo configFile)
    {
        using var configReader = (configFile.Exists)?configFile.OpenText():throw new FileNotFoundException($"Could not find the config file at path: {configFile.FullName}");
        var ser = new YamlDotNet.Serialization.Deserializer();
        return ser.Deserialize<Configuration>(configReader);
    }
}

public static class DatabaseProviderExtensions
{
    public static bool TryValidateConnectionString(this DatabaseProvider provider, string connectionString)
    {
        return false;
    }
}

public static class ScriptConfigurationExtensions
{
    public static FileSystemScriptOptions AsFileSystemScriptOptions(this ScriptConfiguration scriptConfiguration)
    {
        return new FileSystemScriptOptions(){
            Encoding=Encoding.GetEncoding(scriptConfiguration.Encoding),
            Filter=scriptConfiguration.Filter,
            IncludeSubDirectories=scriptConfiguration.Recursive,
        }
    }
}
