﻿using System;
using JudoPayDotNet.Models;
using NUnit.Framework;

namespace JudoPayDotNetIntegrationTests
{
    [TestFixture]
    public class SaveCardTest : IntegrationTestsBase
    {
        [Test]
        public void SaveCard()
        {
            var registerCardModel = GetCardPaymentModel("432438862");

            var response = JudoPayApi.SaveCards.Create(registerCardModel).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
        }

        [Test]
        [Ignore("This test requires a change to the aPI to make CV2 optional")]
        public void SaveCardWithNoCv2()
        {
            var registerCardModel = GetCardPaymentModel("432438862", "4976000000003436", null);

            var response = JudoPayApi.SaveCards.Create(registerCardModel).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
        }

        [Test]
        public void SaveCardAndATokenPayment()
        {
            var consumerReference = Guid.NewGuid().ToString();

            var registerCard = GetCardPaymentModel(consumerReference);

            var response = JudoPayApi.SaveCards.Create(registerCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentReceiptModel;

            Assert.IsNotNull(receipt);

            Assert.AreEqual("Success", receipt.Result);

            // Fetch the card token
            var cardToken = receipt.CardDetails.CardToken;

            var paymentWithToken = GetTokenPaymentModel(cardToken, consumerReference, 26);

            response = JudoPayApi.Payments.Create(paymentWithToken).Result;

            paymentWithToken = GetTokenPaymentModel(cardToken, consumerReference, 27);

            response = JudoPayApi.Payments.Create(paymentWithToken).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);
        }

        [Test]
        public void AFailedSaveCard()
        {
            var registerCard = GetCardPaymentModel("432438862", "4221690000004963", "125");

            var response = JudoPayApi.SaveCards.Create(registerCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Declined", response.Response.Result);
        }
    }
}
