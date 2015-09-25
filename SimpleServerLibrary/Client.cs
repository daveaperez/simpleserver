using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace SimpleServerLib {
    public class Client {
        public StateObject ClientStateObject { get; set; }

        public Client() {
            ClientStateObject = new StateObject();
            ClientStateObject.Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public void Connect(string host, int port) {
            if(!ClientStateObject.Client.Connected)
                ClientStateObject.Client.Connect(host, port);
            //ClientStateObject.Client.Close();
        }

        public void Send(byte[] data) {
            ClientStateObject.Client.Send(data);
        }
    }
}
