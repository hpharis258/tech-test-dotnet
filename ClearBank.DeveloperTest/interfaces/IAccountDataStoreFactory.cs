namespace ClearBank.DeveloperTest.interfaces;

public interface IAccountDataStoreFactory
{
    IBankAccountDataStore CreateDataStore();
}