using NUnit.Framework;
using UnityEngine.UIElements;

namespace Mortar.Unity.Tests
{
    public sealed class UiTests
    {
        [Test]
        public void TitledBoxesSetTheTitleIntoTheTopBorder()
        {
            var box = Ui.Box("inventory", "Inventory");

            var title = box.Q<Label>("title");
            Assert.IsNotNull(title);
            Assert.AreEqual("┤ Inventory ├", title.text);
            Assert.AreEqual(1f, box.style.borderTopWidth.value);
        }

        [Test]
        public void UntitledBoxesHaveNoTitle()
        {
            Assert.IsNull(Ui.Box("menu-box").Q<Label>("title"));
        }

        [Test]
        public void TheMonospaceFontApplies()
        {
            var root = new VisualElement();

            Assert.IsTrue(Ui.ApplyFont(root));
            Assert.IsNotNull(root.style.unityFontDefinition.value.font);
        }
    }
}
