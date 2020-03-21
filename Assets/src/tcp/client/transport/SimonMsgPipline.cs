namespace GoClient
{
    using System;
    using System.Text;
    using LitJson;
    using System.Collections.Generic;

    public class SimonMsgPipline
    {
        public static uint decodeUInt32(int offset, byte[] bytes, out int length)
        {
            uint n = 0;
            length = 0;

            for (int i = offset; i < bytes.Length; i++)
            {
                length++;
                uint m = Convert.ToUInt32(bytes[i]);
                n = n + Convert.ToUInt32((m & 0x7f) * Math.Pow(2, (7 * (i - offset))));
                if (m < 128)
                {
                    break;
                }
            }

            return n;
        }

        public static byte[] encodeUInt32(uint n)
        {
            List<byte> byteList = new List<byte>();
            do
            {
                uint tmp = n % 128;
                uint next = n >> 7;
                if (next != 0)
                {
                    tmp = tmp + 128;
                }
                byteList.Add(Convert.ToByte(tmp));
                n = next;
            } while (n != 0);

            return byteList.ToArray();
        }

        private const int MSG_Route_Limit = 255;
        private const int MSG_Type_Mask = 0x07;

        public SimonMsgPipline()
        {
        }

        /// <summary>
        /// ---------------header------------------------------
        /// |  flag  | message id  | routeLength |   route    |
        /// | 1byte  |  0-4bytes   |    1byte    | 0-256bytes |
        /// ---------------------------------------------------
        ///
        /// ---------------------flag--------------------
        /// | -----000 | -----001 | -----010 | -----011 |
        /// |  request |  notify  | response |   push   |
        /// ---------------------------------------------
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] MsgEncode(Message msg)
        {
            MessageType messageType = msg.type;
            string route = msg.route;
            uint id = msg.id;
            byte[] msgData = msg.data;

            int routeLength = GetByteLength(route);
            if (routeLength > MSG_Route_Limit)
            {
                throw new Exception("Route is too long!");
            }

            //1.Encode head


            byte flag = 0;
            switch (messageType)
            {
                case MessageType.MSG_REQUEST:
                    flag = 0;
                    break;
                case MessageType.MSG_NOTIFY:
                    flag = 1;
                    break;
                case MessageType.MSG_RESPONSE:
                    flag = 2;
                    break;
                case MessageType.MSG_PUSH:
                    flag = 3;
                    break;

            }

            int headLen = 1;
            //route
            headLen += routeLength;
            //routelen
            headLen += 1;
            byte[] bytes = null;
            if (id > 0)
            {
                bytes = encodeUInt32(id);
                headLen += bytes.Length;
            }

            byte[] head = new byte[headLen];
            
            //1.1.write flag
            head[0] = flag;
            int offset = 1;
            
            //1.2.write id
            if (id > 0)
            {
                writeBytes(bytes, offset, head);
                offset += bytes.Length;
            }

            //1.3.write route
            head[offset] = (byte)routeLength;
            offset++;
            writeBytes(Encoding.UTF8.GetBytes(route), offset, head);
            offset += routeLength;

            //2.Encode body
            byte[] body = msgData;

            if (offset != head.Length)
            {
                throw new Exception("ji suan cuo wu@@!!");
            }

            //3. result = head+body
            byte[] result = new byte[offset + body.Length];
            for (int i = 0; i < offset; i++)
            {
                result[i] = head[i];
            }

            for (int i = 0; i < body.Length; i++)
            {
                result[offset + i] = body[i];
            }

            return result;
        }

        public Message MsgDecode(byte[] buffer)
        {
            //Decode head
            uint id = 0;
            int offset = 1;

            //Get type from flag;
            MessageType type = (MessageType)(buffer[0]);

            //get req id
            switch (type)
            {
                case MessageType.MSG_REQUEST:
                case MessageType.MSG_RESPONSE:
                    int idLength;
                    //get id
                    id = (uint)decodeUInt32(offset, buffer, out idLength);
                    offset += idLength;
                    break;

                case MessageType.MSG_NOTIFY:
                    break;
                case MessageType.MSG_PUSH:
                    break;
            }

            //get route
            byte routeLength;
            routeLength = buffer[offset];
            offset += 1;
            string route = Encoding.UTF8.GetString(buffer, offset, routeLength);
            offset += routeLength;


            //Decode body
            byte[] body = new byte[buffer.Length - offset];
            for (int i = 0; i < body.Length; i++)
            {
                body[i] = buffer[i + offset];
            }

            //Construct the message
            return new Message(type, id, route, body);
        }

        /// <summary>
        /// -------header-------|-----body------
        /// |  type  |  length  |     msg      |
        /// | 1byte  |  3bytes  | length bytes |
        /// --------------------|---------------
        ///
        /// ---------------------type----------------------------------
        /// | ----0001   | ----0010 | ----0011  | ----0100 | ----0101 |
        /// |  handshake |  hs ack  | heartbeat |   data   |   kick   |
        /// -----------------------------------------------------------
        /// </summary>
        /// <param name="pkg"></param>
        /// <returns></returns>
        public byte[] PackageEncode(Package pkg)
        {
            PackageType type = pkg.type;
            byte[] body = pkg.body;

            int length = 4;
            if (body != null)
            {
                length += body.Length;
            }

            byte[] buf = new byte[length];

            int index = 0;

            buf[index++] = Convert.ToByte(type);
            buf[index++] = Convert.ToByte(pkg.length >> 16 & 0xFF);
            buf[index++] = Convert.ToByte(pkg.length >> 8 & 0xFF);
            buf[index++] = Convert.ToByte(pkg.length & 0xFF);

            while (index < length)
            {
                buf[index] = body[index - 4];
                index++;
            }

            return buf;
        }

        public Package PackageDecode(byte[] buffer)
        {
            const int headerLength = 4;
            PackageType type = (PackageType)buffer[0];

            byte[] body = new byte[buffer.Length - headerLength];

            for (int i = 0; i < body.Length; i++)
            {
                body[i] = buffer[i + headerLength];
            }

            return new Package(type, body);
        }

        private int GetByteLength(string msg)
        {
            return Encoding.UTF8.GetBytes(msg).Length;
        }

        private void writeBytes(byte[] source, int offset, byte[] target)
        {
            for (int i = 0; i < source.Length; i++)
            {
                target[offset + i] = source[i];
            }
        }

        private void writeShort(int offset, ushort value, byte[] bytes)
        {
            bytes[offset] = (byte)(value >> 8 & 0xff);
            bytes[offset + 1] = (byte)(value & 0xff);
        }
    }
}

