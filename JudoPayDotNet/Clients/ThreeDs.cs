using System.Threading.Tasks;
using JudoPayDotNet.Http;
using JudoPayDotNet.Logging;
using JudoPayDotNet.Models;
using JudoPayDotNet.Models.Internal;

namespace JudoPayDotNet.Clients
{
    internal class ThreeDs : JudoPayClient, IThreeDs
    {
        private const string CompleteThreeDAuthorizationAddress = "transactions";

        private const string ResumeThreeDSecureTwoAddress = "resume3ds";

        private const string CompleteThreeDSecureTwoAddress = "complete3ds";
        
        public ThreeDs(ILog logger, IClient client)
            : base(logger, client)
        {
        }

        /*
        *  To be called to complete a ThreeDSecure One transaction
        */
        public Task<IResult<PaymentReceiptModel>> Complete3DSecure(long receiptId, ThreeDResultModel model)
        {
            var address = $"{CompleteThreeDAuthorizationAddress}/{receiptId}";

            return PutInternal<ThreeDResultModel, PaymentReceiptModel>(address, model);
        }

        /*
         *  To be called after device details gathering following the Issuer ACS request for a ThreeDSecure Two transaction
         */
        public Task<IResult<ITransactionResult>> Resume3DSecureTwo(long receiptId, ResumeThreeDSecureTwoModel model)
        {
            var address = $"transactions/{receiptId}/{ResumeThreeDSecureTwoAddress}";

            // Convert SDK model to internal model
            var internalResumeThreeDSecureTwoModel = InternalResumeThreeDSecureTwoModel.From(model);

            // Send the request to the API 
            return PutInternal<InternalResumeThreeDSecureTwoModel, ITransactionResult>(address, internalResumeThreeDSecureTwoModel);
        }

        /*
        *  To be called after the Issuer ACS challenge has been completed for a ThreeDSecure Two transaction
        */
        public Task<IResult<PaymentReceiptModel>> Complete3DSecureTwo(long receiptId, CompleteThreeDSecureTwoModel model)
        {
            var address = $"transactions/{receiptId}/{CompleteThreeDSecureTwoAddress}";

            // Convert SDK model to internal model
            var internalCompleteThreeDSecureTwoModel = InternalCompleteThreeDSecureTwoModel.From(model);

            // Send the request to the API 
            return PutInternal<InternalCompleteThreeDSecureTwoModel, PaymentReceiptModel>(address, internalCompleteThreeDSecureTwoModel);
        }
    }
}