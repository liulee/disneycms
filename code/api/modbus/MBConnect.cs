using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace DisneyCMS.modbus
{
    public class MBConnect
    {
        public string IP { get; set; }
        public Socket _socket;
        public Socket Socket
        {
            get
            {
                return _socket;
            }
        }
        public DateTime Connected { get; private set; }
        public int Req { get; set; }
        public int Recv { get;  set;} // bytes
        public int Ack { get; private set; }
        public int Sent { get; private set; } //bytes

        public MBConnect(Socket s,string remoteIp)
        {
            this._socket = s;
            Req = 0;
            Recv = 0;
            Ack = 0;
            Sent = 0;
            Connected = DateTime.Now;
            IP = remoteIp;
        }

        public int Elapsed
        {
            get
            {
                return (int)(DateTime.Now - Connected).TotalSeconds;
            }
        }

        // 异步发送应答.
        public void ASend(MBMessage resp)
        {
            if (resp != null)
            {
                byte[] buff = resp.encode();
                if (this._socket != null)
                {
                    Ack++;
                    Sent += buff.Length;
                    this._socket.Send(buff);
                }
            }
        }
    }
}
