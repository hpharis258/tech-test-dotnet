using ClearBank.DeveloperTest.interfaces;
using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.concrete;

public class ChapsPaymentValidator : IPaymentValidator
{
    public bool CanMakePayment(Account account, MakePaymentRequest request)
    {
        if (account == null)
            return false;

        if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Chaps))
            return false;

        return account.Status == AccountStatus.Live;
    }
}