using System;
using System.IO;
using System.Threading.Tasks;
using Comander.Core;
using Comander.ViewModel;
using LogLib;
using NUnit.Framework;

namespace ComanderTest
{
    [TestFixture]
    public class BusyStateTest
    {
        // The busy flag used to be static, so every StateMonitor shared one lock.
        [Test]
        public void StateMonitor_LocksPerInstance()
        {
            var a = new StateMonitor();
            var b = new StateMonitor();

            Assert.That(a.Enter(), Is.True);
            Assert.That(a.Enter(), Is.False, "second enter while busy");
            Assert.That(b.Enter(), Is.True, "another monitor is independent");
            a.Exit();
            Assert.That(a.Enter(), Is.True, "free again after exit");
        }

        // Toggling mode/recursive starts a new scan; the older one must neither throw nor leave the spinner on.
        [Test]
        public async Task CompareRescan_Overlapping_EndsIdle_WithFullResult()
        {
            var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-compare-" + Guid.NewGuid().ToString("N"))).FullName;
            try
            {
                var left = Directory.CreateDirectory(Path.Combine(root, "l")).FullName;
                var right = Directory.CreateDirectory(Path.Combine(root, "r")).FullName;
                System.IO.File.WriteAllText(Path.Combine(left, "a.txt"), "a");
                System.IO.File.WriteAllText(Path.Combine(left, "b.txt"), "b");
                var vm = new CompareVM(left, right, new JobQueue(new StateMonitor()), new FakeDialogs(), new ComplexLogger(new GUILogger()), null);

                await Task.WhenAll(vm.Rescan(), vm.Rescan(), vm.Rescan());

                Assert.That(vm.IsScanning, Is.False);
                Assert.That(vm.Rows, Has.Count.EqualTo(2));
                Assert.That(vm.Rows, Has.All.Matches<CompareRowVM>(r => r.OnlyLeft));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
