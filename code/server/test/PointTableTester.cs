using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DisneyCMS.cms;

namespace DisneyCMS.test
{
    [TestClass]
    public class PointTableTester
    {

        [TestMethod]
        public void TestPointTable()
        {
            int size = 52;
            PointTable table = new PointTable(size);
            table.SetValueAt(1, 0, true);
            Assert.AreEqual(1, table.GetValueAt(1, 0));
            Assert.AreEqual(PointTable.INVALID_COIL_VAL, table.GetValueAt(size, 0));
            Assert.AreEqual(0, table.GetValueAt(CMS.REG_CNT -1, 0));

            // Set All
            table.Fill(true);
            for (int i = 0; i < size; i++)
                for (int j = 0; j < 16; j++)
                    Assert.AreEqual(1, table.GetValueAt(i,j));

            // Reset All
            table.Fill(false);
            for (int i = 0; i < 52; i++)
                for (int j = 0; j < 8; j++)
                    Assert.AreEqual(0, table.GetValueAt(i, j));
        }

        [TestMethod]
        public void TestGetValues() {
            int size = 52;
            PointTable table = new PointTable(size);
            byte[] bs = table.GetValues(0, 0, 3);
            Assert.AreEqual(1, bs.Length);
            Assert.AreEqual(0, bs[0]);

            table.SetValueAt(0, 1, true);
            bs = table.GetValues(0, 0, 3);
            Assert.AreEqual(2, bs[0]); // 0b...010" = 2;

            table.SetValueAt(0, 2, true);
            bs = table.GetValues(0, 0, 3); // 110=6;
            Assert.AreEqual(6, bs[0]);

            bs = table.GetValues(0, 0, 8); // 00000110=6;
            Assert.AreEqual(6, bs[0]);

            table.SetValueAt(0, 8, true);
            bs = table.GetValues(0, 0, 9); // 00000001,00000110=6;
            Assert.AreEqual(6, bs[0]);
            Assert.AreEqual(1, bs[1]);
        }

        [TestMethod]
        public void TestPTToByte()
        {
            PointTable table = new PointTable(52);
            byte[] bs = new byte[8] { 0, 1, 0, 0, 0, 0, 0, 0 };
            byte b = PointTable.ToByte(bs, 0, 2);
            Assert.AreEqual(2, b);

            for (int i = 0; i < 8; i++)
                bs[i] = 1;
            b = PointTable.ToByte(bs, 0, 8);
            Assert.AreEqual(0xFF, b);

            for (int i = 0; i < 8; i++)
                bs[i] = 0;
            b = PointTable.ToByte(bs, 0, 8);
            Assert.AreEqual(0, b);

            byte[] bs1 = new byte[16];
            for (int i = 8; i < 16; i++)
                bs1[i] = 1;
            b = PointTable.ToByte(bs1, 8, 8);
            Assert.AreEqual(0xFF, b);

            Assert.AreEqual(0x01, PointTable.ToByte(bs1, 8, 1));
            Assert.AreEqual(0x03, PointTable.ToByte(bs1, 8, 2));
            Assert.AreEqual(0x07, PointTable.ToByte(bs1, 8, 3));
            Assert.AreEqual(0x0F, PointTable.ToByte(bs1, 8, 4));
            Assert.AreEqual(0x1F, PointTable.ToByte(bs1, 8, 5));
            Assert.AreEqual(0x3F, PointTable.ToByte(bs1, 8, 6));
            Assert.AreEqual(0x7F, PointTable.ToByte(bs1, 8, 7));
        }
    }
}
