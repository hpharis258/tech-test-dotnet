using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.interfaces;

public interface IPaymentValidator
{
    bool CanMakePayment(Account account, MakePaymentRequest request);
}