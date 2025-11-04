using System;
using ClearBank.DeveloperTest.concrete;
using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.interfaces;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using Xunit;
using Moq;

namespace ClearBank.DeveloperTest.Tests;

public class PaymentService_tests
{
    /// <summary>
    // Throughout my unit tests I am using the AAA, Arrange, Act, Assert.
    /// </summary>
    private readonly Mock<IAccountDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<IPaymentValidatorFactory> _mockValidatorFactory;
    private readonly Mock<IBankAccountDataStore> _mockDataStore;
    private readonly Mock<IPaymentValidator> _mockValidator;
    private readonly PaymentService _paymentService;

    public PaymentService_tests()
    {
        _mockDataStoreFactory = new Mock<IAccountDataStoreFactory>();
        _mockValidatorFactory = new Mock<IPaymentValidatorFactory>();
        _mockDataStore = new Mock<IBankAccountDataStore>();
        _mockValidator = new Mock<IPaymentValidator>();

        // Setup default behavior
        _mockDataStoreFactory.Setup(x => x.CreateDataStore()).Returns(_mockDataStore.Object);
        _mockValidatorFactory.Setup(x => x.GetValidator(It.IsAny<PaymentScheme>())).Returns(_mockValidator.Object);

        _paymentService = new PaymentService(_mockDataStoreFactory.Object, _mockValidatorFactory.Object);
    }

    [Fact]
    public void PaymentServiceMakePaymentShouldThrowNullExceptionWhenPassedInNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _paymentService.MakePayment(null));
    }

    [Fact]
    public void MakePayment_WithEmptyAccountNumber_ReturnsFalse()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "",
            Amount = 100,
            PaymentScheme = PaymentScheme.Bacs
        };

        var account = new Account();
        _mockDataStore.Setup(x => x.GetAccount("")).Returns(account);
        _mockValidator.Setup(x => x.CanMakePayment(account, request)).Returns(false);

        // Act
        var result = _paymentService.MakePayment(request);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void MakePayment_WithNullAccountNumber_ReturnsFalse()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = null,
            Amount = 100,
            PaymentScheme = PaymentScheme.Bacs
        };

        var account = new Account();
        _mockDataStore.Setup(x => x.GetAccount(null)).Returns(account);
        _mockValidator.Setup(x => x.CanMakePayment(account, request)).Returns(false);

        // Act
        var result = _paymentService.MakePayment(request);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void MakePayment_WithZeroAmount_ReturnsFalse()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "123456789",
            Amount = 0,
            PaymentScheme = PaymentScheme.Bacs
        };

        var account = new Account { Balance = 1000 };
        _mockDataStore.Setup(x => x.GetAccount("123456789")).Returns(account);
        _mockValidator.Setup(x => x.CanMakePayment(account, request)).Returns(false);

        // Act
        var result = _paymentService.MakePayment(request);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void MakePayment_WithNegativeAmount_ReturnsFalse()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "123456789",
            Amount = -100,
            PaymentScheme = PaymentScheme.Bacs
        };

        var account = new Account { Balance = 1000 };
        _mockDataStore.Setup(x => x.GetAccount("123456789")).Returns(account);
        _mockValidator.Setup(x => x.CanMakePayment(account, request)).Returns(false);

        // Act
        var result = _paymentService.MakePayment(request);

        // Assert
        Assert.False(result.Success);
    }

    [Theory]
    [InlineData(PaymentScheme.Bacs, AllowedPaymentSchemes.Bacs, AccountStatus.Live, 1000, 500, true)]
    [InlineData(PaymentScheme.Bacs, AllowedPaymentSchemes.FasterPayments, AccountStatus.Live, 1000, 500, false)]
    [InlineData(PaymentScheme.Bacs, AllowedPaymentSchemes.Bacs, AccountStatus.Disabled, 1000, 500, true)] // Bacs doesn't check status
    [InlineData(PaymentScheme.FasterPayments, AllowedPaymentSchemes.FasterPayments, AccountStatus.Live, 1000, 500, true)]
    [InlineData(PaymentScheme.FasterPayments, AllowedPaymentSchemes.Bacs, AccountStatus.Live, 1000, 500, false)]
    [InlineData(PaymentScheme.FasterPayments, AllowedPaymentSchemes.FasterPayments, AccountStatus.Live, 500, 1000, false)]
    [InlineData(PaymentScheme.Chaps, AllowedPaymentSchemes.Chaps, AccountStatus.Live, 1000, 500, true)]
    [InlineData(PaymentScheme.Chaps, AllowedPaymentSchemes.FasterPayments, AccountStatus.Live, 1000, 500, false)]
    [InlineData(PaymentScheme.Chaps, AllowedPaymentSchemes.Chaps, AccountStatus.Disabled, 1000, 500, false)]
    public void MakePayment_WithVariousScenarios_ReturnsExpectedResult(
        PaymentScheme paymentScheme,
        AllowedPaymentSchemes allowedSchemes,
        AccountStatus accountStatus,
        decimal accountBalance,
        decimal paymentAmount,
        bool expectedSuccess)
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "123456789",
            Amount = paymentAmount,
            PaymentScheme = paymentScheme
        };

        var account = new Account
        {
            AccountNumber = "123456789",
            Balance = accountBalance,
            Status = accountStatus,
            AllowedPaymentSchemes = allowedSchemes
        };

        _mockDataStore.Setup(x => x.GetAccount("123456789")).Returns(account);
        _mockValidator.Setup(x => x.CanMakePayment(account, request)).Returns(expectedSuccess);

        // Act
        var result = _paymentService.MakePayment(request);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);

        if (expectedSuccess)
        {
            _mockDataStore.Verify(x => x.UpdateAccount(account), Times.Once);
            Assert.Equal(accountBalance - paymentAmount, account.Balance);
        }
        else
        {
            _mockDataStore.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Never);
        }
    }

    [Fact]
    public void MakePayment_ValidPayment_UpdatesAccountBalance()
    {
        // Arrange
        var initialBalance = 1000m;
        var paymentAmount = 100m;
        var account = new Account
        {
            AccountNumber = "123456789",
            Balance = initialBalance,
            Status = AccountStatus.Live,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs
        };

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "123456789",
            Amount = paymentAmount,
            PaymentScheme = PaymentScheme.Bacs
        };

        _mockDataStore.Setup(x => x.GetAccount("123456789")).Returns(account);
        _mockValidator.Setup(x => x.CanMakePayment(account, request)).Returns(true);

        // Act
        var result = _paymentService.MakePayment(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialBalance - paymentAmount, account.Balance);
        _mockDataStore.Verify(x => x.UpdateAccount(account), Times.Once);
    }

    [Fact]
    public void MakePayment_CallsCorrectValidator()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "123456789",
            Amount = 100,
            PaymentScheme = PaymentScheme.Chaps
        };

        var account = new Account();
        _mockDataStore.Setup(x => x.GetAccount("123456789")).Returns(account);

        // Act
        _paymentService.MakePayment(request);

        // Assert
        _mockValidatorFactory.Verify(x => x.GetValidator(PaymentScheme.Chaps), Times.Once);
        _mockValidator.Verify(x => x.CanMakePayment(account, request), Times.Once);
    }

    [Fact]
    public void MakePayment_CallsDataStoreFactory()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "123456789",
            Amount = 100,
            PaymentScheme = PaymentScheme.Bacs
        };

        var account = new Account();
        _mockDataStore.Setup(x => x.GetAccount("123456789")).Returns(account);

        // Act
        _paymentService.MakePayment(request);

        // Assert
        _mockDataStoreFactory.Verify(x => x.CreateDataStore(), Times.Once);
        _mockDataStore.Verify(x => x.GetAccount("123456789"), Times.Once);
    }

    [Fact]
    public void MakePayment_WhenValidationFails_DoesNotUpdateAccount()
    {
        // Arrange
        var account = new Account
        {
            AccountNumber = "123456789",
            Balance = 1000,
            Status = AccountStatus.Disabled,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Chaps
        };

        var request = new MakePaymentRequest
        {
            DebtorAccountNumber = "123456789",
            Amount = 100,
            PaymentScheme = PaymentScheme.Chaps
        };

        _mockDataStore.Setup(x => x.GetAccount("123456789")).Returns(account);
        _mockValidator.Setup(x => x.CanMakePayment(account, request)).Returns(false);

        // Act
        var result = _paymentService.MakePayment(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(1000, account.Balance); // Balance should remain unchanged
        _mockDataStore.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Never);
    }
}

