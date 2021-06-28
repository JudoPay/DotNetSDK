using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JudoPayDotNet.Models;
using JudoPayDotNet.Models.Validations;
using NUnit.Framework;

namespace JudoPayDotNetIntegrationTests
{
    [TestFixture]
    public class WebPaymentsTests : IntegrationTestsBase
    {
        [Test]
        public void PaymentCreate()
        {
            var request = GetWebPaymentRequestModel();

            var result = JudoPayApiElevated.WebPayments.Payments.Create(request).Result;

            Assert.NotNull(result);
            Assert.IsFalse(result.HasError);
            Assert.NotNull(result.Response);
            Assert.NotNull(result.Response.Reference);
            Assert.NotNull(result.Response.PostUrl);
        }

        [Test]
        public void PaymentUpdate()
        {
            var request = GetWebPaymentRequestModel();

            var result = JudoPayApiElevated.WebPayments.Payments.Create(request).Result;

            Assert.NotNull(result);
            Assert.IsFalse(result.HasError);
            Assert.NotNull(result.Response);
            Assert.NotNull(result.Response.Reference);
            Assert.NotNull(result.Response.PostUrl);

            request.Status = WebPaymentStatus.Success;
            request.Reference = result.Response.Reference;

            var resultUpdate = JudoPayApiElevated.WebPayments.Payments.Update(request).Result;

            Assert.NotNull(resultUpdate);
            Assert.IsFalse(resultUpdate.HasError);
            Assert.NotNull(resultUpdate.Response); //todo pick a judoID that has permissions to do this
            Assert.NotNull(resultUpdate.Response.Reference);
            Assert.AreEqual(result.Response.Reference, resultUpdate.Response.Reference);
            Assert.AreEqual(resultUpdate.Response.Status, resultUpdate.Response.Status);
        }

        [Test]
        public void PreAuthCreate()
        {
            var request = GetWebPaymentRequestModel();

            var result = JudoPayApiElevated.WebPayments.PreAuths.Create(request).Result;

            Assert.NotNull(result);
            Assert.IsFalse(result.HasError);
            Assert.NotNull(result.Response);
            Assert.NotNull(result.Response.Reference);
            Assert.NotNull(result.Response.PostUrl);
        }

        [Test]
        public void PreAuthUpdate()
        {
            var request = GetWebPaymentRequestModel();

            var result = JudoPayApiElevated.WebPayments.PreAuths.Create(request).Result;

            Assert.NotNull(result);
            Assert.IsFalse(result.HasError);
            Assert.NotNull(result.Response);
            Assert.NotNull(result.Response.Reference);
            Assert.NotNull(result.Response.PostUrl);

            request.Status = WebPaymentStatus.Success;
            request.Reference = result.Response.Reference;

            var resultUpdate = JudoPayApiElevated.WebPayments.PreAuths.Update(request).Result;

            Assert.NotNull(resultUpdate);
            Assert.IsFalse(resultUpdate.HasError);
            Assert.NotNull(resultUpdate.Response);
            Assert.NotNull(resultUpdate.Response.Reference);
            Assert.AreEqual(result.Response.Reference, resultUpdate.Response.Reference);
            Assert.AreEqual(resultUpdate.Response.Status, resultUpdate.Response.Status);
        }

        [Test]
        public void TransactionsGetByReference()
        {
            var request = GetWebPaymentRequestModel();

            var result = JudoPayApiElevated.WebPayments.Payments.Create(request).Result;

            Assert.NotNull(result);
            Assert.IsFalse(result.HasError);
            Assert.NotNull(result.Response);
            Assert.NotNull(result.Response.Reference);
            Assert.NotNull(result.Response.PostUrl);

            var webRequest = JudoPayApiElevated.WebPayments.Transactions.Get(result.Response.Reference).Result;

            Assert.NotNull(webRequest);
            Assert.IsFalse(webRequest.HasError);
            Assert.NotNull(webRequest.Response);
            Assert.NotNull(webRequest.Response.Reference);
            Assert.AreEqual(result.Response.Reference, webRequest.Response.Reference);
            Assert.AreEqual(request.JudoId, webRequest.Response.JudoId);
        }

