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
            Debug.WriteLine(new byte[] { 0xAA }.ToHexString());
            Assert.IsTrue("FF".ToBytes() == new byte[] {0xFF});
            Assert.IsTrue(new byte[] {0xAA}.ToHexString() == "AA");
        }
    }
}
