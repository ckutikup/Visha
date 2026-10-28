using NUnit.Framework;
using UnityEngine;
using Visha.Campaign;

public class VaultArrivalTests
{
    [Test]
    public void ContinueRunsOnceAndCancelDoesNotEnter()
    {
        var root = new GameObject("Root", typeof(RectTransform));
        var view = root.AddComponent<VaultArrivalView>();
        int entered = 0, cancelled = 0;
        try
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            view.Show(root.transform, font, () => entered++, () => cancelled++);
            view.Continue(); view.Continue();
            Assert.AreEqual(1, entered);
            Assert.IsFalse(view.IsOpen);
            view.Show(root.transform, font, () => entered++, () => cancelled++);
            view.Cancel(); view.Continue();
            Assert.AreEqual(1, entered);
            Assert.AreEqual(1, cancelled);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
