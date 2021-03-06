﻿using System.Runtime.Serialization;

namespace JudoPayDotNet.Models
{
    /// <summary>
    /// Data to register a card using a OneUseToken
    /// </summary>
    [DataContract]
    // ReSharper disable UnusedMember.Global
    public class RegisterEncryptedCardModel : RegisterCardModel
    {
        /// <summary>
        /// Gets or sets the one use token.
        /// </summary>
        /// <value>
        /// The one use token.
        /// </value>
        [DataMember(IsRequired = true)]
        public string OneUseToken { get; set; }
    }
}
