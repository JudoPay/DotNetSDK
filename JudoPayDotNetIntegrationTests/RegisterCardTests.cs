﻿using System;
using System.Linq;
using System.Threading.Tasks;
using JudoPayDotNet.Models;
using JudoPayDotNet.Models.Validations;
using NUnit.Framework;

namespace JudoPayDotNetIntegrationTests
{
    [TestFixture]
    public class RegisterCardTest : IntegrationTestsBase
    {

        [Test]
        public async Task RegisterCard()
        {

            var registerCardModel = GetRegisterCardModel("432438862");

            var response = await JudoPayApiIridium.RegisterCards.Create(registerCardModel);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
        }

        [Test]
        public void RegisterEncryptedCard()
        {
            var registerEncryptedCardModel = GetRegisterEncryptedCardModel().Result;

            var response = JudoPayApiIridium.RegisterCards.Create(registerEncryptedCardModel).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
        }

        [Test]
        public async Task RegisterCardAndATokenPayment()
        {
            var consumerReference = Guid.NewGuid().ToString();

            var registerCard = GetRegisterCardModel(consumerReference);

            var response = await JudoPayApiIridium.RegisterCards.Create(registerCard);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;

            Assert.IsNotNull(receipt);

            Assert.AreEqual("Success", receipt.Result);

            // Fetch the card token
            var cardToken = receipt.CardDetails.CardToken;

            var paymentWithToken = GetTokenPaymentModel(cardToken, consumerReference, 27);

            response = await JudoPayApiIridium.Payments.Create(paymentWithToken);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
        }

        [Test]
        public void ADeclinedCardPayment()
        {
            var registerCard = GetRegisterCardModel("432438862", "4221690000004963", "125");

            var response = JudoPayApiIridium.RegisterCards.Create(registerCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Declined", response.Response.Result);
        }

        [Test, TestCaseSource(typeof(CheckCardTests.RegisterCheckCardTestSource), nameof(CheckCardTests.RegisterCheckCardTestSource.ValidateFailureTestCases))]
        public void ValidateWithoutSuccess(RegisterCardModel registerCardModel, JudoModelErrorCode expectedModelErrorCode)
        {
            var registerCardReceiptResult = JudoPayApiIridium.RegisterCards.Create(registerCardModel).Result;
            Assert.NotNull(registerCardReceiptResult);
            Assert.IsTrue(registerCardReceiptResult.HasError);
            Assert.IsNull(registerCardReceiptResult.Response);
            Assert.IsNotNull(registerCardReceiptResult.Error);
            Assert.AreEqual((int)JudoApiError.General_Model_Error, registerCardReceiptResult.Error.Code);

            var fieldErrors = registerCardReceiptResult.Error.ModelErrors;
            Assert.IsNotNull(fieldErrors);
            Assert.IsTrue(fieldErrors.Count >= 1);
            Assert.IsTrue(fieldErrors.Any(x => x.Code == (int)expectedModelErrorCode));
        }
    }
}
