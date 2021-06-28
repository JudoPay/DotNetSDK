using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using JudoPayDotNet;
using JudoPayDotNet.Enums;
using JudoPayDotNet.Models;
using JudoPayDotNet.Models.Validations;
using NUnit.Framework;

namespace JudoPayDotNetIntegrationTests
{
    [TestFixture]
    public class ThreeDAuthorizationTests : IntegrationTestsBase
    {
        // Prod Sandbox transaction
        [Test]
        public void PaymentWithThreedSecure()
        {
            var paymentWithCard = GetCardPaymentModel("432438862", "4976350000006891", "341", judoId: Configuration.Judoid);
            paymentWithCard.MobileNumber = "07123456789";
            paymentWithCard.PhoneCountryCode = "44";
            paymentWithCard.EmailAddress = "test@gmail.com";
            paymentWithCard.UserAgent = "Mozilla/5.0,(Windows NT 6.1; WOW64),AppleWebKit/537.36,(KHTML, like Gecko),Chrome/33.0.1750.154,Safari/537.36";
            paymentWithCard.AcceptHeaders = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            paymentWithCard.DeviceCategory = "Mobile";

            var paymentsFactory = JudoPaymentsFactory.Create(Configuration.JudoEnvironment, Configuration.Token, Configuration.Secret);
            var response = paymentsFactory.Payments.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentRequiresThreeDSecureModel;

            Assert.IsNotNull(receipt);
            Assert.AreEqual("Requires 3D Secure", receipt.Result);
            Assert.IsNotEmpty(receipt.Md);
        }

        [Test]
        public void FullPaymentWithThreedSecure()
        {
            var paymentWithCard = GetCardPaymentModel("432438862", "4976350000006891", "341", "B42 1SX", judoId: Configuration.Judoid);
            paymentWithCard.MobileNumber = "07123456789";
            paymentWithCard.PhoneCountryCode = "44";
            paymentWithCard.EmailAddress = "test@gmail.com";
            paymentWithCard.UserAgent = "Mozilla/5.0,(Windows NT 6.1; WOW64),AppleWebKit/537.36,(KHTML, like Gecko),Chrome/33.0.1750.154,Safari/537.36";
            paymentWithCard.AcceptHeaders = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            paymentWithCard.DeviceCategory = "Mobile";

            var paymentsFactory = JudoPaymentsFactory.Create(Configuration.JudoEnvironment, Configuration.Token, Configuration.Secret);
            var response = paymentsFactory.Payments.Create(paymentWithCard).Result;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);

            var receipt = response.Response as PaymentRequiresThreeDSecureModel;

            Assert.IsNotNull(receipt);
            Assert.AreEqual("Requires 3D Secure", receipt.Result);
            Assert.IsNotEmpty(receipt.Md);

