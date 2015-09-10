using DisneyCMS.cms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.test
{
    [TestClass]
    public class CMSTester
    {
        [TestMethod]
        public void TestCmsInit()
        {
            CMS cms = new CMS( null );
            Assert.IsTrue(cms.init());
            cms.StopService();
        }
    }
}
