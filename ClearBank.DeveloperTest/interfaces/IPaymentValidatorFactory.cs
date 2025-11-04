using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.interfaces; 

public interface IPaymentValidatorFactory
{
    IPaymentValidator GetValidator(PaymentScheme paymentScheme);
}
