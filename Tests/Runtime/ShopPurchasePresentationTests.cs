using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Dreamy.Shop.Tests
{
    public sealed class ShopPurchasePresentationTests
    {
        private sealed class Scope : IDisposable
        {
            public int Disposals;
            public void Dispose() => Disposals++;
        }

        private sealed class View : IShopView, IShopPurchasePresentation
        {
            public event Action<string> PurchaseRequested;
            public event Action CloseRequested;
            public Scope LastScope;
            public CancellationToken Token;
            public int Results;
            public IDisposable BeginPurchasePresentation(string offerId, CancellationToken cancellation)
            {
                Token = cancellation;
                return LastScope = new Scope();
            }
            public void Render(ShopViewState state) { }
            public void SetPurchaseInteractable(bool interactable) { }
            public void ShowPurchaseResult(ShopPurchaseResult result) => Results++;
            public void Close() { }
            public void Buy() => PurchaseRequested?.Invoke("offer");
            public void RequestClose() => CloseRequested?.Invoke();
        }

        private sealed class Service : IShopService
        {
            public Func<UniTask<ShopPurchaseResult>> PurchaseAction;
            public ShopViewState GetState() => new(Array.Empty<ShopOfferViewState>());
            public ShopPurchaseResult Purchase(string id) => ShopPurchaseResult.Purchased(null);
            public UniTask<ShopPurchaseResult> PurchaseAsync(string id, CancellationToken token = default) => PurchaseAction();
        }

        [Test]
        public void ScopeStartsBeforeMutation_AndWaitsForPresentation()
        {
            var view = new View();
            var finish = new UniTaskCompletionSource();
            var service = new Service { PurchaseAction = () =>
            {
                Assert.That(view.LastScope, Is.Not.Null, "Scope must exist before the service can mutate wallet.");
                Assert.That(view.LastScope.Disposals, Is.Zero);
                return UniTask.FromResult(ShopPurchaseResult.Purchased(null));
            }};
            using var presenter = new ShopPresenter(service, view, _ => finish.Task);
            presenter.Show(); view.Buy();
            Assert.That(view.LastScope.Disposals, Is.Zero);
            finish.TrySetResult();
            Assert.That(view.LastScope.Disposals, Is.EqualTo(1));
            Assert.That(view.Results, Is.EqualTo(1));
        }

        [Test]
        public void Close_ReleasesImmediatelyWhileServiceIsPending_OldCompletionCannotReleaseNewScope()
        {
            var view = new View();
            var first = new UniTaskCompletionSource<ShopPurchaseResult>();
            var second = new UniTaskCompletionSource<ShopPurchaseResult>();
            var service = new Service { PurchaseAction = () => first.Task };
            using var presenter = new ShopPresenter(service, view);
            presenter.Show(); view.Buy();
            var old = view.LastScope;
            CancellationToken oldToken = view.Token;
            view.RequestClose();
            Assert.That(old.Disposals, Is.EqualTo(1));
            Assert.That(oldToken.IsCancellationRequested, Is.True);
            service.PurchaseAction = () => second.Task;
            presenter.Show(); view.Buy();
            var current = view.LastScope;
            first.TrySetResult(ShopPurchaseResult.Purchased(null));
            Assert.That(old.Disposals, Is.EqualTo(1));
            Assert.That(current.Disposals, Is.Zero);
            Assert.That(view.Results, Is.Zero);
            second.TrySetResult(ShopPurchaseResult.Purchased(null));
            Assert.That(current.Disposals, Is.EqualTo(1));
            Assert.That(view.Results, Is.EqualTo(1));
        }

        [Test]
        public void FailedPurchase_ReleasesBeforeRecovery_AndDoesNotWaitForAcquisition()
        {
            var view = new View();
            var recovery = new UniTaskCompletionSource();
            var service = new Service { PurchaseAction = () => UniTask.FromResult(
                ShopPurchaseResult.InsufficientResources(null, default)) };
            using var presenter = new ShopPresenter(service, view, recoverResources: (_, token) =>
            {
                Assert.That(view.LastScope.Disposals, Is.EqualTo(1));
                return recovery.Task;
            });
            presenter.Show(); view.Buy();
            Assert.That(view.LastScope.Disposals, Is.EqualTo(1));
            presenter.Dispose();
            recovery.TrySetResult();
            Assert.That(view.LastScope.Disposals, Is.EqualTo(1));
        }

        [Test]
        public void PurchaseException_ReleasesScope_AndReportsError()
        {
            var view = new View();
            var expected = new InvalidOperationException("purchase failed");
            var service = new Service { PurchaseAction = () => throw expected };
            Exception reported = null;
            Action<Exception> onError = error => reported = error;
            UniTaskScheduler.UnobservedTaskException += onError;
            try
            {
                using var presenter = new ShopPresenter(service, view);
                presenter.Show(); view.Buy();
                Assert.That(view.LastScope.Disposals, Is.EqualTo(1));
                Assert.That(reported, Is.SameAs(expected));
                Assert.That(view.Results, Is.Zero);
            }
            finally { UniTaskScheduler.UnobservedTaskException -= onError; }
        }

        [Test]
        public void CancellationException_ReleasesScope()
        {
            var view = new View();
            var pending = new UniTaskCompletionSource<ShopPurchaseResult>();
            var service = new Service { PurchaseAction = () => pending.Task };
            using var presenter = new ShopPresenter(service, view);
            presenter.Show(); view.Buy();
            presenter.Dispose();
            pending.TrySetCanceled(view.Token);
            Assert.That(view.LastScope.Disposals, Is.EqualTo(1));
            Assert.That(view.Results, Is.Zero);
        }
    }
}
