using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using log4net;
using System.Runtime.InteropServices;

namespace DisneyCMS.modbus
{
    public delegate MBMessage OnMsgReceivedCallback(MBConnect c, MBMessage req);
    public delegate void OnClientConnectCallback(MBConnect c);
    public delegate void OnClientDisconnectCallback(MBConnect c);

    public class MBServer
    {
        private static ILog Log = LogManager.GetLogger("MBS");
        private Socket _serverSocket;
        private bool _serverReady = false;
        private Thread _listenThread;
        private ConcurrentDictionary<string, MBConnect> _connections = new ConcurrentDictionary<string, MBConnect>();
        public OnClientConnectCallback OnClientConnect = null;
        public OnClientDisconnectCallback OnClientDisconnect = null;
        public OnMsgReceivedCallback OnMsgDealed = null;

        public bool Start()
        {
            if (_serverSocket != null)
            {
                return true; // started.
            }
            try
            {
                IPAddress ip = IPAddress.Parse("0.0.0.0");
                int myProt = 502;
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(ip, myProt));  //绑定IP地址：端口  
                _serverSocket.Listen(20);    //设定最多10个排队连接请求  
                Log.InfoFormat("服务启动成功: {0}", _serverSocket.LocalEndPoint.ToString());
                //通过Clientsoket发送数据  
                _serverReady = true;
                _listenThread = new Thread(ListenClientConnect);
                _listenThread.Start();
                return true;
            }
            catch (Exception e)
            {
                Log.ErrorFormat("无法启动服务: {0}", e.Message);
                return false;
            }
        }

        public bool Stop()
        {
            try
            {
                if (_serverSocket != null)
                {
                    Log.InfoFormat("停止服务.");
                    _serverReady = false;
                    _listenThread.Abort();
                    _serverSocket.Close();
                    _serverSocket.Dispose();
                    _serverSocket = null;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return true;
        }

        byte[] GetKeepOption()
        {
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);//是否启用Keep-Alive
            BitConverter.GetBytes((uint)60000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));//多长时间开始第一次探测:1m
            BitConverter.GetBytes((uint)2000).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);//探测时间间隔, 2s
            return inOptionValues;
        }

        private void ListenClientConnect()
        {
            while (true)
            {
                if (_serverReady && _serverSocket != null)
                {
                    Socket clientSocket = _serverSocket.Accept();
                    clientSocket.IOControl(IOControlCode.KeepAliveValues, GetKeepOption(), null);
                    string ip = clientSocket.RemoteEndPoint.ToString();
                    MBConnect c = new MBConnect(clientSocket, ip);
                    _connections[ip] = c;
                    Log.InfoFormat("客户端已连接: {0} ", ip);
                    if (OnClientConnect != null)
                    {
                        OnClientConnect.Invoke(c);
                    }

                    ReceiveThread _client = new ReceiveThread(this, c);
                    _client.OnMsgDealed = _OnMsgDealed;
                    Thread t = new Thread(_client.DoWork);
                    t.Start();
                }
                else
                {
                    break;// 停止.
                }
                Thread.Sleep(10);
            }
        }

        class ReceiveThread
        {
            private MBConnect _client;
            private MBServer _server;
            private bool _running = true;
            public OnMsgReceivedCallback OnMsgDealed = null;
            public void Stop()
            {
                _running = false;
            }

            public ReceiveThread(MBServer server, MBConnect client)
            {
                this._client = client;
                this._server = server;
            }

            public void DoWork()
            {
                Socket myClientSocket = _client.Socket;
                byte[] result = new byte[1024];
                while (_running)
                {
                    // Is Timeout.
                    try
                    {
                        if (myClientSocket.Poll(-1, SelectMode.SelectRead))
                        {
                            //通过clientSocket接收数据  
                            int aNumber = myClientSocket.Available;
                            if (aNumber > 0)
                            {
                                int receiveNumber = myClientSocket.Receive(result);
                                Log.InfoFormat("接收消息: {0}, len={1}, buff={2}", myClientSocket.RemoteEndPoint.ToString(), receiveNumber, ValueHelper.BytesToHexStr(result, receiveNumber));
                                MBMessage req = new MBMessage(result);
                                if (req != null)
                                {
                                    _client.Req++;
                                    _client.Recv += aNumber;

                                    ushort funCode = req.FC;
                                    Log.InfoFormat("消息: F={0:D2}, Unit=0x{1:X00}, len={2:D2}", req.FC, req.UID, req.Length);
                                    MBMessage resp = null;
                                    if (OnMsgDealed != null)
                                    {
                                        resp = OnMsgDealed.Invoke(_client, req);
                                    }
                                    else
                                    {
                                        Log.ErrorFormat("No delegate defined");
                                    }
                                    if (resp != null)
                                    {
                                        _client.ASend(resp);// myClientSocket.Send(resp.encode());
                                    }
                                    else
                                    {
                                        Log.ErrorFormat("Null resp, ack default.");
                                    }
                                }
                                else
                                {
                                    Log.ErrorFormat("Invalid request.");
                                }
                            }
                        }
                        else
                        {
                            // 不可读.
                            Log.InfoFormat("Clientk {0} broken.", _client.IP);
                            _server.CloseClient(_client);
                            break; 
                        }
                        if (myClientSocket.Poll(20, SelectMode.SelectError)) {
                            // Error
                            Log.InfoFormat("Clientk {0} ERROR.", _client.IP);
                            _server.CloseClient(_client);
                            break; 
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        _server.CloseClient(_client);
                        break;
                    }
                    Thread.Sleep(10);
                }
                Log.InfoFormat("Client {0} exit.", _client.IP);
            }
        }

        private MBMessage _OnMsgDealed(MBConnect c, MBMessage req)
        {
            MBMessage resp = null;
            if (OnMsgDealed != null)
            {
                resp = OnMsgDealed.Invoke(c, req);
            }
            return resp;
        }

        void CloseClient(MBConnect c)
        {
            string ip = c.IP;
            if (OnClientDisconnect != null)
            {
                OnClientDisconnect.Invoke(c);
            }
            MBConnect _removed;
            _connections.TryRemove(ip, out _removed);
            Log.InfoFormat("客户端断开: {0}", ip);
            try
            {
                c.Socket.Shutdown(SocketShutdown.Both);
                c.Socket.Close();
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Closing error:{0}", e.Message);
            }
        }

        public bool IsRunning()
        {
            return this._serverReady;
        }
    }
}