        [Test]
        public void TransactionsGetByReceiptId()
        {
            var request = GetWebPaymentRequestModel();

            var result = JudoPayApiIridium.WebPayments.Payments.Create(request).Result;

            var reference = result.Response.Reference;

            // Forms - Post a form with credentials to post url from the webpayment response passing form parameter Reference

            var httpClient = new HttpClient();
            var formContent = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("Reference", reference)
            });

            // The test works only with webpayments v1, so we need to alter the URL version if the account is set to use v2 
            result.Response.PostUrl = result.Response.PostUrl.Replace("/v2", "/v1");

            var formRequest = CreateJudoApiRequest(result.Response.PostUrl, HttpMethod.Post, "5.3.0.0", Configuration.ElevatedPrivilegesToken, Configuration.ElevatedPrivilegesSecret);

            formContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            formRequest.Content = formContent;

            var paymentPage = httpClient.SendAsync(formRequest).Result;

            var resultBody = paymentPage.Content.ReadAsStringAsync().Result;

            /* ok this next bit is a hack. I know on Iridium's ACS simulator the PaRes value is lurking in the HTML (It's a simulator after all!) */
            var doc = new HtmlDocument();
            doc.LoadHtml(resultBody);

            // Forms - Post a form with credentials and cookie and form following variables:
            // url= /v1/Pay variables: CardNumber, ExpiryDate, Cv2, form variable: __RequestVerificationToken

            var formField = doc.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']");

            var requestVerificationToken = formField.GetAttributeValue("value", "");

                // CardNumber = "4976000000003436",
                // CV2 = "452",
                // ExpiryDate = "12/25",

