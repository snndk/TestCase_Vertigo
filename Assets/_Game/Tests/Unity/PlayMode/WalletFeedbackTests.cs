#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class WalletFeedbackTests : GameViewTestFixture
    {
        [UnityTest]
        public IEnumerator SpendingCountsDownWithoutRestartingOnRepeatedRenderAndSettlesOnDisable()
        {
            var wallet = Root.GetComponentInChildren<WalletFeedbackView>();
            var spending = Root.GetComponentsInChildren<TMP_Text>(true)
                .Single(text => text.name == "ui_text_wallet_spending_value");
            wallet.ShowBalance(225);
            wallet.ShowSpending(25, "Gold");
            Assert.That(Text("wallet_amount"), Is.EqualTo("250"));
            Assert.That(spending.text, Is.EqualTo("−25 Gold"));
            yield return new WaitForSecondsRealtime(0.15f);
            var displayed = long.Parse(Text("wallet_amount"));
            Assert.That(displayed, Is.InRange(226, 249));
            wallet.ShowBalance(225);
            Assert.That(Text("wallet_amount"), Is.EqualTo(displayed.ToString()));
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Wallet-spending.png")));
            yield return null;
            wallet.enabled = false;
            Assert.That(Text("wallet_amount"), Is.EqualTo("225"));
            Assert.That(spending.alpha, Is.Zero);
            wallet.enabled = true;
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(Text("wallet_amount"), Is.EqualTo("225"));
            Assert.That(spending.alpha, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ResetCancelsOldFeedbackAndLargeBalancesFinishExactly()
        {
            var wallet = Root.GetComponentInChildren<WalletFeedbackView>();
            var target = long.MaxValue - 25;
            wallet.ShowBalance(target);
            wallet.ShowSpending(25, "Gold");
            Assert.That(Text("wallet_amount"), Is.EqualTo(long.MaxValue.ToString("N0")));
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(Text("wallet_amount"), Is.EqualTo(target.ToString("N0")));
            wallet.ShowSpending(25, "Gold");
            wallet.ResetBalance();
            wallet.ShowBalance(100);
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(Text("wallet_amount"), Is.EqualTo("100"));
        }
    }
}
#endif
