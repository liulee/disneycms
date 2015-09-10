using DisneyCMS.cms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DisneyCMS.test
{
    [TestClass]
    public class JDQVisitorTester
    {
        [TestMethod]
        public void TestSetOutput()
        {
            JDQVisitor visitor = new JDQVisitor();
            JDQRequest req = new JDQRequest(1, JDQRequestType.SetOutput);
            string ip = "192.168.0.18";
            // turn off
            req.TurnOnOutput(JDQRequest.ALL);
            JDQResponse resp = visitor.Request(ip, req);
            Assert.IsTrue(resp.IsOK);
            Thread.Sleep(2000);

            // turn off
             req.TurnOffOutput(JDQRequest.ALL);
             resp = visitor.Request(ip, req);
             Assert.IsTrue(resp.IsOK);
            Thread.Sleep(2000);

            // turn on 1,3,5
            req.TurnOnOutput(0);
            req.TurnOnOutput(2);
            req.TurnOnOutput(4);
            resp = visitor.Request(ip, req);
            Assert.IsTrue(resp.IsOK);
            Thread.Sleep(2000);

            // turn off 3
            req.TurnOffOutput(2);
            resp = visitor.Request(ip, req);
            Assert.IsTrue(resp.IsOK);
        }


        [TestMethod]
        public void TestReadOutput(){
            JDQVisitor visitor = new JDQVisitor();
            JDQRequest req = new JDQRequest(1, JDQRequestType.SetOutput);
            string ip = "192.168.0.18";
            // turn off
            req.TurnOffOutput(JDQRequest.ALL);
            req.TurnOnOutput(0);
            req.TurnOnOutput(4);
            JDQResponse resp = visitor.Request(ip, req);
            Assert.IsTrue(resp.IsOK); //SetOutput OK

            JDQRequest ro = new JDQRequest(1, JDQRequestType.ReadOutput);
            resp = visitor.Request(ip, ro);
            Assert.IsTrue(resp.IsOK); // ReadOutputOK
            Assert.AreEqual(RelayState.ACTION, resp.GetRelayState(0));
            Assert.AreEqual(RelayState.RESET, resp.GetRelayState(1));
            Assert.AreEqual(RelayState.RESET, resp.GetRelayState(2));
            Assert.AreEqual(RelayState.RESET, resp.GetRelayState(3));
            Assert.AreEqual(RelayState.ACTION, resp.GetRelayState(4));
            Assert.AreEqual(RelayState.RESET, resp.GetRelayState(5));
            Assert.AreEqual(RelayState.RESET, resp.GetRelayState(6));
            Assert.AreEqual(RelayState.RESET, resp.GetRelayState(7));
            visitor.Terminate();
        }

        [TestMethod]
        public void TestRawReadOutput()
        {
            byte[] req=new byte[]{0xcc,0xdd,0xb0,0x01,0x00,0x00,0x0d,0xbe,0x7c, 0x00};
            string ip = "192.168.0.18";
            IPAddress ipAdress = IPAddress.Parse(ip);
            IPEndPoint ep = new IPEndPoint(ipAdress, 50000);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ep);
            s.Send(req);
            Thread.Sleep(10);
            byte[] buff = new byte[20];
            int len = 0;
            while (true)
            {
                if (s.Available > 0)
                {
                    len = s.Available;
                    s.Receive(buff, 20, SocketFlags.None);
                    break;
                }
                Thread.Sleep(10);
            }
            s.Close();
        }

        [TestMethod]
        public void TestFastSetOutput()
        {
            JDQVisitor visitor = new JDQVisitor();
            JDQRequest req = new JDQRequest(1, JDQRequestType.SetOutput);
            string ip = "192.168.0.18";
            
            for (int i = 0; i < 20; i++)
            {
                // turn on all
                req.TurnOnOutput(JDQRequest.ALL);
                visitor.Request(ip, req);

                // turn off all
                req.TurnOffOutput(JDQRequest.ALL);
                visitor.Request(ip, req);
            }
            visitor.Terminate();
        }

        [TestMethod]
        public void TestFastReadOutput()
        {
            JDQVisitor visitor = new JDQVisitor();
            JDQRequest req = new JDQRequest(1, JDQRequestType.ReadOutput);
            string ip = "192.168.0.18";
            // turn off
            JDQResponse resp;
            for (int i = 0; i < 100; i++)
            {
                resp = visitor.Request(ip, req);
            }
            visitor.Terminate();
        }

        [TestMethod]
        public void TestFastReadInput()
        {
            JDQVisitor visitor = new JDQVisitor();
            JDQRequest req = new JDQRequest(1, JDQRequestType.ReadInput);
            string ip = "192.168.0.18";
            // turn off
            JDQResponse resp;
            for (int i = 0; i < 100; i++)
            {
                resp = visitor.Request(ip, req);
            }
            visitor.Terminate();
        }
    }
}