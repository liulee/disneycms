using log4net;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DisneyCMS.cms
{
    public class JDQVisitor
    {
        private static ILog log = LogManager.GetLogger("JDQV");
        private ConcurrentDictionary<string, SocketClient> _connections = new ConcurrentDictionary<string, SocketClient>();

        /// <summary>
        ///  发送请求
        /// </summary>
        /// <param name="ip">目标设备</param>
        /// <param name="port">端口</param>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public JDQResponse Request(string ip, JDQRequest req, ushort port = 50000, uint timeout = 2000)
        {
            lock (this)
            {
                SocketClient conn = TryConnect(ip, port);
                JDQResponse resp;
                if (conn != null && conn.IsConnected)
                {
                    SocketError error;
                    byte[] recv = conn.SSend(req.Encode(), out error);
                    resp = new JDQResponse(req.Type,recv);
                    resp.Error = error;
                    resp.ExtError = error.ToString();
                }
                else
                {
                    // ERROR response
                    log.ErrorFormat("Connection null or Not Ready.");
                    resp = new JDQResponse(req.Type, new byte[0]);
                    resp.Error = SocketError.NotConnected;
                    resp.ExtError = "连接未就绪";
                }
                return resp;
            }
        }

        public void Terminate()
        {
            lock (this)
            {
                foreach (var s in _connections.Values)
                {
                    CloseConnect(s);
                }
                _connections.Clear();
            }
        }

        public void CloseConnect(SocketClient c, bool remove=false)
        {
            if (c == null)
            {
                return;
            }
            log.InfoFormat("Device {0} closed.", c.IpAddr );
            c.Close();
            if (remove)
            {
                SocketClient removed;
                _connections.TryRemove(c.IpAddr, out removed);
            }
        }

        public SocketClient TryConnect(string ip, ushort port, uint timeout = 2000)
        {
            SocketClient c;
            if (_connections.ContainsKey(ip))
            {
                c = _connections[ip];
            }
            else
            {
                c = new SocketClient(ip, port);
                _connections[ip] = c;
            }
            if (!c.IsConnected)
            {
                c.Connect(timeout);
            }
            return c;
        }
    }
}
