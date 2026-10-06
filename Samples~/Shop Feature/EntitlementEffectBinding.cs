using System;
using Dreamy.Economy;

namespace Dreamy.Feature.Shop.Integration
{
    /// <summary>Applies an ownership effect once, from loaded or newly granted entitlement state.</summary>
    public sealed class EntitlementEffectBinding : IDisposable
    {
        private readonly IResourceBalanceSource source;
        private readonly ResourceId entitlementId;
        private readonly Action applyOwnedEffect;
        private readonly Action<Exception> reportError;
        private bool applied;
        private bool applying;
        private bool disposed;

        public EntitlementEffectBinding(
            IResourceBalanceSource source,
            ResourceId entitlementId,
            Action applyOwnedEffect,
            Action<Exception> reportError)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.entitlementId = entitlementId;
            this.applyOwnedEffect = applyOwnedEffect ?? throw new ArgumentNullException(nameof(applyOwnedEffect));
            this.reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
            source.BalanceChanged += OnBalanceChanged;
            Synchronize();
        }

        /// <summary>Call after SDK readiness or to retry a previously failed effect.</summary>
        public void Synchronize()
        {
            if (disposed || applied || applying || source.GetBalance(entitlementId) <= 0) return;
            applying = true;
            try
            {
                applyOwnedEffect();
                applied = true;
            }
            catch (Exception exception)
            {
                // The wallet has already saved the grant. SDK errors must not escape into purchasing.
                reportError(exception);
            }
            finally
            {
                applying = false;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            source.BalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(ResourceBalanceChanged change)
        {
            if (change.ResourceId.Equals(entitlementId)) Synchronize();
        }
    }
}
