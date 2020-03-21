namespace GoClient
{
    using LitJson;

    public enum PackageType
    {
        PKG_HANDSHAKE = 1,
        PKG_HANDSHAKE_ACK = 2,
        PKG_HEARTBEAT = 3,
        PKG_DATA = 4,
        PKG_KICK = 5
    }

    public enum MessageType
    {
        MSG_REQUEST = 0,
        MSG_NOTIFY = 1,
        MSG_RESPONSE = 2,
        MSG_PUSH = 3
    }

    public class Package
    {
        public PackageType type;
        public int length;
        public byte[] body;

        public Package(PackageType type)
        {
            this.type = type;
            this.length = 0;
            this.body = null;
        }
        public Package(PackageType type, byte[] body)
        {
            this.type = type;
            this.length = body.Length;
            this.body = body;
        }
    }

    public class Message
    {
        public MessageType type;
        public string route;
        public uint id;
        public byte[] data;

        public Message(MessageType type, uint id, string route, byte[] data)
        {
            this.type = type;
            this.id = id;
            this.route = route;
            this.data = data;
        }
    }
}