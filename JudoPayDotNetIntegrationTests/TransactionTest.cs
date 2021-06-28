using System.Linq;
using JudoPayDotNet.Models;
using NUnit.Framework;

namespace JudoPayDotNetIntegrationTests
{
    [TestFixture]
    public class TransactionTest : IntegrationTestsBase
    {
        [Test]
        public void GetTransaction()
        {
            var paymentWithCard = GetCardPaymentModel("432438862");

            var response = JudoPayApiIridium.Payments.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);

            var transaction = JudoPayApiIridium.Transactions.Get(response.Response.ReceiptId).Result;

            Assert.IsNotNull(transaction);
            Assert.IsFalse(transaction.HasError);
            Assert.AreEqual("Success", transaction.Response.Result);
            Assert.AreEqual(response.Response.ReceiptId, transaction.Response.ReceiptId);

            var receipt = transaction.Response as PaymentReceiptModel;
            Assert.IsNotNull(receipt);
            Assert.IsNotNull(receipt.Acquirer);
            Assert.That(receipt.AuthCode, Does.Match("\\d{6}"), $"AuthCode on receipt not in correct format xxxxxx. Was {receipt.AuthCode}");
        }

        [Test]
        // For JR-4723
        public void GetTransactionFromWebPayment()
        {
            // Given a WebPayment session
            var yourConsumerReference = "432438862";
            var webPaymentRequest = new WebPaymentRequestModel
            {
                JudoId = Configuration.Judoid,
                YourConsumerReference = yourConsumerReference,
                Amount = 25
            };

            var webPaymentResult = JudoPayApiIridium.WebPayments.Payments.Create(webPaymentRequest).Result;
            Assert.NotNull(webPaymentResult);
            Assert.IsFalse(webPaymentResult.HasError);

            var webPaymentReference = webPaymentResult.Response.Reference;
            Assert.NotNull(webPaymentReference);

            // And an associated payment (passing webPaymentReference)
            var paymentWithCard = GetCardPaymentModel();
            paymentWithCard.WebPaymentReference = webPaymentReference;

            // For security reasons, fields need to match when the WebPaymentReference is used
            paymentWithCard.YourPaymentReference = webPaymentRequest.YourPaymentReference;
            paymentWithCard.YourConsumerReference = webPaymentRequest.YourConsumerReference;
            paymentWithCard.Amount = webPaymentRequest.Amount;

            var response = JudoPayApiIridium.Payments.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);

            // When receiptId of payment is retrieved
            var transaction = JudoPayApiIridium.Transactions.Get(response.Response.ReceiptId).Result;

            Assert.IsNotNull(transaction);
            Assert.IsFalse(transaction.HasError);
            Assert.AreEqual("Success", transaction.Response.Result);
            Assert.AreEqual(response.Response.ReceiptId, transaction.Response.ReceiptId);

            var receipt = transaction.Response as PaymentReceiptModel;
            Assert.IsNotNull(receipt);

            // Then original WebPaymentReference is shown
            Assert.AreEqual(webPaymentReference, receipt.WebPaymentReference);
            // And AuthCode and Acquirer are also set
            Assert.That(receipt.AuthCode, Does.Match("\\d{6}"), $"AuthCode on receipt not in correct format xxxxxx. Was {receipt.AuthCode}");
            Assert.IsNotNull(receipt.Acquirer);

        }

        [Test]
        public void GetTransactionAfterPreAuthAndTwoCollections()
        {
            var paymentWithCard = GetCardPaymentModel();

            var response = JudoPayApiIridium.PreAuths.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);

            var collection = new CollectionModel()
            {
                Amount = 5,
                ReceiptId = response.Response.ReceiptId
            };
            var collection2 = new CollectionModel()
            {
                Amount = 5,
                ReceiptId = response.Response.ReceiptId
            };
            var collectionResult = JudoPayApiIridium.Collections.Create(collection).Result;
            collectionResult = JudoPayApiIridium.Collections.Create(collection2).Result;

            var transaction = JudoPayApiIridium.Transactions.Get(response.Response.ReceiptId).Result;

            var payment = transaction.Response as PaymentReceiptModel;

            Assert.IsNotNull(transaction);
            Assert.IsFalse(transaction.HasError);
            Assert.AreEqual("Success", transaction.Response.Result);
            Assert.AreEqual(response.Response.ReceiptId, transaction.Response.ReceiptId);
            Assert.AreEqual(10, payment.AmountCollected);
        }


        [Test]
        public void GetAllTransaction()
        {
            var paymentWithCard = GetCardPaymentModel("66666666");

            var response = JudoPayApiIridium.Payments.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
            System.Threading.Thread.Sleep(1000);
            var transaction = JudoPayApiIridium.Transactions.Get().Result;

            Assert.IsNotNull(transaction);
            Assert.IsFalse(transaction.HasError);
            Assert.IsNotEmpty(transaction.Response.Results);
            Assert.IsTrue(transaction.Response.Results.Any(t => t.ReceiptId == response.Response.ReceiptId));
        }

        [Test]
        public void GetPaymentTransactions()
        {
            var paymentWithCard = GetCardPaymentModel("432438862");

            var response = JudoPayApiIridium.Payments.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
            System.Threading.Thread.Sleep(1000);
            var transaction = JudoPayApiIridium.Transactions.Get(TransactionType.PAYMENT).Result;

            Assert.IsNotNull(transaction);
            Assert.IsFalse(transaction.HasError);
            Assert.IsNotEmpty(transaction.Response.Results);
            Assert.AreEqual("Success", transaction.Response.Results.First().Result);
            Assert.IsTrue(transaction.Response.Results.Any(t => t.ReceiptId == response.Response.ReceiptId));
        }
    }
}
