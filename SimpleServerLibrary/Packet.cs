using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleServerLib {
    public static class Packet {

        public static byte[] MakePacket(byte[] data) {
            int len = data.Length + sizeof(int);
            byte[] packet = new byte[len];
            Array.Copy(BitConverter.GetBytes(len), 0, packet, 0, sizeof(int));
            Array.Copy(data, 0, packet, sizeof(int), data.Length);
            return packet;
        }

        public static byte[] DecodePacket(byte[] packet) {
            byte[] dataLenBytes = new byte[sizeof(int)];
            Array.Copy(packet, 0, dataLenBytes, 0, sizeof(int));
            int dataLen = BitConverter.ToInt32(dataLenBytes, 0) - sizeof(int);
            byte[] data = new byte[dataLen];
            Array.Copy(packet, sizeof(int), data, 0, dataLen);
            return data;
        }

        public static int GetByteCount(byte[] data) {
            byte[] dataLenBytes = new byte[sizeof(int)];
            Array.Copy(data, 0, dataLenBytes, 0, sizeof(int));
            return BitConverter.ToInt32(dataLenBytes, 0);
        }
    }
}
