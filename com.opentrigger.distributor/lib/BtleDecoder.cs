using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Authentication.ExtendedProtection.Configuration;
using System.Text;

namespace com.opentrigger.distributord
{

    public class BtleDecoded
    {
        public string Mac { get; set; }
        public sbyte Rssi { get; set; }
        public byte[] RawData { get; set; }
        public byte[] AdvertisingData { get; set; }
        public BtleAdvertManufacturerSpecific ManufacturerSpecific { get; set; }

        public int? GetEventId()
        {
            if (ManufacturerSpecific != null && ManufacturerSpecific.SensorData != null)
            {
                if (ManufacturerSpecific.SensorData.ContainsKey("EventId"))
                {
                    int eventId;
                    if (int.TryParse(ManufacturerSpecific.SensorData["EventId"], out eventId)) return eventId;
                }
            }
            return null;
        }
    }

    public class BtleAdvertManufacturerSpecific
    {
        public byte[] CompanyId { get; set; }
        public byte[] Data { get; set; }
        public Dictionary<string, string> SensorData { get; set; }

        public void AddSensortData(string sensor, string value)
        {
            if (SensorData == null) SensorData = new Dictionary<string, string>();
            SensorData.Add(sensor, value);
        }
    }

    public static class BtleDecoder
    {
        public static BtleDecoded Decode(string hexString) => Decode(hexString.ToBytes());
        public static BtleDecoded Decode(byte[] data)
        {
            var result = new BtleDecoded
            {
                Mac = data.Skip(7).Take(6).Reverse().ToArray().ToHexString(":"),
                RawData = data,
            };

            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                ms.Seek(-1, SeekOrigin.End);
                result.Rssi = br.ReadSByte();
                ms.Seek(13, SeekOrigin.Begin);
                var advertSize = (int)br.ReadByte();
                if (advertSize > 0)
                {
                    result.AdvertisingData = br.ReadBytes(advertSize);
                    AdvertDecoder(result);
                }
            }

            if (result.ManufacturerSpecific != null) ParseTokenCube(result);

            return result;
        }


