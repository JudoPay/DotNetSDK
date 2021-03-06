﻿using System;
using System.Threading;
using JudoPayDotNet;
using JudoPayDotNet.Models;

namespace JudoPayDotNetFrameworkSampleApp
{
    class JudoPayDotNetFrameworkSampleApp
    {
        private static string ApiToken = "{BASE_TOKEN}";
        private static readonly string ApiSecret = "{SECRET}";

        static void Main(string[] args)
        {
            var client = JudoPaymentsFactory.Create(JudoPayDotNet.Enums.JudoEnvironment.Sandbox, ApiToken, ApiSecret);
            var cardPaymentModel = new CardPaymentModel
            {
                JudoId = "100915867",

                // value of the payment
                Amount = 1.01m,
                Currency = "GBP",

                // card details
                CardNumber = "4976000000003436",
                ExpiryDate = "1225",
                CV2 = "452",

                // an identifier for your customer
                YourConsumerReference = "MyCustomer004",
            };

            client.Payments.Create(cardPaymentModel).ContinueWith(result =>
            {
                if (result.IsFaulted)
                {
                    Console.WriteLine($"Payment request failed, exception={result.Exception.InnerExceptions[0].Message}");
                }
                else
                {
                    var paymentResult = result.Result;

                    if (!paymentResult.HasError && paymentResult.Response.Result == "Success")
                    {
                        Console.WriteLine($"Payment successful. Transaction Reference {paymentResult.Response.ReceiptId}");
                    }
                    else
                    {
                        Console.WriteLine("Payment failed");
                    }
                }
            });
            Console.WriteLine("Payment request sent");
            Thread.Sleep(2000);

            // Wait for the user to respond before closing.
            Console.WriteLine("Press any key to close the app...");
            Console.ReadKey();
        }
    }
}