// Test individual validators
public class BacsPaymentValidatorTests
{
    private readonly BacsPaymentValidator _validator;

    public BacsPaymentValidatorTests()
    {
        _validator = new BacsPaymentValidator();
    }

    [Fact]
    public void CanMakePayment_WithNullAccount_ReturnsFalse()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            Amount = 100,
            PaymentScheme = PaymentScheme.Bacs
        };

        // Act
        var result = _validator.CanMakePayment(null, request);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(AllowedPaymentSchemes.Bacs, true)]
    [InlineData(AllowedPaymentSchemes.FasterPayments, false)]
    [InlineData(AllowedPaymentSchemes.Chaps, false)]
    [InlineData(AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps, true)]
    public void CanMakePayment_ChecksAllowedPaymentSchemes(AllowedPaymentSchemes allowedSchemes, bool expected)
    {
        // Arrange
        var account = new Account
        {
            AllowedPaymentSchemes = allowedSchemes,
            Status = AccountStatus.Live,
            Balance = 1000
        };
        var request = new MakePaymentRequest
        {
            Amount = 100,
            PaymentScheme = PaymentScheme.Bacs
        };

        // Act
        var result = _validator.CanMakePayment(account, request);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AccountStatus.Live)]
    [InlineData(AccountStatus.Disabled)]
    [InlineData(AccountStatus.InboundPaymentsOnly)]
    public void CanMakePayment_DoesNotCheckAccountStatus(AccountStatus status)
    {
        // Arrange
        var account = new Account
        {
            AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs,
            Status = status,
            Balance = 1000
        };
        var request = new MakePaymentRequest
        {
            Amount = 100,
            PaymentScheme = PaymentScheme.Bacs
        };

        // Act
        var result = _validator.CanMakePayment(account, request);

        // Assert - Bacs should work regardless of account status
        Assert.True(result);
    }
}

