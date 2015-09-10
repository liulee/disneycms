using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.modbus
{
    public class MBMessage
    {
        public const byte MIN_LENGTH = 8;
        private const byte HEAD_LENGTH = MIN_LENGTH;

        // head, len=8
        public ushort TID { get; set; }      // 2:byte, Transaction identifier 
        public ushort PID { get; set; }      // 2:Protocol Identifier (00 00)
        public ushort Length { get; set; }   // 2:Number of following bytes;
        public byte UID { get; set; }        // 1:Unit Identifier(slave address)
        public byte FC { get; set; }         // 1:功能码
        // body: 0-125
        private byte[] _body;              // 数据区, FC以后内容. 长度 = Length - 2
        public MBMessage()
        {
        }

        // parse.
        public MBMessage(byte[] frame)
        {
            if (frame.Length < MIN_LENGTH)
            {
                throw new Exception("buffer too short(<8)");
            }
            else
            {
                TID = ValueHelper.GetUshort_BE(frame, 0);
                PID = ValueHelper.GetUshort_BE(frame, 2);
                Length = ValueHelper.GetUshort_BE(frame, 4);
                int recvLen = frame.Length - 6; // PDU长度.
                if (recvLen < Length)
                {
                    // 长度不匹配. 
                    throw new Exception(string.Format("无效消息头, 长度不匹配({0} > {1} )", Length, recvLen));
                }
                if (Length < 2)
                {
                    throw new Exception("无效消息头, 长度 <2 ");
                }
                UID = frame[6];
                FC = frame[7];
                if (Length > 2)
                {
                    _body = new byte[Length - 2];  // UID, FC
                    Array.Copy(frame, HEAD_LENGTH, _body, 0, Length - 2);
                }
                else
                {
                    _body = new byte[0];
                }
            }
        }

        public MBMessage(MBMessage request)
        {
            //this.result = result;
            this.TID = request.TID;
            this.PID = request.PID;
            this.UID = request.UID;
            this.Length = request.Length;
            this.FC = request.FC;
            this._body = new byte[request._body.Length];
            Array.Copy(request._body, this._body, request._body.Length);
        }

        public byte[] encode()
        {
            byte[] buff = new byte[HEAD_LENGTH + _body.Length];
            ValueHelper.WriteShort_BE(buff, 0, TID);
            ValueHelper.WriteShort_BE(buff, 2, PID);
            ValueHelper.WriteShort_BE(buff, 4, Length);
            buff[6] = UID;
            buff[7] = FC;
            if (_body.Length > 0)
                Array.Copy(_body, 0, buff, HEAD_LENGTH, _body.Length);
            return buff;
        }

        public void SetBodySize(ushort len)
        {
            this._body = new byte[len];
            this.Length = (ushort)(HEAD_LENGTH + len - 6);
        }

        public void SetBody(byte[] data)
        {
            this._body = data;
            this.Length = (ushort)(HEAD_LENGTH + data.Length - 6);
        }

        // Set a word value
        public void SetWord(int idx, ushort val)
        {
            if (idx <= _body.Length - 1)
                ValueHelper.WriteUShort_BE(_body, idx, val);
            else
                throw new Exception(string.Format("Index {0} out out range.", idx));
        }
        public void SetByte(int idx, byte v)
        {
            if (idx <= _body.Length - 1)
                _body[idx] = v;
            else
                throw new Exception(string.Format("Index {0} out out range.", idx));
        }
        // Get a word value.
        public ushort GetWord(int idx)
        {
            if (idx <= _body.Length - 1)
                return ValueHelper.GetUShort_BE(_body, idx);
            else
                throw new Exception(string.Format("Index {0} out out range.", idx));
        }

        public byte GetByte(int idx)
        {
            if (idx <= _body.Length - 1)
                return _body[idx];
            else
                throw new Exception(string.Format("Index {0} out out range.", idx));
        }
    }
}
