using System.Configuration;
using ClearBank.DeveloperTest.interfaces;

namespace ClearBank.DeveloperTest.concrete;

public class ConfigurationService : IConfig
{
    public string GetDataStoreType()
    {
       return ConfigurationManager.AppSettings["DataStoreType"] ?? "";
    }
}