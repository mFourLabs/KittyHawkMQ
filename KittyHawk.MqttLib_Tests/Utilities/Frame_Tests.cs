using System;
using KittyHawk.MqttLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Utilities
{
    [TestClass]
    public class Frame_Tests
    {
        [TestMethod]
        public void GetBitPositionWorksWithNormalInput()
        {
            Assert.AreEqual(0, Frame.GetBitPosition(0x01));
            Assert.AreEqual(1, Frame.GetBitPosition(0x02));
            Assert.AreEqual(2, Frame.GetBitPosition(0x04));
            Assert.AreEqual(3, Frame.GetBitPosition(0x08));
            Assert.AreEqual(4, Frame.GetBitPosition(0x10));
            Assert.AreEqual(5, Frame.GetBitPosition(0x20));
            Assert.AreEqual(6, Frame.GetBitPosition(0x40));
            Assert.AreEqual(7, Frame.GetBitPosition(0x80));
        }

        [TestMethod]
        public void CanEncodeDecodeString()
        {
            int pos = 0;
            var buf = new byte[10];
            string str = "Dr. Who";
            Frame.EncodeString(str, buf, ref pos);
            Assert.AreEqual(9, pos);

            pos = 0;
            string test = Frame.DecodeString(buf, ref pos);
            Assert.AreEqual("Dr. Who", test);
            Assert.AreEqual(9, pos);
        }

        [TestMethod]
        public void CanEncodeDecodeInt16()
        {
            int pos = 0;
            var buf = new byte[10];
            Frame.EncodeInt16(42, buf, ref pos);
            Assert.AreEqual(2, pos);

            pos = 0;
            int test = Frame.DecodeInt16(buf, ref pos);
            Assert.AreEqual(42, test);
            Assert.AreEqual(2, pos);
        }
    }
}
