using System;
using System.Collections.Generic;
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
            var dummy = typeof(DummyPlugin);
            Plugins.StartPlugins(new []{"--dummy"});
            Console.WriteLine(string.Join(", ", Plugins.ActivatedPlugins.Keys.Select(k => k.FullName)));

            var activeDummy = Plugins.ActivatedPlugins[dummy] as DummyPlugin;
            Assert.IsNotNull(activeDummy);
        }

        private class FailingPlugin : IPlugin
        {
            public void Start(string[] cmdlineArgs)
            {
                if(cmdlineArgs.Contains("--failing")) throw new Exception("die");
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
