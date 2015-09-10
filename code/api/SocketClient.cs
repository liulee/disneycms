using log4net;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DisneyCMS
{
    public class SocketClient
    {
        private static readonly int BUFF_SIZE = 160; // max: 125 + 
        private ILog log;
        private ManualResetEvent _connTimeoutEvent, _sendTimeoutEvent;
        private Socket _socket;
        private bool _connected = false;
        private string _ip;
        private int _port = 50000;
        public bool IsConnected { get { return _connected;} }
        public string IpAddr { get { return _ip; } }
        public SocketClient(string ip, ushort port = 50000)
        {
            if (log == null)
            {
                log = LogManager.GetLogger(string.Format("C.{0}", ValueHelper.GetIpLastAddr(ip)));
            }
            this._ip = ip;
            this._port = port;
            _connTimeoutEvent = new ManualResetEvent(false);
            _sendTimeoutEvent = new ManualResetEvent(false);
        }

        public bool Connect(uint timeout = 1000) {
            IPAddress ipAdress = IPAddress.Parse(_ip);
            IPEndPoint ep = new IPEndPoint(ipAdress, _port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _connected = false;

            log.InfoFormat("连接中...");
            _socket.BeginConnect(ep, new AsyncCallback(OnClientConnected), _socket);
            if (!_connTimeoutEvent.WaitOne((int)timeout, false))
            {
                _socket.Close();
                log.ErrorFormat("连接超时: {0}. ", timeout);
            }
            else
            {
                log.Info("已连接.");
            }
            return _connected;
        }

        private void OnClientConnected(IAsyncResult r)
        {
            try
            {
                var socket = r.AsyncState as Socket;
                if (socket != null)
                {
                    socket.EndConnect(r);
                    _connected = true;
                }
            }
            catch (Exception  )
            {
                _connected = false;
            }
            finally
            {
                _connTimeoutEvent.Set();
            }
        }
        private byte[] _recvBuffer;
        private int _recvLen;
        public byte[] SSend(byte[] tosend, out SocketError err, int timeout = 1000)
        {
            err = SocketError.Success;
            if (!_connected || _socket == null || !_socket.Connected)
            {
                err = SocketError.NotConnected;
                return null;
            }
            lock (this)
            {
                if (_socket == null) return null;
                try
                {
                    _socket.Send(tosend);
                    //_socket.BeginSend(tosend,0,tosend.Length, SocketFlags.None, null, _socket);
                    _recvBuffer = new byte[BUFF_SIZE];
                    log.DebugFormat("> {0,3}: {1}", tosend.Length, ValueHelper.BytesToHexStr(tosend));
                    _sendTimeoutEvent.Reset();
                    _recvLen = 0;
                    IAsyncResult iar = _socket.BeginReceive(_recvBuffer, 0, BUFF_SIZE, SocketFlags.None, out err,
                        new AsyncCallback(receiveCallback), _socket);
                    if (!_sendTimeoutEvent.WaitOne(timeout, false))
                    {
                        _socket.EndReceive(iar);
                        log.ErrorFormat("< 超时: {0}", timeout);
                    }
                    if (_recvLen > 0)
                    {
                        log.DebugFormat("< {0,3}: {1}", _recvLen, ValueHelper.BytesToHexStr(_recvBuffer, _recvLen));
                    }
                    byte[] outBuff = new byte[_recvLen];
                    Array.Copy(_recvBuffer, outBuff, _recvLen);
                    return outBuff;
                }
                catch (Exception e)
                {
                    log.ErrorFormat("SSend erro: {0}", e.Message);
                    return null;
                }
                finally
                {

                }
            }
        }

        private void receiveCallback(IAsyncResult ar)
        {
            try
            {
                _recvLen =  _socket.EndReceive(ar);
            }
            catch (Exception e)
            {
                log.ErrorFormat("< 错误: {0}", e.Message);
            }
            finally
            {
                _sendTimeoutEvent.Set();
            }
        }

        public void Close()
        {
            if (_socket != null && _connected)
            {
                log.Info("连接已断开.");
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _connected = false;
                _socket = null;
            }
        }
    }
}
