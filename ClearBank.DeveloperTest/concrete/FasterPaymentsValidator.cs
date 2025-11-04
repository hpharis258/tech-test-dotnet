using ClearBank.DeveloperTest.interfaces;
using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.concrete;

public class FasterPaymentsValidator : IPaymentValidator
{
    public bool CanMakePayment(Account account, MakePaymentRequest request)
    {
        if (account == null)
            return false;

        if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.FasterPayments))
            return false;

        return account.Balance >= request.Amount;
    }
}