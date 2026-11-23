using DeathNotice.Core;
using NUnit.Framework;

namespace DeathNotice.Tests.EditMode
{
    public class CoreInfoTests
    {
        [Test]
        public void Add_ReturnsSum()
        {
            Assert.AreEqual(5, CoreInfo.Add(2, 3));
        }

        [Test]
        public void GameName_IsSet()
        {
            Assert.AreEqual("Death Notice", CoreInfo.GameName);
        }
    }
}
