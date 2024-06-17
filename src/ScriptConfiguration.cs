using System.Text;
using System.Text.RegularExpressions;
using DbUp.Engine;
using DbUp.ScriptProviders;

namespace dbup.dotnet.tool;

internal record class ScriptConfiguration
(
    string Path,
    string[]? Extensions,
    bool? IncludeSubDirectories,
    string? Encoding,
    string? Filter,
    bool? UseOnlyFilenameForScriptName,
    int? RunGroupOrder,
    DbUp.Support.ScriptType? ScriptType
)
{
    public static implicit operator FileSystemScriptProvider(ScriptConfiguration scriptConfiguration)=>scriptConfiguration?.AsFileSystemScriptProvider();
}


internal static class ScriptConfigurationExtensions
{
    internal static FileSystemScriptProvider AsFileSystemScriptProvider(this ScriptConfiguration scriptConfiguration)
    {
        ArgumentNullException.ThrowIfNull(scriptConfiguration);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(scriptConfiguration.Path);
        if(!Path.Exists(scriptConfiguration.Path)) throw new ArgumentOutOfRangeException(nameof(scriptConfiguration.Path), $"Path not found. {scriptConfiguration.Path}");
        
        var options = new FileSystemScriptOptions();

        if ((scriptConfiguration.Extensions?.Length ?? 0) > 0)
        {
            options.Extensions=new string[scriptConfiguration.Extensions.Length];
            Array.Copy(scriptConfiguration.Extensions, options.Extensions, options.Extensions.Length);
        }

        if(!string.IsNullOrWhiteSpace(scriptConfiguration.Encoding))
        {
            options.Encoding= Encoding.GetEncoding(scriptConfiguration.Encoding);
        }

        if(!string.IsNullOrWhiteSpace(scriptConfiguration.Filter))
        {
            options.Filter=(v1)=>Regex.IsMatch(v1,scriptConfiguration.Filter);
        }

        if(scriptConfiguration.IncludeSubDirectories.HasValue)
        {
            options.IncludeSubDirectories=scriptConfiguration.IncludeSubDirectories.Value;
        }

        if(scriptConfiguration.UseOnlyFilenameForScriptName.HasValue)
        {
            options.UseOnlyFilenameForScriptName=scriptConfiguration.UseOnlyFilenameForScriptName.Value;
        }

        SqlScriptOptions? sqlScriptOptions=new SqlScriptOptions();
        if(scriptConfiguration.RunGroupOrder.HasValue)
        {
            sqlScriptOptions.RunGroupOrder=scriptConfiguration.RunGroupOrder.Value;
        } 
        if(scriptConfiguration.ScriptType.HasValue)
        {
            sqlScriptOptions.ScriptType=scriptConfiguration.ScriptType.Value;
            
        }
        
        return new FileSystemScriptProvider(scriptConfiguration.Path,options,sqlScriptOptions);
    }
}