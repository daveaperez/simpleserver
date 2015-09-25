using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace SimpleServerLib {
    public class Server {

        public delegate void ConnectionHandler(StateObject so);
        public delegate void DataHandler(byte[] data, int length);
        public event ConnectionHandler Connected;
        public event ConnectionHandler Disconnected;
        public event DataHandler DataReceiveCompleted;

        public List<StateObject> clientList { get; internal set; }
        public int ClientCount { get { return clientList.Count; } }
        public int ReceiveTimeout { get; set; }
        private Socket m_listener;
        private int m_port;
        private bool m_isrunning;
        private ManualResetEvent m_resetEvent;
        private bool m_initialized;

        public Server(int port) {
            m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientList = new List<StateObject>();
            m_resetEvent = new ManualResetEvent(false);
            m_port = port;
            m_isrunning = false;
            m_initialized = false;
            ReceiveTimeout = 5000;
        }

        public void Start(bool threaded) {
            if (!m_initialized) {
                try {
                    m_listener.Bind(new IPEndPoint(IPAddress.Any, m_port));
                    m_listener.Listen(25);
                    m_isrunning = true;
                    m_initialized = true;
                }
                catch (Exception) { } //TODO
            }
            else
                m_isrunning = true;

            if (threaded) {
                new Thread(() => {
                    acceptLoop();
                    Console.WriteLine("Stopped");
                }).Start();
            }
            else
                acceptLoop();
        }

        void acceptLoop() {
            while (m_isrunning) {
                m_resetEvent.Reset();
                m_listener.BeginAccept(new AsyncCallback(accept_callback), null);
                m_resetEvent.WaitOne();
            }
        }

        public void Stop() {
            lock (clientList) {
                foreach (StateObject so in clientList) {
                    try {
                        so.Client.Close();
                    }
                    catch { }
                }
                m_isrunning = false;
                m_resetEvent.Set();
            }
        }
        void accept_callback(IAsyncResult ar) {
            StateObject so = new StateObject();
            if (!m_isrunning)
                return;
            so.Client = m_listener.EndAccept(ar);
            so.ReceiveTimoutOccurred += receiveTimeout;
            m_resetEvent.Set();
            lock (clientList) {
                clientList.Add(so);
            }
            if (Connected != null)
                Connected(so);
            so.Client.BeginReceive(so.ReceiveBuffer, 0, StateObject.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(receive_callback), so);
        }

        void receiveTimeout(StateObject so) {
            Console.WriteLine("TIMEOUT!");
        }

        void receive_callback(IAsyncResult ar) {
            StateObject so = (StateObject)ar.AsyncState;
            if (!m_isrunning)
                return;
            so.BytesReceived = so.Client.EndReceive(ar);
            if (so.BytesReceived <= 0) {
                remove_client(so);
                return;
            }

            if (so.BytesExpected == 0) {
                if (so.TotalBytesReceived == 0)
                    so.setReceiveTimer(ReceiveTimeout);
                int numBytesToCopy = sizeof(int) - so.TotalBytesReceived >= so.BytesReceived ? so.BytesReceived : sizeof(int) - so.TotalBytesReceived;
                Array.Copy(so.ReceiveBuffer, 0, so.DataLengthInBytes, so.TotalBytesReceived, numBytesToCopy);
                so.TotalBytesReceived += so.BytesReceived;
                
                if (so.TotalBytesReceived >= sizeof(int)) { //got all data, plus some on the first pass
                    so.BytesExpected = BitConverter.ToInt32(so.DataLengthInBytes, 0);
                    byte[] tmp = new byte[so.BytesReceived - numBytesToCopy];
                    Array.Copy(so.ReceiveBuffer, numBytesToCopy, tmp, 0, so.BytesReceived - numBytesToCopy);
                    so.Add(tmp, so.BytesReceived - numBytesToCopy);
                }
            }
            else {
                so.Add(so.ReceiveBuffer, so.BytesReceived);
                so.TotalBytesReceived += so.BytesReceived;
            }
            if (so.TotalBytesReceived == so.BytesExpected) {
                so.ReceiveTimer.Dispose();
                if (DataReceiveCompleted != null) {
                    DataReceiveCompleted(so.CompleteBuffer, so.TotalBytesReceived - sizeof(int));
                    so.Reset();
                }
            }

            so.Client.BeginReceive(so.ReceiveBuffer, 0, StateObject.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(receive_callback), so);
        }

        void remove_client(StateObject so) {
            lock (clientList) {
                if (clientList.Contains(so))
                    clientList.Remove(so);
            }
        }
    }
}