            formContent = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", requestVerificationToken),
                new KeyValuePair<string, string>("CardNumber", "4976000000003436"),
                new KeyValuePair<string, string>("Cv2", "452"), 
                new KeyValuePair<string, string>("CardAddress.CountryCode", "826"), 
                new KeyValuePair<string, string>("CardAddress.PostCode", "TR14 8PA"), 
                new KeyValuePair<string, string>("ExpiryDate", "12/25"), 
                new KeyValuePair<string, string>("Reference", reference),
                new KeyValuePair<string, string>("YourConsumerReference", "4235325"), 
            });

            formRequest = CreateJudoApiRequest(Configuration.WebpaymentsUrl, HttpMethod.Post, "5.3.0.0", Configuration.ElevatedPrivilegesToken, Configuration.ElevatedPrivilegesSecret);

            formContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            formRequest.Content = formContent;

            var resultPage = httpClient.SendAsync(formRequest).Result;

            resultBody = resultPage.Content.ReadAsStringAsync().Result;

            // Retrieve from the response the receipt id

            doc = new HtmlDocument();
            doc.LoadHtml(resultBody);

            formField = doc.DocumentNode.SelectSingleNode("//input[@name='ReceiptId']");

            var receiptId = formField.GetAttributeValue("value", "");

            var webRequest = JudoPayApiIridium.WebPayments.Transactions.GetByReceipt(receiptId).Result;

            Assert.NotNull(webRequest);
            Assert.IsFalse(webRequest.HasError);
            Assert.NotNull(webRequest.Response);
            Assert.NotNull(webRequest.Response.Reference);
            Assert.AreEqual(request.JudoId, webRequest.Response.JudoId);
        }

        private HttpRequestMessage CreateJudoApiRequest(string url, HttpMethod method, string apiVersion, string apiToken, string apiSecret)
        {
            var request = new HttpRequestMessage(method, url);

            var full = string.Format("{0}:{1}", apiToken, apiSecret);

            var authDetails = Encoding.GetEncoding("iso-8859-1").GetBytes(full);
            var parameter = Convert.ToBase64String(authDetails);

            request.Headers.Add("Accept", "application/json");

            request.Headers.Add("Api-Version", apiVersion);

            request.Headers.Add("User-Agent", "Mozilla");

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", parameter);

            return request;
        }

        [Test]
        public async Task CreatePaymentSessionThenPayWithReference()
        {
            var request = GetWebPaymentRequestModel();

            var result = JudoPayApiElevated.WebPayments.Payments.Create(request).Result;

            Assert.NotNull(result);
            Assert.IsFalse(result.HasError);
            Assert.NotNull(result.Response);
            Assert.NotNull(result.Response.Reference);
            Assert.NotNull(result.Response.PostUrl);

            // Send payment request with WebPaymentReference set
            var paymentWithCard = GetCardPaymentModel();
            paymentWithCard.WebPaymentReference = result.Response.Reference;

            // For security reasons, fields need to match when the WebPaymentReference is used
            paymentWithCard.YourPaymentReference = request.YourPaymentReference;
            paymentWithCard.YourConsumerReference = request.YourConsumerReference;
            paymentWithCard.Amount = request.Amount;

            var response = await JudoPayApiIridium.Payments.Create(paymentWithCard);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.HasError);
            Assert.AreEqual("Success", response.Response.Result);

            // Can't check WebPaymentReference on receipt yet (until JR-4659 implemented)
        }

        [Test, TestCaseSource(typeof(WebPaymentsTestSource), nameof(WebPaymentsTestSource.ValidateFailureTestCases))]
        public void ValidateWithoutSuccess(WebPaymentRequestModel webPayment, JudoModelErrorCode expectedModelErrorCode)
        {

            var webPaymentResult = JudoPayApiIridium.WebPayments.Payments.Create(webPayment).Result;

            Assert.NotNull(webPaymentResult);
            Assert.IsTrue(webPaymentResult.HasError);
            Assert.IsNull(webPaymentResult.Response);
            Assert.IsNotNull(webPaymentResult.Error);
            Assert.AreEqual((int)JudoApiError.General_Model_Error, webPaymentResult.Error.Code);

            var fieldErrors = webPaymentResult.Error.ModelErrors;
            Assert.IsNotNull(fieldErrors);
            Assert.IsTrue(fieldErrors.Count >= 1);
            Assert.IsTrue(fieldErrors.Any(x => x.Code == (int)expectedModelErrorCode));
        }

        internal class WebPaymentsTestSource
        {
            public static IEnumerable ValidateFailureTestCases
            {
                get
                {
                    yield return new TestCaseData(new WebPaymentRequestModel
                    {
                        Amount = 1.20m,
                        JudoId = "Invalid",
                        YourConsumerReference = "User10"
                    }, JudoModelErrorCode.JudoId_Not_Valid_1).SetName("ValidateWebPaymentInvalidJudoId");
                    yield return new TestCaseData(new WebPaymentRequestModel
                    {
                        Amount = 1.20m,
                        JudoId = null,
                        YourConsumerReference = "User10"
                    }, JudoModelErrorCode.JudoId_Not_Supplied_1).SetName("ValidateWebPaymentMissingJudoId");
                    yield return new TestCaseData(new WebPaymentRequestModel
                    {
                        Amount = 1.20m,
                        JudoId = "100200302",
                        YourConsumerReference = null
                    }, JudoModelErrorCode.Consumer_Reference_Not_Supplied).SetName("ValidateWebPaymentMissingConsumerReference");
                    yield return new TestCaseData(new WebPaymentRequestModel
                    {
                        Amount = 1.20m,
                        JudoId = "100200302",
                        YourConsumerReference = ""
                    }, JudoModelErrorCode.Consumer_Reference_Not_Supplied).SetName("ValidateWebPaymentEmptyConsumerReference");
                    // Amount is not mandatory for WebPayments;
                    yield return new TestCaseData(new WebPaymentRequestModel
                    {
                        Amount = -1m,
                        JudoId = "100200302",
                        YourConsumerReference = "User10"
                    }, JudoModelErrorCode.Amount_Must_Be_Greater_Than_Or_Equal_To_0).SetName("ValidateWebPaymentNegativeAmount");
                }
            }
        }
    }
}
