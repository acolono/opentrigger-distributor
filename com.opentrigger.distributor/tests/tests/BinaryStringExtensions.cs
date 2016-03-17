using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using com.opentrigger.distributord;
using NUnit.Framework;

namespace com.opentrigger.tests
{
    [TestFixture]
    public class BinaryStringExtensions
    {
        [Test]
        public void TryNunit()
        {
            Assert.IsTrue("FFAA".ToBytes().ToHexString() == "FFAA");
        }
    }
}
