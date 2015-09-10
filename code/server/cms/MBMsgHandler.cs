using DisneyCMS.modbus;

namespace DisneyCMS.cms
{
    public class MBMsgHandler
    {
        private CMS _cms;

        public MBMsgHandler(CMS _cms)
        {
            this._cms = _cms;
        }

        public MBMessage DealRequest(MBConnect c, MBMessage req)
        {
            MBMessage resp;
            if (req.FC == 0x01)
            {
                resp = AckReadCoil(req);
            }
            else if (
               req.FC == 0x05)
            {
                resp = AckWriteCoil(req);
            }
            else
            {
                resp = new MBMessage(req);
                resp.FC = (byte)(0x80 | req.FC);
                resp.SetBody(new byte[] { MBException.E01_ILLEGAL_FUNCTION });
            }
            return resp;
        }

        private MBMessage AckReadCoil(MBMessage req)
        {
            MBMessage resp = new MBMessage(req);
            /** 
             * req:
             * Byte 1-2:Reference number
             * Byte 3-4:Bit count (1-2000)
             * resp:
             * Response
             * Byte 1: Byte count of response (B=(bit count+7)/8)
             * Byte 2-(B+1):Bit values (least significant bit is first coil!)"
            */
            ushort refNum = req.GetWord(0);
            ushort bitCount = req.GetWord(2);

            if (_cms.IsValidRegAddress((byte)refNum))
            {
                byte[] byteValues = _cms.MB_ReadCoils(refNum, bitCount); 
                // 写入.
                var bcnt = byteValues.Length;
                resp.SetBodySize((ushort)(bcnt + 1));
                resp.SetByte(0, (byte)bcnt);
                for (byte i = 0; i < bcnt; i++)
                {
                    resp.SetByte(i + 1, byteValues[i]);
                }
            }
            else
            {
                // Error Response.
                // Byte 0:FC = 81 (hex) 
                // Byte 1:exception code = 01 or 02
                resp.FC = 0x82;
                resp.SetBody(new byte[] { MBException.E02_ILLEGAL_DATA_ADDRESS });
            }
            return resp;
        }

        /* WriteCoil
        REQ:
        Byte 0:FC = 05 
        Byte 1-2: Reference number 
        Byte 3:= FF to turn coil ON, =00 to turn coil OFF
        Byte 4:= 00	
        Resp:
        Byte 1-2:Reference number 
        Byte 3:= FF to turn coil ON, =00 to turn coil OFF (echoed)
        Byte 4:= 00"
        */
        private MBMessage AckWriteCoil(MBMessage req)
        {
            MBMessage resp = new MBMessage(req); // copy header
            ushort refNum = req.GetWord(0); // 
            byte val = req.GetByte(2);
            byte result = _cms.MB_WriteCoil(refNum, val); // exception
            if (MBException.MB_SUCCESS == result)
            {
                // OK, return resp 
            }
            else
            {
                // ERROR
                resp.FC = 0x85;
                resp.SetBody(new byte[] { result });
            }
            return resp;
        }
    }
}
