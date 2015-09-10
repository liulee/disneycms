using DisneyCMS.cms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.test
{
    [TestClass]
    public class JDQRequestTester
    {
        [TestMethod]
        public void TestEncodeWriteOutput()
        {
            JDQRequest req = new JDQRequest(1,  JDQRequestType.SetOutput);
            req.TurnOnOutput(4);
            req.TurnOnOutput(5);
            for (byte i = 0; i < 16; i++)
            {
                req.TurnOnOutput(i);
            }
            byte[] buff = req.Encode();
            Console.WriteLine("buff is: {0}", ValueHelper.BytesToHexStr(buff));
            Assert.AreEqual(10, buff.Length);
            Assert.AreEqual(0xCC, buff[0]);
            Assert.AreEqual(0x9E, buff[8]);
            Assert.AreEqual(0x3C, buff[9]);
        }


        [TestMethod]
        public void TestEncodeReadOutput()
        {
            JDQRequest req = new JDQRequest(1, JDQRequestType.ReadOutput);

            byte[] buff = req.Encode();
            Console.WriteLine("buff is: {0}", ValueHelper.BytesToHexStr(buff));
            Assert.AreEqual(9, buff.Length);
            Assert.AreEqual(0xCC, buff[0]);
            Assert.AreEqual(0xBE, buff[7]);
            Assert.AreEqual(0x7C, buff[8]);
        }

        [TestMethod]
        public void TestEncodeReadInput()
        {
            JDQRequest req = new JDQRequest(1, JDQRequestType.ReadInput);
            byte[] buff = req.Encode();
            Console.WriteLine("buff is: {0}", ValueHelper.BytesToHexStr(buff));
            Assert.AreEqual(9, buff.Length);
            Assert.AreEqual(0xC0, buff[2]);
            Assert.AreEqual(0xCE, buff[7]);
            Assert.AreEqual(0x9C, buff[8]);
        }
    }
}
