using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.interfaces;

namespace ClearBank.DeveloperTest.concrete;

public class DataStoreFactory : IAccountDataStoreFactory
{
    private readonly IConfig _configuration;
    public DataStoreFactory(IConfig configurationService)
    {
        _configuration = configurationService;
    }
    
    public IBankAccountDataStore CreateDataStore()
    {
        var dataStoreType = _configuration.GetDataStoreType();
            
        return dataStoreType == "Backup" 
            ? new BackupAccountDataStore() 
            : new AccountDataStore();
    }
}