using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using JudoPayDotNet.Http;
using JudoPayDotNet.Logging;
using JudoPayDotNet.Models;

namespace JudoPayDotNet.Clients
{
    internal class ThreeDs : JudoPayClient, IThreeDs
    {
        private const string CompleteThreeDAuthorizationAddress = "transactions";

        private const string ResumeThreeDSecureTwoAddress = "resume3ds";

        private const string CompleteThreeDSecureTwoAddress = "complete3ds";


        private readonly IValidator<ThreeDResultModel> _threeDResultValidator = new InlineValidator<ThreeDResultModel>();

        private readonly IValidator<ResumeThreeDSecureTwoModel> _resumeThreeDSecureValidator = new InlineValidator<ResumeThreeDSecureTwoModel>();

        private readonly IValidator<CompleteThreeDSecureTwoModel> _completeThreeDSecureValidator = new InlineValidator<CompleteThreeDSecureTwoModel>();

        public ThreeDs(ILog logger, IClient client)
            : base(logger, client)
        {
        }

        /*
        *  To be called to complete a ThreeDSecure One transaction
        */
        public Task<IResult<PaymentReceiptModel>> Complete3DSecure(long receiptId, ThreeDResultModel model)
        {
            var validationError = Validate<ThreeDResultModel, PaymentReceiptModel>(_threeDResultValidator, model);

            var address = $"{CompleteThreeDAuthorizationAddress}/{receiptId}";

            // Do not call the API if validation fail
            return validationError ?? PutInternal<ThreeDResultModel, PaymentReceiptModel>(address, model);
        }

        /*
         *  To be called after device details gathering following the Issuer ACS request for a ThreeDSecure Two transaction
         */
        public Task<IResult<ITransactionResult>> Resume3DSecureTwo(long receiptId, ResumeThreeDSecureTwoModel model)
        {
            var validationError = Validate<ResumeThreeDSecureTwoModel, ITransactionResult>(_resumeThreeDSecureValidator, model);

            var address = $"transactions/{receiptId}/{ResumeThreeDSecureTwoAddress}";

            // Do not call the API if validation fail 
            return validationError ?? PutInternal<ResumeThreeDSecureTwoModel, ITransactionResult>(address, model);
        }

        /*
        *  To be called after the Issuer ACS challenge has been completed for a ThreeDSecure Two transaction
        */
        public Task<IResult<PaymentReceiptModel>> Complete3DSecureTwo(long receiptId, CompleteThreeDSecureTwoModel model)
        {
            var validationError = Validate<CompleteThreeDSecureTwoModel, PaymentReceiptModel>(_completeThreeDSecureValidator, model);

            var address = $"transactions/{receiptId}/{CompleteThreeDSecureTwoAddress}";

            // Do not call the API if validation fail 
            return validationError ?? PutInternal<CompleteThreeDSecureTwoModel, PaymentReceiptModel>(address, model);
        }
    }
}