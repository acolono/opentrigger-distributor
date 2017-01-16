using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using com.opentrigger.distributord.Plugins;
using NUnit.Core;
using NUnit.Framework;

namespace com.opentrigger.tests
{
    [TestFixture]
    public class PluginTests
    {
        [Test]
        public void BuiltinDummy()
        {
            var sw = new Stopwatch();
            var dummy = typeof(DummyPlugin);
            sw.Start();
            Plugins.StartPlugins(new []{"--dummy"});
            sw.Stop();
            var pluginCount = Plugins.ActivatedPlugins.Count;
            Console.WriteLine($"cnt={pluginCount} msec={sw.ElapsedMilliseconds}");
            Console.WriteLine(string.Join(", ", Plugins.ActivatedPlugins.Keys.Select(k => k.FullName)));

            var activeDummy = Plugins.ActivatedPlugins[dummy] as DummyPlugin;
            Assert.IsNotNull(activeDummy);

            var activeEmpty = Plugins.ActivatedPlugins[typeof(EmptyPlugin)];
            Assert.IsNotNull(activeEmpty);
        }

        private class FailingPlugin : IPlugin
        {
            public void Start(string[] cmdlineArgs)
            {
                if(cmdlineArgs.Contains("--failing")) throw new Exception("die");
            }
        }

        private class EmptyPlugin : IPlugin
        {
            public void Start(string[] cmdlineArgs)
            {
            }
        }

        [Test]
        public void Failing()
        {
            var name = typeof(FailingPlugin).FullName;
            var cnt = 0;
            try
            {
                Plugins.StartPlugins(new [] {"--failing"});
            }
            catch (Exception e)
            {
                if (e.Message.Contains(name))
                {
                    Assert.AreEqual(e.InnerException?.Message,"die");
                    cnt++;
                }
                else
                {
                    throw;
                }
                
            }
            Assert.AreEqual(cnt,1);
        }
    }
}
