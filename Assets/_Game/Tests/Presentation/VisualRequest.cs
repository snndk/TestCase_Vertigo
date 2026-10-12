using System;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class VisualRequest
    {
        public string Stage { get; }
        public SpinResult Spin { get; }
        public Action<VisualResult> Complete { get; }

        public VisualRequest(string stage, SpinResult spin, Action<VisualResult> complete)
        {
            Stage = stage;
            Spin = spin;
            Complete = complete;
        }
    }
}
