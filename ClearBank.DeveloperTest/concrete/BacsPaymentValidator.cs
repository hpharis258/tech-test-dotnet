using ClearBank.DeveloperTest.interfaces;
using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.concrete;

public class BacsPaymentValidator : IPaymentValidator
{
    public bool CanMakePayment(Account account, MakePaymentRequest request)
    {
        if (account == null)
            return false;

        return account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Bacs);
    }
}