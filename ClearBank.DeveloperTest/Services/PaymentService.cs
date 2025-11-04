using System;
using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Types;
using System.Configuration;
using ClearBank.DeveloperTest.concrete;
using ClearBank.DeveloperTest.interfaces;

namespace ClearBank.DeveloperTest.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentValidatorFactory _paymentValidatorFactory;
        private readonly IAccountDataStoreFactory _dataStoreFactory;
        public PaymentService(IAccountDataStoreFactory dataStoreFactory, IPaymentValidatorFactory paymentValidatorFactory)
        {
            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
            _paymentValidatorFactory = paymentValidatorFactory ?? throw new ArgumentNullException(nameof(paymentValidatorFactory));
        }

        public MakePaymentResult MakePayment(MakePaymentRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException();
            }
            
            var dataStore = _dataStoreFactory.CreateDataStore();
            var account = dataStore.GetAccount(request.DebtorAccountNumber);
            
            var validator = _paymentValidatorFactory.GetValidator(request.PaymentScheme);
            var canMakePayment = validator.CanMakePayment(account, request);

            var result = new MakePaymentResult { Success = canMakePayment };

            if (result.Success && account != null)
            {
                account.Balance -= request.Amount;
                dataStore.UpdateAccount(account);
            }

            return result;
        }
    }
}
