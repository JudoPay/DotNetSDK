﻿using System.Linq;
using JudoPayDotNet.Models;
using JudoPayDotNet.Models.Validations;
using NUnit.Framework;

namespace JudoPayDotNetIntegrationTests
{
    [TestFixture]
    public class PreAuthAndCollectionTests : IntegrationTestsBase
    {
        [Test]
        public void ASimplePreAuth()
        {
            var paymentWithCard = GetCardPaymentModel("432438862");

            MakeAPreAuthWithCard(paymentWithCard);
        }

        [Test]
        public void AOneTimePreAuth()
        {
            var oneTimePaymentModel = GetOneTimePaymentModel().Result;

            var response = JudoPayApiIridium.PreAuths.Create(oneTimePaymentModel).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;

            Assert.IsNotNull(receipt);
            Assert.AreEqual("Success", receipt.Result);
        }

        [Test]
        public void ADeclinedCardPreAuth()
        {
            var paymentWithCard = GetCardPaymentModel("432438862", "4221690000004963", "125");

            var response = JudoPayApiIridium.PreAuths.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Declined", response.Response.Result);
        }

        [Test]
        public void ASimplePreAuthAndCollection()
        {
            var paymentWithCard = GetCardPaymentModel();
            var preAuthReceiptId = MakeAPreAuthWithCard(paymentWithCard);

            var collection = new CollectionModel
            {
                ReceiptId = preAuthReceiptId,
                Amount = paymentWithCard.Amount
            };

            var response = JudoPayApiIridium.Collections.Create(collection).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;

            Assert.IsNotNull(receipt);

            Assert.AreEqual("Success", receipt.Result);
            Assert.AreEqual("Collection", receipt.Type);
            Assert.AreEqual(paymentWithCard.Amount, receipt.Amount);
        }

        [Test]
        public void PreAuthAndCollectionWithoutAmount()
        {
            var paymentWithCard = GetCardPaymentModel();
            var preAuthReceiptId = MakeAPreAuthWithCard(paymentWithCard);

            var collection = new CollectionModel
            {
                ReceiptId = preAuthReceiptId
            };

            var response = JudoPayApiIridium.Collections.Create(collection).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;

            Assert.IsNotNull(receipt);

            Assert.AreEqual("Success", receipt.Result);
            Assert.AreEqual("Collection", receipt.Type);
            Assert.AreEqual(paymentWithCard.Amount, receipt.Amount);
        }

        [Test]
        public void AFailedSimplePreAuthAndValidateCollection()
        {
            var paymentWithCard = GetCardPaymentModel("432438862");
            var preAuthReceiptId = MakeAPreAuthWithCard(paymentWithCard);

            var collection = new CollectionModel
            {
                Amount = 20,
                ReceiptId = preAuthReceiptId
            };
            var collection2 = new CollectionModel
            {
                Amount = 20,
                ReceiptId = preAuthReceiptId
            };
            var collectionResponse = JudoPayApiIridium.Collections.Create(collection).Result;

            // The collection will go through since it is less than the preauth amount
            Assert.That(collectionResponse.HasError, Is.False);
            Assert.That(collectionResponse.Response.ReceiptId, Is.GreaterThan(0));

            var validateResponse = JudoPayApiIridium.Collections.Create(collection2).Result;

            Assert.That(validateResponse, Is.Not.Null);
            Assert.That(validateResponse.HasError, Is.True);
            Assert.That(validateResponse.Error.Message, Is.EqualTo("Sorry, but the amount you're trying to collect is greater than the pre-auth"));
            Assert.That(validateResponse.Error.Code, Is.EqualTo(46));
        }

        [Test]
        public void ASimplePreAuthAndVoid()
        {
            var paymentWithCard = GetCardPaymentModel();
            var preAuthReceiptId = MakeAPreAuthWithCard(paymentWithCard);

            var voidPreAuth = new VoidModel
            {
                Amount = paymentWithCard.Amount,
                ReceiptId = preAuthReceiptId
            };

            var response = JudoPayApiIridium.Voids.Create(voidPreAuth).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;

            Assert.IsNotNull(receipt);

            Assert.AreEqual("Success", receipt.Result);
            Assert.AreEqual("VOID", receipt.Type);
            Assert.AreEqual(paymentWithCard.Amount, receipt.Amount);
        }


        [Test]
        public void PreAuthAndVoidWithoutAmount()
        {
            var paymentWithCard = GetCardPaymentModel();
            var preAuthReceiptId = MakeAPreAuthWithCard(paymentWithCard);

            var voidPreAuth = new VoidModel
            {
                ReceiptId = preAuthReceiptId
            };

            var response = JudoPayApiIridium.Voids.Create(voidPreAuth).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;

            Assert.IsNotNull(receipt);

            Assert.AreEqual("Success", receipt.Result);
            Assert.AreEqual("VOID", receipt.Type);
            Assert.AreEqual(paymentWithCard.Amount, receipt.Amount);
        }

        // Reuse Test source from PaymentTest as some model is used for payments/preauths
        [Test, TestCaseSource(typeof(PaymentTest.PaymentsTestSource), nameof(PaymentTest.PaymentsTestSource.ValidateFailureTestCases))]
        public void ValidateWithoutSuccess(PaymentModel preauth, JudoModelErrorCode expectedModelErrorCode)
        {
            IResult<ITransactionResult> preAuthReceiptResult = null;

            switch (preauth)
            {
                // ReSharper disable CanBeReplacedWithTryCastAndCheckForNull
                case CardPaymentModel model:
                    preAuthReceiptResult = JudoPayApiIridium.PreAuths.Create(model).Result;
                    break;
                case TokenPaymentModel model:
                    preAuthReceiptResult = JudoPayApiIridium.PreAuths.Create(model).Result;
                    break;
                case OneTimePaymentModel model:
                    preAuthReceiptResult = JudoPayApiIridium.PreAuths.Create(model).Result;
                    break;
                case PKPaymentModel model:
                    preAuthReceiptResult = JudoPayApiIridium.PreAuths.Create(model).Result;
                    break;
            }
            // ReSharper restore CanBeReplacedWithTryCastAndCheckForNull

            Assert.NotNull(preAuthReceiptResult);
            Assert.IsTrue(preAuthReceiptResult.HasError);
            Assert.IsNull(preAuthReceiptResult.Response);
            Assert.IsNotNull(preAuthReceiptResult.Error);
            Assert.AreEqual((int)JudoApiError.General_Model_Error, preAuthReceiptResult.Error.Code);

            var fieldErrors = preAuthReceiptResult.Error.ModelErrors;
            Assert.IsNotNull(fieldErrors);
            Assert.IsTrue(fieldErrors.Count >= 1);
            Assert.IsTrue(fieldErrors.Any(x => x.Code == (int)expectedModelErrorCode));
        }

        private long MakeAPreAuthWithCard(CardPaymentModel paymentWithCard)
        {
            var response = JudoPayApiIridium.PreAuths.Create(paymentWithCard).Result;
            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;
            Assert.IsNotNull(receipt);
            Assert.AreEqual("Success", receipt.Result);
            Assert.AreEqual("PreAuth", receipt.Type);

            return receipt.ReceiptId;
        }
    }
}