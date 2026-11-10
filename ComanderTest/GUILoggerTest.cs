using System;
using System.IO;
using Comander.Core;
using LogLib;
using NUnit.Framework;

namespace ComanderTest
{
    // The status bar gets a short "what: why" line; stack traces belong in the log file only.
    [TestFixture]
    public class GUILoggerTest
    {
        private static LogMsg Capture(Action<GUILogger> log)
        {
            var logger = new GUILogger();
            LogMsg result = null;
            logger.NotifyEvent += (info, message) => result = new LogMsg(info, message);
            log(logger);
            return result;
        }

        private static Exception Thrown()
        {
            try { throw new IOException("Access denied"); }
            catch (Exception ex) { return ex; }
        }

        [Test]
        public void ErrorWithMessage_AppendsReason()
        {
            var msg = Capture(l => l.Error("Cannot copy a.txt", Thrown()));
            Assert.That(msg.Message, Is.EqualTo("Cannot copy a.txt: Access denied"));
            Assert.That(msg.IsError, Is.True);
        }

        [Test]
        public void ErrorWithException_ShowsMessageWithoutStackTrace()
        {
            var msg = Capture(l => l.Error(Thrown()));
            Assert.That(msg.Message, Is.EqualTo("Access denied"));
        }

        [Test]
        public void Fatal_ShowsReasonAndPointsToLogFile()
        {
            var msg = Capture(l => l.Fatal(Thrown()));
            Assert.That(msg.Message, Is.EqualTo("Unexpected error: Access denied (details in the log file)"));
        }

        [Test]
        public void Warning_IsMarkedAsWarning()
        {
            var msg = Capture(l => l.Warn("Disk almost full"));
            Assert.That(msg.IsWarning, Is.True);
            Assert.That(msg.IsError, Is.False);
        }
    }
}