            var httpClient = new HttpClient();
            var formContent = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("MD", receipt.Md),
				new KeyValuePair<string, string>("PaReq", receipt.PaReq),
				new KeyValuePair<string, string>("TermUrl", "https://pay.judopay.com/")
            });

            var taskSendMDandPaReqToAcsServer = httpClient.PostAsync(receipt.AcsUrl, formContent).ContinueWith(authResponse =>
            {
                var resultBody = authResponse.Result.Content.ReadAsStringAsync().Result;

                /* ok this next bit is a hack. I know on Iridium's ACS simulator the PaRes value is lurking in the HTML (It's a simulator after all!) */
                var doc = new HtmlDocument();
                doc.LoadHtml(resultBody);

                var formField = doc.DocumentNode.SelectSingleNode("//input[@name='PaRes']");

                var paResValue = formField.GetAttributeValue("value", "");

                Assert.That(paResValue, Is.Not.Empty);

                var threeDResult = paymentsFactory.ThreeDs.Complete3DSecure(receipt.ReceiptId, new ThreeDResultModel { PaRes = paResValue, Md = receipt.Md }).Result;

                Assert.IsNotNull(threeDResult);
                Assert.IsFalse(threeDResult.HasError);
                Assert.AreEqual("Success", threeDResult.Response.Result);

            });

            taskSendMDandPaReqToAcsServer.Wait();
        }

        public CardPaymentModel PrepareThreeDSecureTwoCardPayment()
        {
            var paymentWithCard = GetCardPaymentModel("DotNetASC123", "4000023104662535", "452", judoId: Configuration.SafeCharge_Judoid);

            paymentWithCard.CardHolderName = "CHALLENGE";
            paymentWithCard.MobileNumber = "07999999999";
            paymentWithCard.PhoneCountryCode = "44";
            paymentWithCard.EmailAddress = "contact@judopay.com";

            paymentWithCard.UserAgent = "Mozilla/5.0,(Windows NT 6.1; WOW64),AppleWebKit/537.36,(KHTML, like Gecko),Chrome/33.0.1750.154,Safari/537.36";
            paymentWithCard.AcceptHeaders = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            paymentWithCard.DeviceCategory = "Mobile";

            paymentWithCard.ThreeDSecure = new ThreeDSecureTwoModel
            {
                AuthenticationSource = ThreeDSecureTwoAuthenticationSource.Browser,
                MethodNotificationUrl = "https://www.test.com",
                ChallengeNotificationUrl = "https://www.test.com",
                ChallengeRequestIndicator = ThreeDSecureTwoChallengeRequestIndicator.NoPreference,
                ScaExemption = ThreeDSecureTwoScaExemption.LowValue
            };

            return paymentWithCard;
        }

        [Test]
        public void PaymentWithThreedSecureTwoRequiresDeviceDetailsCheck()
        {
            var paymentModel = PrepareThreeDSecureTwoCardPayment();

            var paymentsFactory = JudoPaymentsFactory.Create(Configuration.JudoEnvironment, Configuration.SafeCharge_Token, Configuration.SafeCharge_Secret);
            var paymentResponse = paymentsFactory.Payments.Create(paymentModel).Result;

            Assert.IsNotNull(paymentResponse);
            Assert.IsFalse(paymentResponse.HasError);

            var receipt = paymentResponse.Response as PaymentRequiresThreeDSecureTwoModel;

            Assert.IsNotNull(receipt);
            Assert.AreEqual("Additional device data is needed for 3D Secure 2", receipt.Result);
            Assert.AreEqual("Issuer ACS has requested additional device data gathering", receipt.Message);
            Assert.IsNotNull(receipt.MethodUrl);
            Assert.IsNotNull(receipt.Md);
            Assert.IsNotNull(receipt.Version);

            Assert.IsNull(receipt.ChallengeUrl);
        }

        [Test]
        public void PaymentWithThreedSecureTwoResumeRequest()
        {
            var paymentsFactory = JudoPaymentsFactory.Create(Configuration.JudoEnvironment, Configuration.SafeCharge_Token, Configuration.SafeCharge_Secret);
            var paymentResponse = paymentsFactory.Payments.Create(PrepareThreeDSecureTwoCardPayment()).Result;

            Assert.IsNotNull(paymentResponse);
            Assert.IsFalse(paymentResponse.HasError);

            var paymentReceipt = paymentResponse.Response as PaymentRequiresThreeDSecureTwoModel;

            Assert.IsNotNull(paymentReceipt);
            Assert.AreEqual("Additional device data is needed for 3D Secure 2", paymentReceipt.Result);

            // Prepare the resume request once device details gathering happened 
            var resumeRequest = new ResumeThreeDSecureTwoModel {CV2 = "452", MethodCompletion = MethodCompletion.Yes};
            var resumeResponse = paymentsFactory.ThreeDs.Resume3DSecureTwo(paymentReceipt.ReceiptId, resumeRequest).Result;

            Assert.IsNotNull(resumeResponse);
            Assert.IsFalse(resumeResponse.HasError);

            var resumeReceipt = resumeResponse.Response as PaymentRequiresThreeDSecureTwoModel;

            Assert.IsNotNull(resumeReceipt);
            Assert.AreEqual("Challenge completion is needed for 3D Secure 2", resumeReceipt.Result);
            Assert.AreEqual("Issuer ACS has responded with a Challenge URL", resumeReceipt.Message);
            Assert.IsNotNull(resumeReceipt.ChallengeUrl);
            Assert.IsNotNull(resumeReceipt.Md);
            Assert.IsNotNull(resumeReceipt.Version);
            Assert.IsNotNull(resumeReceipt.CReq);

            Assert.IsNull(resumeReceipt.MethodUrl);
        }

        [Test]
        public void PaymentWithThreedSecureTwoResumeRequestNoCv2()
        {
            var paymentsFactory = JudoPaymentsFactory.Create(Configuration.JudoEnvironment, Configuration.SafeCharge_Token, Configuration.SafeCharge_Secret);
            var paymentResponse = paymentsFactory.Payments.Create(PrepareThreeDSecureTwoCardPayment()).Result;

            Assert.IsNotNull(paymentResponse);
            Assert.IsFalse(paymentResponse.HasError);

            var paymentReceipt = paymentResponse.Response as PaymentRequiresThreeDSecureTwoModel;

            Assert.IsNotNull(paymentReceipt);
            Assert.AreEqual("Additional device data is needed for 3D Secure 2", paymentReceipt.Result);

            // Given a Resume request without CV2
            var resumeRequest = new ResumeThreeDSecureTwoModel {MethodCompletion = MethodCompletion.Yes };

            // When the request is sent to the API 
            // Then the request is not flagged as invalid by the SDK
            var resumeResponse = paymentsFactory.ThreeDs.Resume3DSecureTwo(paymentReceipt.ReceiptId, resumeRequest).Result;
            
            // Then the request is rejected at the API level instead because the test API application is expecting a CV2
            Assert.IsNotNull(resumeResponse);
            Assert.IsTrue(resumeResponse.HasError);
            Assert.AreEqual("Cv2", resumeResponse.Error.ModelErrors.First().FieldName);
        }

        [Explicit]
        public void PaymentWithThreedSecureTwoCompleteRequest()
        {
            var paymentsFactory = JudoPaymentsFactory.Create(Configuration.JudoEnvironment, Configuration.SafeCharge_Token, Configuration.SafeCharge_Secret);
            var paymentResponse = paymentsFactory.Payments.Create(PrepareThreeDSecureTwoCardPayment()).Result;

            Assert.IsNotNull(paymentResponse);
            Assert.IsFalse(paymentResponse.HasError);

            var paymentReceipt = paymentResponse.Response as PaymentRequiresThreeDSecureTwoModel;

            Assert.IsNotNull(paymentReceipt);
            Assert.AreEqual("Additional device data is needed for 3D Secure 2", paymentReceipt.Result);

            // Prepare the resume request once device details gathering happened 
            var resumeRequest = new ResumeThreeDSecureTwoModel { CV2 = "452", MethodCompletion = MethodCompletion.Yes };
            var resumeResponse = paymentsFactory.ThreeDs.Resume3DSecureTwo(paymentReceipt.ReceiptId, resumeRequest).Result;

            Assert.IsNotNull(resumeResponse);
            Assert.IsFalse(resumeResponse.HasError);

            var resumeReceipt = resumeResponse.Response as PaymentRequiresThreeDSecureTwoModel;

            Assert.IsNotNull(resumeReceipt);
            Assert.AreEqual("Challenge completion is needed for 3D Secure 2", resumeReceipt.Result);

            // Perform the challenge on the web browser using the information from the Resume

            // Then prepare the Complete request 
            var completeRequest = new CompleteThreeDSecureTwoModel { CV2 = "452" };
            var completeResponse = paymentsFactory.ThreeDs.Complete3DSecureTwo(paymentReceipt.ReceiptId, completeRequest).Result;

            Assert.IsNotNull(completeResponse);
            Assert.IsFalse(completeResponse.HasError);

            var completeReceipt = resumeResponse.Response as PaymentReceiptModel;
            Assert.IsNotNull(completeReceipt);
            Assert.IsNotNull(completeReceipt.JudoId);
            Assert.AreEqual("Success", completeReceipt.Result);
        }

        [Test]
        public void CheckCardWithThreedSecureTwoRequiresDeviceDetailsCheck()
        {
            // Given a CheckCard request model
            var checkCardPayment = GetCheckCardModel(Configuration.SafeCharge_Judoid, "4976000000003436", "452");

            checkCardPayment.CardHolderName = "CHALLENGE";
            checkCardPayment.MobileNumber = "07999999999";
            checkCardPayment.PhoneCountryCode = "34";
            checkCardPayment.EmailAddress = "contact@judopay.com";
            checkCardPayment.Currency = "GBP";

            checkCardPayment.InitialRecurringPayment = false;
            checkCardPayment.RecurringPayment = false;
            checkCardPayment.RecurringPaymentType = RecurringPaymentType.Unknown;
            checkCardPayment.RelatedReceiptId = string.Empty;

            checkCardPayment.ThreeDSecure = new ThreeDSecureTwoModel
            {
                AuthenticationSource = ThreeDSecureTwoAuthenticationSource.Browser,
                MethodNotificationUrl = "https://www.test.com",
                ChallengeNotificationUrl = "https://www.test.com",
                ChallengeRequestIndicator = ThreeDSecureTwoChallengeRequestIndicator.NoPreference,
                ScaExemption = ThreeDSecureTwoScaExemption.LowValue
            };

            // When a request to the CheckCards endpoint is made with a CardHolderName
            // that requires a device details check
            var paymentsFactory = JudoPaymentsFactory.Create(Configuration.JudoEnvironment, Configuration.SafeCharge_Token, Configuration.SafeCharge_Secret);
            var checkCardResponse = paymentsFactory.CheckCards.Create(checkCardPayment).Result;

            Assert.IsNotNull(checkCardResponse);
            Assert.IsFalse(checkCardResponse.HasError);

            // Then a response indicating a 3DS2 device details check is required is received
            var deviceDetailsResponse = checkCardResponse.Response as PaymentRequiresThreeDSecureTwoModel;
            Assert.IsNotNull(deviceDetailsResponse);
            Assert.IsNotNull(deviceDetailsResponse.ReceiptId);
            Assert.IsNotNull(deviceDetailsResponse.Md);
        }

        [Test, TestCaseSource(typeof(ThreeDTestSource), nameof(ThreeDTestSource.ValidateFailureTestCases))]
        public void ValidateWithoutSuccess(ThreeDResultModel threeDsModel, JudoModelErrorCode expectedModelErrorCode)
        {
            var threeDsResult = JudoPayApiIridium.ThreeDs.Complete3DSecure(686563432945848320, threeDsModel).Result;

            Assert.NotNull(threeDsResult);
            Assert.IsTrue(threeDsResult.HasError);
            Assert.IsNull(threeDsResult.Response);
            Assert.IsNotNull(threeDsResult.Error);
            Assert.AreEqual((int)JudoApiError.General_Model_Error, threeDsResult.Error.Code);

            var fieldErrors = threeDsResult.Error.ModelErrors;
            Assert.IsNotNull(fieldErrors);
            Assert.IsTrue(fieldErrors.Count >= 1);
            Assert.IsTrue(fieldErrors.Any(x => x.Code == (int)expectedModelErrorCode));
        }

        internal class ThreeDTestSource
        {
            public static IEnumerable ValidateFailureTestCases
            {
                get
                {
                    yield return new TestCaseData(new ThreeDResultModel
                    {
                        Md = "210309131405694701132842",
                        PaRes = null
                    }, JudoModelErrorCode.ThreeDSecure_PaRes_Not_Supplied).SetName("ValidateThreeDsMissingPaRes"); // No ReceiptId_Not_Supplied as ReceiptId will be set to 0 as it is not null
                    yield return new TestCaseData(new ThreeDResultModel
                    {
                        Md = "210309131405694701132842",
                        PaRes = ""
                    }, JudoModelErrorCode.ThreeDSecure_PaRes_Not_Supplied).SetName("ValidateThreeDsEmptyPaRes");
                 }
            }
        }

    }
}
