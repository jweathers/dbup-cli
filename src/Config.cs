using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using DbUp.ScriptProviders;

namespace dbup.dotnet.tool;


internal record class NamingConfiguration(
    bool UseOnlyFileName,
    bool IncludeBaseFolderName,
    string Prefix
);
internal record class JournalConfiguration(
    string Schema,
    string Table
);
internal record class LogConfiguration(
    LogTarget? LogTo,
    bool? LogScriptOutput
);
internal record class Configuration(
    DatabaseProvider Provider,
    TimeSpan ConnectionTimeout,
    TransactionConfiguration Transaction,
    ScriptConfiguration Script,
    NamingConfiguration? Naming,
    JournalConfiguration? JournalTo,
    Dictionary<string, string>? Variables,
    bool DisableVars,
    LogConfiguration? Log
)
{
    internal static Configuration CreateFromFile(FileInfo configFile)
    {
        using var configReader = (configFile.Exists) ? configFile.OpenText() : throw new FileNotFoundException($"Could not find the config file at path: {configFile.FullName}");
        var ser = new YamlDotNet.Serialization.Deserializer();
        return ser.Deserialize<Configuration>(configReader);
    }
}

internal static class DatabaseProviderExtensions
{
    internal static bool TryValidateConnectionString(this DatabaseProvider provider, string connectionString)
    {
        return false;
    }
}


internal static class ConfigurationExtensions
{

    internal static readonly Dictionary<DatabaseProvider,Func<string,UpgradeEngineBuilder>> databaseProviderFactory = new Dictionary<DatabaseProvider, Func<string,UpgradeEngineBuilder>>(){
        {DatabaseProvider.mysql,(cnstr)=>DeployChanges.To.MySqlDatabase(cnstr)},
        {DatabaseProvider.postgresql,(cnstr)=>DeployChanges.To.PostgresqlDatabase(cnstr)},
        {DatabaseProvider.sqlserver,(cnstr)=>DeployChanges.To.SqlDatabase(cnstr)}
    };

    private static UpgradeEngineBuilder CreateBuilderFromProvider(this Configuration configuration, string connectionString)=>databaseProviderFactory[configuration.Provider](connectionString);
    static readonly Dictionary<TransactionConfiguration, Action<UpgradeEngineBuilder>> transactionConfigActions = new Dictionary<TransactionConfiguration, Action<UpgradeEngineBuilder>>{
        {TransactionConfiguration.None,e=>e.WithoutTransaction()},
        {TransactionConfiguration.PerScript,e=>e.WithTransactionPerScript()},
        {TransactionConfiguration.Single,e=>e.WithTransaction()}
    };
    internal static Configuration ConfigureTransactions(this Configuration configuration, UpgradeEngineBuilder upgradeEngineBuilder)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(upgradeEngineBuilder);
        transactionConfigActions[configuration.Transaction](upgradeEngineBuilder);
        return configuration;
    }

    internal static Configuration ConfigureScripts(this Configuration configuration, UpgradeEngineBuilder upgradeEngineBuilder)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(upgradeEngineBuilder);
        upgradeEngineBuilder.WithScripts(configuration.Script.AsFileSystemScriptProvider());
        return configuration;
    }
    internal static Configuration ConfigureVariables(this Configuration configuration, UpgradeEngineBuilder upgradeEngineBuilder)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(upgradeEngineBuilder);
        if ((configuration.Variables?.Count ?? 0) > 0)
        {
            upgradeEngineBuilder.WithVariables(configuration.Variables);
        }
        if (configuration.DisableVars)
        {
            upgradeEngineBuilder.WithVariablesDisabled();
        }
        else
        {
            upgradeEngineBuilder.WithVariablesEnabled();
        }
        return configuration;
    }


    static readonly Dictionary<LogTarget,Action<UpgradeEngineBuilder>> logConfigurationActions = new Dictionary<LogTarget, Action<UpgradeEngineBuilder>>(){
        {LogTarget.Autodetect,b=>b.LogToAutodetectedLog()},
        {LogTarget.Console,b=>b.LogToConsole()},
        {LogTarget.Nowhere,b=>b.LogToNowhere()},
        {LogTarget.Trace,b=>b.LogToTrace()}
    };
    internal static Configuration ConfigureLogging(this Configuration configuration, UpgradeEngineBuilder upgradeEngineBuilder)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(upgradeEngineBuilder);
        if(configuration.Log?.LogScriptOutput ?? false)
        {
            upgradeEngineBuilder.LogScriptOutput();
        }
        if(configuration.Log?.LogTo is not null)
        {
            logConfigurationActions[configuration.Log.LogTo.Value](upgradeEngineBuilder);
        }
        return configuration;
    }
    
    internal static UpgradeEngineBuilder CreateBuilder(this Configuration configuration, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);
        var builder = configuration.CreateBuilderFromProvider(connectionString);
        configuration.ConfigureVariables(builder).ConfigureLogging(builder).ConfigureTransactions(builder).ConfigureScripts(builder);
        return builder;
    }
}

public enum LogTarget
{
    Autodetect,
    Nowhere,
    Console,
    Trace
}