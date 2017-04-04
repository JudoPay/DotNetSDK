﻿using System.Threading.Tasks;
using JudoPayDotNet.Models;

namespace JudoPayDotNet.Clients
{
    /// <summary>
    /// The entity responsible for providing refunds operations
    /// </summary>
    public interface IRefunds
    {
        /// <summary>
        /// Creates the specified refund.
        /// </summary>
        /// <param name="refund">The refund.</param>
        /// <returns>The receipt for the created refund</returns>
        Task<IResult<ITransactionResult>> Create(RefundModel refund);
    }
}