public class FasterPaymentsValidatorTests
{
    private readonly FasterPaymentsValidator _validator;

    public FasterPaymentsValidatorTests()
    {
        _validator = new FasterPaymentsValidator();
    }

    [Fact]
    public void CanMakePayment_WithNullAccount_ReturnsFalse()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            Amount = 100,
            PaymentScheme = PaymentScheme.FasterPayments
        };

        // Act
        var result = _validator.CanMakePayment(null, request);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(AllowedPaymentSchemes.FasterPayments, 1000, 500, true)]
    [InlineData(AllowedPaymentSchemes.FasterPayments, 500, 500, true)]
    [InlineData(AllowedPaymentSchemes.FasterPayments, 499, 500, false)]
    [InlineData(AllowedPaymentSchemes.Bacs, 1000, 500, false)]
    public void CanMakePayment_ChecksSchemeAndBalance(AllowedPaymentSchemes allowedSchemes, decimal balance, decimal amount, bool expected)
    {
        // Arrange
        var account = new Account
        {
            Balance = balance,
            AllowedPaymentSchemes = allowedSchemes,
            Status = AccountStatus.Live
        };
        var request = new MakePaymentRequest { Amount = amount };

        // Act
        var result = _validator.CanMakePayment(account, request);

        // Assert
        Assert.Equal(expected, result);
    }
}

public class ChapsPaymentValidatorTests
{
    private readonly ChapsPaymentValidator _validator;

    public ChapsPaymentValidatorTests()
    {
        _validator = new ChapsPaymentValidator();
    }

    [Fact]
    public void CanMakePayment_WithNullAccount_ReturnsFalse()
    {
        // Arrange
        var request = new MakePaymentRequest
        {
            Amount = 100,
            PaymentScheme = PaymentScheme.Chaps
        };

        // Act
        var result = _validator.CanMakePayment(null, request);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(AllowedPaymentSchemes.Chaps, AccountStatus.Live, true)]
    [InlineData(AllowedPaymentSchemes.Chaps, AccountStatus.Disabled, false)]
    [InlineData(AllowedPaymentSchemes.Chaps, AccountStatus.InboundPaymentsOnly, false)]
    [InlineData(AllowedPaymentSchemes.Bacs, AccountStatus.Live, false)]
    public void CanMakePayment_ChecksSchemeAndAccountStatus(AllowedPaymentSchemes allowedSchemes, AccountStatus status, bool expected)
    {
        // Arrange
        var account = new Account
        {
            Status = status,
            AllowedPaymentSchemes = allowedSchemes,
            Balance = 1000
        };
        var request = new MakePaymentRequest { Amount = 100 };

        // Act
        var result = _validator.CanMakePayment(account, request);

        // Assert
        Assert.Equal(expected, result);
    }
}

// Test factories
public class PaymentValidatorFactoryTests
{
    private readonly PaymentValidatorFactory _factory;

    public PaymentValidatorFactoryTests()
    {
        _factory = new PaymentValidatorFactory();
    }

    [Theory]
    [InlineData(PaymentScheme.Bacs, typeof(BacsPaymentValidator))]
    [InlineData(PaymentScheme.FasterPayments, typeof(FasterPaymentsValidator))]
    [InlineData(PaymentScheme.Chaps, typeof(ChapsPaymentValidator))]
    public void GetValidator_ReturnsCorrectValidatorType(PaymentScheme scheme, Type expectedType)
    {
        // Act
        var validator = _factory.GetValidator(scheme);

        // Assert
        Assert.IsType(expectedType, validator);
    }

    [Fact]
    public void GetValidator_WithInvalidScheme_ThrowsNotSupportedException()
    {
        // Arrange
        var invalidScheme = (PaymentScheme)999;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _factory.GetValidator(invalidScheme));
    }
}

public class DataStoreFactoryTests
{
    [Fact]
    public void CreateDataStore_WithBackupConfig_ReturnsBackupDataStore()
    {
        // Arrange
        var mockConfig = new Mock<IConfig>();
        mockConfig.Setup(x => x.GetDataStoreType()).Returns("Backup");
        var factory = new DataStoreFactory(mockConfig.Object);

        // Act
        var dataStore = factory.CreateDataStore();

        // Assert
        Assert.IsType<BackupAccountDataStore>(dataStore);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Primary")]
    [InlineData("SomethingElse")]
    [InlineData(null)]
    public void CreateDataStore_WithNonBackupConfig_ReturnsAccountDataStore(string configValue)
    {
        // Arrange
        var mockConfig = new Mock<IConfig>();
        mockConfig.Setup(x => x.GetDataStoreType()).Returns(configValue);
        var factory = new DataStoreFactory(mockConfig.Object);

        // Act
        var dataStore = factory.CreateDataStore();

        // Assert
        Assert.IsType<AccountDataStore>(dataStore);
    }
}