﻿using System.Runtime.Serialization;

// ReSharper disable ClassNeverInstantiated.Global

namespace JudoPayDotNet.Models
{
    [DataContract]
    public class PKPaymentModel : PaymentModel
    {
        /// <summary>
        /// Gets or sets the apple pay token.
        /// </summary>
        /// <value>
        /// The apple pay token.
        /// </value>
        [DataMember(IsRequired = true)]
        public PKPaymentInnerModel PkPayment { get; set; }
    }
}
