using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.interfaces;

public interface IBankAccountDataStore
{
    Account GetAccount(string accountNumber);
    void UpdateAccount(Account account);
}