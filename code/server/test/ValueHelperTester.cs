using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DisneyCMS.test
{
    [TestClass]
    public class ValueHelperTester
    {
        [TestMethod]
        public void TestToByte()
        {
            byte[] bs = new byte[8] { 0, 0, 0, 0, 0, 0, 1, 0 };
            byte b = ValueHelper.ToByte(bs, 0);
            Assert.AreEqual(2, b);

            for (int i = 0; i < 8; i++)
                bs[i] = 1;
            b = ValueHelper.ToByte(bs, 0);
            Assert.AreEqual(0xFF, b);

            for (int i = 0; i < 8; i++)
                bs[i] = 0;
            b = ValueHelper.ToByte(bs, 0);
            Assert.AreEqual(0, b);

            byte[] bs1 = new byte[16];
            for (int i = 8; i < 16; i++)
                bs1[i] = 1;
            b = ValueHelper.ToByte(bs1, 8);
            Assert.AreEqual(0xFF, b);
        }

        [TestMethod]
        public void TestSum()
        {
            byte[] bs = new byte[8] { 0, 0, 0, 0, 0, 0, 1, 1 };
            byte b = ValueHelper.Sum(bs, 0,8);
            Assert.AreEqual(2, b);

            bs[0] = 12;
            b = ValueHelper.Sum(bs, 0, 8);
            Assert.AreEqual(14, b);

            bs[0] = 0xFF;
            b = ValueHelper.Sum(bs, 0, 8);
            Assert.AreEqual(0x01, b);
        }

        [TestMethod]
        public void TestGetIpLastAddr()
        {
            Assert.AreEqual("18", ValueHelper.GetIpLastAddr("192.168.0.18"));
            Assert.AreEqual("222", ValueHelper.GetIpLastAddr("222"));
            Assert.AreEqual("33", ValueHelper.GetIpLastAddr(".33"));
        }
    }
}
