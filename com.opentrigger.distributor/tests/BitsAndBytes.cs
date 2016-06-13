using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using com.opentrigger.distributord;
using NUnit.Core;
using NUnit.Framework;

namespace com.opentrigger.tests
{

    public static class Extensions
    {
        public static byte[] ReadBytes(this MemoryStream ms, int count)
        {
            var values = new byte[count];
            ms.Read(values, 0, count);
            return values;
        }

        public static byte[] ReadBits(this MemoryStream ms, int count)
        {
            if(count % 8 != 0) throw new Exception("Can Only read in 8 bit chunks");
            return ms.ReadBytes(count/8);
        }
    }

    [TestFixture]
    public class BitsAndBytes
    {
        [Test]
        public void Endian()
        {
            Trace.WriteLine($"isLittleEndian: {BitConverter.IsLittleEndian}");
            var intOne = BitConverter.GetBytes((int) 1);
            Trace.WriteLine($"1:{intOne.ToHexString()}");
            var flipOne = intOne.Reverse().ToArray();
            Trace.WriteLine($"R:{flipOne.ToHexString()}");

            var littleEndianOne = "01000000".ToBytes();
            var bigEndianOne = "00000001".ToBytes();

            var leconv = BitConverter.ToInt32(littleEndianOne.ConvertEndianness(distributord.Endian.LittleEndian), 0);
            var beconv = BitConverter.ToInt32(bigEndianOne.ConvertEndianness(distributord.Endian.BigEndian), 0);
            Trace.WriteLine($"beconv: {beconv}, leconv: {leconv}");

            Assert.IsTrue(beconv == 1);
            Assert.IsTrue(leconv == 1);

            using (var ms = new MemoryStream("0100000000000001".ToBytes()))
            {
                var le = ms.ReadBits(32).ConvertEndianness(distributord.Endian.LittleEndian);
                var be = ms.ReadBits(32).ConvertEndianness(distributord.Endian.BigEndian);

                using (var lems = new MemoryStream(le))
                using (var lebr = new BinaryReader(lems))
                {
                    var lestrconv = lebr.ReadInt32();
                    Trace.WriteLine("lestrconv=" + lestrconv);
                    Assert.IsTrue(lestrconv == 1);
                    
                }

                using (var bems = new MemoryStream(be))
                using (var bebr = new BinaryReader(bems))
                {
                    var bestrconv = bebr.ReadInt32();
                    Trace.WriteLine("bestrconv=" + bestrconv);
                    Assert.IsTrue(bestrconv == 1);
                    Assert.IsTrue(bebr.BaseStream.Position == bebr.BaseStream.Length);
                }

            }
        }

        [Test]
        public void Decoder()
        {
            var decoded = BtleDecoder.Decode("043e24020100011210111111f1180308313202010510ffeeff040111010a3b040e9007000a64b3");
            Assert.AreEqual(-77,decoded.Rssi);
            Assert.AreEqual("F1:11:11:11:10:12",decoded.Mac);
            Trace.WriteLine(decoded.Serialize());

            var litlePkg = BtleDecoder.Decode("043e0c020104011210111111f100b7");
            Trace.WriteLine(litlePkg.Serialize());
            Assert.AreEqual("F1:11:11:11:10:12", litlePkg.Mac);
            Assert.AreEqual(-73, litlePkg.Rssi);

            var someGoogleDevice = BtleDecoder.Decode("043e2b0201000055f0390960541f02010603039ffe17169ffe025150387a586741627357550000015370fde2afd6");
            Trace.WriteLine(someGoogleDevice.Serialize());
            Assert.AreEqual(someGoogleDevice.Mac,"54:60:09:39:F0:55");
            Assert.AreEqual(someGoogleDevice.Rssi,-42);

            var appleManufSpec = BtleDecoder.Decode("043e2a02010300002d00eef30c1e0201041aff4c000215699ebc80e1f311e39a0f0cf3ee3bc01200012d00c1ac");
            Trace.WriteLine(appleManufSpec.Serialize());
            Assert.AreEqual("0C:F3:EE:00:2D:00",appleManufSpec.Mac);
            Assert.AreEqual(-84,appleManufSpec.Rssi);
            Assert.IsNotNull(appleManufSpec.ManufacturerSpecific);

            var sensorTest1 = BtleDecoder.Decode("043e24020100011210111111f1180308313202010510ffeeff040111010a3b040e8d07000a64b2");
            Trace.WriteLine(sensorTest1.Serialize());

            //InvariantCulture ist used in library
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            Assert.AreEqual(26.19M, decimal.Parse(sensorTest1.ManufacturerSpecific.SensorData["Temperature"]));
            Assert.AreEqual("37.25", sensorTest1.ManufacturerSpecific.SensorData["Humidity"]);
            Assert.AreEqual("100", sensorTest1.ManufacturerSpecific.SensorData["BatteryCharge"]);
        }

        [Test]
        public void nRF51_PIR()
        {
            var ddDecoded = BtleDecoder.Decode("043E2502010000E7AAAAAAAAAA19020106030300A00809427574746F6E0008FFEEFF0401110700C0");
            Trace.WriteLine(ddDecoded.Serialize());
            Assert.AreEqual("False", ddDecoded.ManufacturerSpecific.SensorData["PIR"]);

            var gpio = BtleDecoder.Decode("043e28020100012066000a9fca1c020106030300a004094f54000fffeeff04011107000f000000000000b4");
            Trace.WriteLine(gpio.Serialize());
            Assert.AreEqual(gpio.ManufacturerSpecific.SensorData["DigitalInputs"], "0,0,0,0,0,0");

            var gpioEnabled = BtleDecoder.Decode("043e28020100012066000a9fca1c020106030300a004094f54000fffeeff04011107010f010000000000bf");
            Trace.WriteLine(gpioEnabled.Serialize());
        }

        [Test]
        public void BinaryReader()
        {
            using (var ms = new MemoryStream("64010003".ToBytes()))
            using (var br = new BinaryReader(ms))
            {
                var fullBattery = br.ReadInt8();
                var isTrue = br.ReadBoolean();
                var isFalse = br.ReadBoolean();
                var isNotTrue = br.ReadInt8() == 0x01;
                Trace.WriteLine($"batt: {fullBattery}, true: {isTrue}, false: {isFalse}, notTrue: {isNotTrue}");
                Assert.AreEqual(100,fullBattery);
                Assert.IsTrue(isTrue);
                Assert.IsFalse(isFalse);
                Assert.IsFalse(isNotTrue);
            }
                
        }
    }
}
