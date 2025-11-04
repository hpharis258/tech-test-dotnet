using System;
using ClearBank.DeveloperTest.interfaces;
using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.concrete;

public class PaymentValidatorFactory :IPaymentValidatorFactory
{
    public IPaymentValidator GetValidator(PaymentScheme paymentScheme)
    {
        return paymentScheme switch
        {
            PaymentScheme.Bacs => new BacsPaymentValidator(),
            PaymentScheme.FasterPayments => new FasterPaymentsValidator(),
            PaymentScheme.Chaps => new ChapsPaymentValidator(),
            _ => throw new NotSupportedException($"Payment scheme {paymentScheme} is not supported")
        };
    }
}