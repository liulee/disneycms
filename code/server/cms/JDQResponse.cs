using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace DisneyCMS.cms
{
    public class JDQResponse
    {
        public SocketError Error { get; set; }
        public string ExtError { get; set; }
        private byte[] _buff;
        public byte[] Buff { get { return _buff; } }
        JDQRequestType _type;

        public JDQResponse(JDQRequestType reqType, byte[] recvBytes)
        {
            this._buff = new byte[recvBytes.Length];
            Array.Copy(recvBytes, _buff, recvBytes.Length);
            this._type = reqType;
            Error = SocketError.Success;
            ExtError = "";
        }

        public bool IsOK
        {
            get
            {
                if (_type == JDQRequestType.SetOutput)
                    return IsSetOutputOK();
                else if (_type == JDQRequestType.ReadOutput)
                    return IsValidReadOutputResp();
                else
                    return IsValidReadInputResp();
            }
        }

        private bool IsValidReadOutputResp()
        {
            return 
                SocketError.Success == Error &&
                _buff.Length >= 8 && // 9: 协议错误, 实际返回是8个字节.
                _buff[0] == 0xAA &&
                _buff[1] == 0xBB &&
                _buff[2] == 0xB0 &&
                _buff[6] == 0x0D &&
                IsCrc1OK(); // 只检查crc1, crc2长度不够
        }

        private bool IsValidReadInputResp()
        {
            return
                SocketError.Success == Error &&
                _buff.Length >= 9 &&
                _buff[0] == 0xEE &&
                _buff[1] == 0xFF &&
                _buff[2] == 0xC0 &&
               // _buff[6] == 0x0D && // 无法检查, 实际返回为0
                IsCrcOK();
        }

        // Check CRC1, CRC2
        private bool IsCrcOK()
        {
            byte crc1 = ValueHelper.Sum(_buff, 2, 5);
            byte crc2 = (byte)(ValueHelper.Sum(_buff, 2, 5) + _buff[7]);
            return _buff[7] == crc1 && _buff[8] == crc2;
        }

        // Check CRC1, CRC2
        private bool IsCrc1OK()
        {
            byte crc1 = ValueHelper.Sum(_buff, 2, 5);
            return _buff[7] == crc1 ;
        }

        private bool IsSetOutputOK()
        {
            if (_type == JDQRequestType.SetOutput)
            {
                string ack = Encoding.ASCII.GetString(_buff);
                return ack == "OK!";
            }
            else
            {
                return false;
            }
        }

        ///  取得 index (0-15) 位 的继电器状态 
        public RelayState GetRelayState(byte index)
        {
            if (_type == JDQRequestType.ReadOutput)
            {
                return GetBitAt(index) == 1 ? RelayState.ACTION : RelayState.RESET;
            }
            else
            {
                return RelayState.UNKNOWN;
            }
        }

        // 取得 index (0-15) 位的输入状态
        public OnOff GetInputState(byte index)
        {
            if (_type == JDQRequestType.ReadInput)
            {
                return GetBitAt(index) == 1 ? OnOff.ON : OnOff.OFF;
            }
            else
            {
                return OnOff.UNKNOWN;
            }
        }

        private byte GetBitAt(int index)
        {
            byte bh = _buff[4]; // 15-8
            byte bl = _buff[5]; // 7-0;
            byte lshift = 0;
            byte bit = 0;
            if (index >= 8)
            {
                lshift = (byte)(15 - index);
                bit = (byte)(((byte)(bh << lshift)) >> 7);
            }
            else
            {
                lshift = (byte)(7 - index);
                bit = (byte)(((byte)(bl << lshift)) >> 7);
            }
            return bit;
        }


        internal int GetLength()
        {
            return _buff != null ? _buff.Length : 0;
        }
    }
}
