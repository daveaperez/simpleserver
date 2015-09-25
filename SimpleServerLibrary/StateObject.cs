using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace SimpleServerLib {
    public class StateObject {
        public delegate void TimerHandler(StateObject so);
        public event TimerHandler ReceiveTimoutOccurred;
        public const int BUFFER_SIZE = 4096;
        public byte[] ReceiveBuffer { get; set; }
        public byte[] CompleteBuffer { get; internal set; }
        public Socket Client;
        public byte[] DataLengthInBytes { get; set; }
        public int BytesExpected { get; set; }
        public int BytesReceived { get; set; }
        public int TotalBytesReceived { get; set; }
        public Timer ReceiveTimer { get; set; }

        public StateObject() {
            ReceiveBuffer = new byte[BUFFER_SIZE];
            CompleteBuffer = new byte[0];
            DataLengthInBytes = new byte[sizeof(int)];
            Client = null;
            Reset();
        }

        public void setReceiveTimer(int timeout) {
            ReceiveTimer = new Timer(new TimerCallback(receiveTimerElapsed), null, timeout, Timeout.Infinite);
        }

        void receiveTimerElapsed(object state) {
            if (ReceiveTimoutOccurred != null)
                ReceiveTimoutOccurred(this);
        }
        public void Reset() {
            Array.Clear(ReceiveBuffer, 0, BUFFER_SIZE);
            CompleteBuffer = new byte[0];
            BytesExpected = 0;
            BytesReceived = 0;
            TotalBytesReceived = 0;
            Array.Clear(DataLengthInBytes, 0, sizeof(int));
        }

        public void Add(byte[] data, int len) {
            byte[] tmpBuffer = new byte[CompleteBuffer.Length + len];
            Array.Copy(CompleteBuffer, 0, tmpBuffer, 0, CompleteBuffer.Length);
            Array.Copy(data, 0, tmpBuffer, CompleteBuffer.Length, len);
            CompleteBuffer = new byte[tmpBuffer.Length];
            Array.Copy(tmpBuffer, 0, CompleteBuffer, 0, tmpBuffer.Length);
        }
    }
}
