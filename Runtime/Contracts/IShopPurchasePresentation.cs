using System;
using System.Threading;

namespace Dreamy.Shop
{
    /// <summary>Optional view capability acquired before a purchase can mutate the wallet.</summary>
    public interface IShopPurchasePresentation
    {
        /// <summary>
        /// Starts a presentation lifetime for this offer. The presenter disposes it on success,
        /// failure, cancellation or disposal. The view must also clean up when hidden/disabled.
        /// No wallet mutation belongs in this method. The returned scope must be idempotent.
        /// </summary>
        IDisposable BeginPurchasePresentation(string offerId, CancellationToken cancellation);
    }
}
