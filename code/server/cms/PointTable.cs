using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    public class PointTable
    {
        public const byte INVALID_COIL_VAL = 0xFF;
        public int Size { get; private set; }
        private byte[] _values;
        public PointTable(int size)
        {
            Size = size;
            _values = new byte[Size * 16];
        }

        public void Fill(bool on)
        {
            byte v = (byte)(on ? 1 : 0);
            for (var i = 0; i < _values.Length; i++)
            {
                _values[i] = v;
            }
        }

        // 更新点表.
        public void SetValueAt(int reg, int bit, bool on)
        {
            if (reg > Size - 1) return;
            if (bit > 15) return;
            _values[reg*16 + bit] = (byte)(on ? 1 : 0);
        }

        // 取值.
        public byte GetValueAt(int reg, int bit)
        {
            if (reg > Size - 1) 
                return INVALID_COIL_VAL;
            if (bit > 15) 
                return INVALID_COIL_VAL;
            return _values[reg*16 + bit];
        }


        public byte[] GetValues(int reg, int bit, int count)
        {
            // Read Coils. (least significant bit is first coil!) (靠前的 Coil在低位)
            // 满8个为一个字节.
            int offset = reg * 16 + bit;
            if (offset + count >= Size * 16) { count = Size * 16 - offset; }
            int byteCnt = (count +7) /8;
            byte[] bs = new byte[byteCnt];
            int bi=0, len;
            for(int i=0; i < byteCnt; i++) {
                if (i == byteCnt -1) {
                    len = count - i*8;
                }else 
                    len = 8;
                bs[bi++] = ToByte(_values, offset, len);
                offset+=8;
            }
            return bs;
        }

        public static byte ToByte(byte[] vs, int offset, int cnt)
        {
            byte v = 0;
            int vcnt = Math.Min(8, cnt);
            for (int i = 0; i < vcnt; i++)
            {
                if (vs[i + offset ] == 1)
                    v |= (byte)(0x01 << i);
            }
            return v;
        }
    }
}
