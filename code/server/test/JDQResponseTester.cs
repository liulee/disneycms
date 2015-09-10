using DisneyCMS.cms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DisneyCMS.test
{
    [TestClass]
    public class JDQResponseTester
    {

        [TestMethod]
        public void TestIsReadInputOK()
        {
            byte[] resp = new byte[] { 0xee, 0xff, 0xc0, 0x01, 0x00, 0x00, 0x00, 0xc1, 0x82 };
            JDQResponse r = new JDQResponse(JDQRequestType.ReadInput, resp);
            Assert.IsTrue(r.IsOK);
        }

        [TestMethod]
        public void TestIsReadOutputOK()
        {
             byte[] resp = new byte[] { 0xaa, 0xbb, 0xb0, 0x01, 0x00, 0x11, 0x0d, 0xcf };
            JDQResponse r = new JDQResponse(JDQRequestType.ReadOutput, resp);
            Assert.IsTrue(r.IsOK);

        }
    }
}
