using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DisneyCMS.modbus;
namespace DisneyCMS.test
{
    [TestClass]
    public class MBMessageTester
    {
        [TestMethod]
        public void TestMBMessage()
        {
            byte[] bs = ValueHelper.StrToToHexByte("00 86 00 00 00 06 01 01 01 01 00 02");
            MBMessage mb = new MBMessage(bs);
            Assert.AreEqual(0x86, mb.TID);
            Assert.AreEqual(0, mb.PID);
            Assert.AreEqual(6, mb.Length);
            Assert.AreEqual(1, mb.UID);
            Assert.AreEqual(1, mb.FC);
            Assert.AreEqual(0x101, mb.GetWord(0));
            Assert.AreEqual(0x2, mb.GetByte(3));
            Assert.AreEqual(0x2, mb.GetWord(2));
        }
    }
}
