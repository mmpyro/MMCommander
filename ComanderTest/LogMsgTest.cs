using Comander.Core;
using LogLib;
using NUnit.Framework;

namespace ComanderTest
{
    // BUG-27: Split('|') dropped everything after a second '|' and threw when there was none.
    [TestFixture]
    public class LogMsgTest
    {
        [Test]
        public void MessageWithPipes_IsKeptWhole()
        {
            var msg = new LogMsg(LogInfo.Error, "12:00|copy a|b failed");
            Assert.That(msg.Time, Is.EqualTo("12:00"));
            Assert.That(msg.Message, Is.EqualTo("copy a|b failed"));
        }

        [Test]
        public void MessageWithoutPipe_DoesNotThrow()
        {
            var msg = new LogMsg(LogInfo.Info, "no time here");
            Assert.That(msg.Message, Is.EqualTo("no time here"));
            Assert.That(msg.Time, Is.Empty);
        }
    }
}