        private static void ParseTokenCube(BtleDecoded result)
        {
            // http://tokencube.com/bluetooth-sensor.html
            if (
                result.ManufacturerSpecific != null &&
                result.ManufacturerSpecific.CompanyId.Length == 2 &&
                result.ManufacturerSpecific.CompanyId[0] == 0xEE && result.ManufacturerSpecific.CompanyId[1] == 0xFF && /* tokencube companyId */
                result.ManufacturerSpecific.Data[0] == 0x04 && /* Hardware identifyer == Token Module v4 */
                result.ManufacturerSpecific.Data[1] == 0x01 /* Firmware/Format Version */
                )
            {
                //byte alarmByte = 0x80;
                using (var ms = new MemoryStream(result.ManufacturerSpecific.Data))
                using (var br = new BinaryReader(ms))
                {
                    ms.Seek(3, SeekOrigin.Begin); /* go to first sensor identifier, byte nr 5 in reference */
                    while (!br.EndOfStream())
                    {
                        var sensorType = br.ReadByte();
                        //var alarm = (sensorType & alarmByte) == alarmByte;
                        if (sensorType == 0x01 || sensorType == 0x81) // 0x01	0x81	Temperature	°C	int16, MSB first. Example: 5123 = 51.23 DegC.
                        {
                            var temperature = br.ReadInt16MsbFirst() / 100M;
                            result.ManufacturerSpecific.AddSensortData("Temperature", temperature.ToString(CultureInfo.InvariantCulture));
                            continue;
                        }
                        if (sensorType == 0x04 || sensorType == 0x84) // 0x04	0x84	Humidity	%RH	int16, MSB first. Example: 4231 = 41.321 %rH
                        {
                            var humidity = br.ReadInt16MsbFirst() / 100M;
                            result.ManufacturerSpecific.AddSensortData("Humidity", humidity.ToString(CultureInfo.InvariantCulture));
                            continue;
                        }
                        if (sensorType == 0x05 || sensorType == 0x85) // 0x05	0x85	Pressure	mBar	int24, MSB first. Example: 96386 Pa = 963.86 hPa = 963.86 millibar
                        {
                            var pressure = br.ReadInt24MsbFirst() / 100M;
                            result.ManufacturerSpecific.AddSensortData("Pressure", pressure.ToString(CultureInfo.InvariantCulture));
                            continue;
                        }
                        if (sensorType == 0x06 || sensorType == 0x86) // 0x06	0x86	Orientation	g	3 x int16, MSB first X, Y, Z. Example: 4096 = 1g
                        {
                            var orientation = new string[]
                            {
                                br.ReadInt8().ToString(), // X
                                br.ReadInt8().ToString(), // Y
                                br.ReadInt8().ToString(), // Z
                            };
                            result.ManufacturerSpecific.AddSensortData("Orientation", string.Join(",",orientation));
                            continue;
                        }
                        if (sensorType == 0x07 || sensorType == 0x87) // 0x07	0x87	PIR	true / false	int8. Example: 0x01 = PIR Detected Motion
                        {
                            var pirMotion = br.ReadTokenCubeBool();
                            result.ManufacturerSpecific.AddSensortData("PIR", pirMotion.ToString());
                            continue;
                        }
                        if (sensorType == 0x08 || sensorType == 0x88) // 0x08	0x88	Motion	true / false	int8. Example: 0x01 = Sensor is in Motion
                        {
                            var motion = br.ReadTokenCubeBool();
                            result.ManufacturerSpecific.SensorData.Add("Motion", motion.ToString());
                            continue;
                        }
                        if (sensorType == 0x09 || sensorType == 0x89) // 0x09	0x89	Mechanical Shock	x/y/z true/false	3 x int8. X, Y, Z. Example: 0x01,0x00,0x00 = Mechanical Shock oriented X
                        {
                            var mechanicalShock = new string[]
                            {
                                br.ReadTokenCubeBool().ToString(), // X
                                br.ReadTokenCubeBool().ToString(), // Y
                                br.ReadTokenCubeBool().ToString(), // Z
                            };
                            result.ManufacturerSpecific.AddSensortData("MechanicalShock",string.Join(",",mechanicalShock));
                            continue;
                        }
                        if (sensorType == 0x0A || sensorType == 0x8A) // 0x0A	0x8A	Battery	%charged	uint8. ex: 0x64=100 ->100% of battery
                        {
                            var batteryCharge = br.ReadInt8();
                            result.ManufacturerSpecific.AddSensortData("BatteryCharge",batteryCharge.ToString());
                            continue;
                        }
                        if (sensorType == 0x0F) // 0x0F 6x unint8
                        {
                            result.ManufacturerSpecific.AddSensortData("EventId", br.ReadUint8().ToString());
                            var digitalInputs = new int[6];

                            for (var i = 0; i < digitalInputs.Length; i++)
                            {
                                digitalInputs[i] = br.ReadInt8();
                                result.ManufacturerSpecific.AddSensortData($"DigitalInput{i}", digitalInputs[i].ToString());
                            }
                            result.ManufacturerSpecific.AddSensortData("DigitalInputs", string.Join(",", digitalInputs.Select(v => v.ToString())));
                            continue;
                        }

                        var bytesLeft = br.BaseStream.Length - br.BaseStream.Position;
                        if (bytesLeft <= 0) continue;
                        var unknownData = new byte[bytesLeft];
                        br.Read(unknownData, 0, unknownData.Length);
                        result.ManufacturerSpecific.AddSensortData("UnknownData", unknownData.ToHexString());
                    }
                }
            }
        }

        public static void AdvertDecoder(BtleDecoded result)
        {
            using (var ms = new MemoryStream(result.AdvertisingData))
            using (var br = new BinaryReader(ms))
            {
                while (!br.EndOfStream())
                {
                    var sectionLength = (int) br.ReadByte();
                    if (sectionLength > 0)
                    {
                        var sectionType = br.ReadByte(); sectionLength--;

                        if (sectionLength > 0)
                        {
                            switch (sectionType)
                            {
                                case 0xFF: // Manufacturer Specific
                                    result.ManufacturerSpecific = new BtleAdvertManufacturerSpecific
                                    {
                                        CompanyId = br.ReadBytes(2)
                                    };
                                    var manufacturerDataLength = sectionLength - 2;
                                    if (manufacturerDataLength > 0)
                                    {
                                        result.ManufacturerSpecific.Data = br.ReadBytes(manufacturerDataLength);
                                    }
                                    break;

                                default:
                                    br.ReadBytes(sectionLength);
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
