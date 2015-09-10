using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    public class JDQRequest
    {
        public const byte DEV_ID = 1;
        public static readonly byte ALL = 0xFF;
        public byte DevId { get; set; }
        private byte[] _output = new byte[16];
        private byte[] _switch = new byte[16];
        public JDQRequestType Type { get { return _type; } }
        private JDQRequestType _type = JDQRequestType.SetOutput;

        public JDQRequest(byte devId, JDQRequestType type)
        {
            this.DevId = devId;
            this._type = type;
        }

        public void SetOutput(byte idx, bool on)
        {
            byte switcher = (byte) (on ? 1 : 0);
            _output[15 - idx] = switcher;
            _switch[15 - idx] = 1;
        }

        public void SetOutputs(byte[] idx, bool on)
        {
            foreach (byte i in idx)
            {
                SetOutput(i, on);
            }
        }

        // 设置输出通道 idx为 使能. 动作某继电器. idx: 0~F
        public void TurnOnOutput(byte idx)
        {
            if (idx == ALL)
            {
                // tune off all
                for (byte i = 0; i < 16; i++)
                {
                    SetOutput(i, true);
                }
            }
            else if (idx <= 0xF)
            {
                SetOutput(idx, true);
            }
        }

        // 设置输出通道 idx 为 复位(0) , idx: 0~F
        public void TurnOffOutput(byte idx)
        {
            if (idx == ALL)
            {
                // tune off all
                for (byte i = 0; i < 16; i++)
                {
                    SetOutput(i, false);
                }
            }
            else if (idx <= 0xF)
            {
                SetOutput(idx, false);
            }
        }

        public byte[] Encode()
        {
            switch (_type)
            {
                case JDQRequestType.SetOutput:
                    return EncodeWriteOutput();
                case JDQRequestType.ReadInput:
                    return EncodeReadInput();
                case JDQRequestType.ReadOutput:
                    return EncodeReadOutput();
            }
            return null;
        }

        // 读输出
        public byte[] EncodeReadOutput()
        {
           // CC	DD	B0=读取	固定为1	固化0	固化0	固化 0D	CRC1, CRC2
            byte[] buff = new byte[9];
            buff[0] = 0xCC;
            buff[1] = 0xDD;
            buff[2] = 0xB0;
            buff[3] = DevId; // Device Addr;
            buff[6] = 0x0D;
            buff[7] = ValueHelper.Sum(buff, 2, 5);
            buff[8] = (byte)(ValueHelper.Sum(buff, 2, 5) + buff[7]);
            return buff;
        }


        // 读输入
        private byte[] EncodeReadInput()
        {
            // CC	DD	B0=读取	固定为1	固化0	固化0	固化 0D	CRC1, CRC2
            byte[] buff = new byte[9];
            buff[0] = 0xCC;
            buff[1] = 0xDD;
            buff[2] = 0xC0;
            buff[3] = DevId; // Device Addr;
            buff[6] = 0x0D;
            buff[7] = ValueHelper.Sum(buff, 2, 5);
            buff[8] = (byte)(ValueHelper.Sum(buff, 2, 5) + buff[7]);
            return buff;
        }

        // 写输出
        private byte[] EncodeWriteOutput()
        {
            byte[] buff=new byte[10];
            buff[0] = 0xCC;
            buff[1] = 0xDD;
            buff[2] = 0xA1;
            buff[3] = DevId; // Device Addr;
            buff[4] = ValueHelper.ToByte(_output, 0);
            buff[5] = ValueHelper.ToByte(_output, 8);
            buff[6] = ValueHelper.ToByte(_switch, 0);
            buff[7] = ValueHelper.ToByte(_switch, 8);
            buff[8] = ValueHelper.Sum(buff, 2, 6);
            buff[9] = (byte)(ValueHelper.Sum(buff, 2, 6) + buff[8]);
            return buff;
        }
    }
}
