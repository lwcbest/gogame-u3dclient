namespace GoClient
{
    using LitJson;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using UnityEngine;
    using System.Threading;
    using System.Text;
    using PB = Google.Protobuf;

    public class LeafClient
    {
        public const string EVENT_DISCONNECT = "disconnect";

        private TcpClient tcpclient;
        private EventManager eventManager;
        private SimonMsgPipline msgPipline;
        private Transporter transporter;
        private HeartBeat heartBeat;

        private bool handshaking = false;

        private uint reqId = 1;
        private string host;
        private int port;

        private Dictionary<string, Action<object>> onReceivePushCallbackDic;

        public LeafClient(string host, int port)
        {
            this.host = host;
            this.port = port;
            this.eventManager = new EventManager();
            this.onReceivePushCallbackDic = new Dictionary<string, Action<object>>();
            this.tcpclient = new TcpClient();
            this.msgPipline = new SimonMsgPipline();
        }

        /// <summary>
        /// sync connect server.
        /// </summary>
        /// <param name="timeout">receive timeout, in millisecond. 3000 is by defualt</param>
        /// <returns>SimonSocketResult</returns>
        public bool ConnectServer(int timeout = 3000)
        {
            bool conRes = false;
            IAsyncResult result = tcpclient.BeginConnect(IPAddress.Parse(this.host), this.port, null, null);
            bool sucess = result.AsyncWaitHandle.WaitOne(timeout);
            if (!sucess)
            {
                this.Disconnect("connected go server time out!");
                Debug.LogError("connected time is longer then the time which you set!");
                conRes = false;
            }
            else
            {
                if (this.tcpclient.Connected)
                {
                    this.transporter = new Transporter(this.tcpclient, this.ProcessServerPackage);
                    this.transporter.start(this.onTransportDisconnect, this.onTransportSendError, this.onTransportReceiveError);
                    //request handshake
                    this.handshaking = true;
                    this.SendHandShake();
                    conRes = true;
                }
                else
                {
                    Debug.LogError("Shit,socket did not connect~");
                    conRes = false;
                    this.Disconnect("handshake error~");
                }
            }

            tcpclient.EndConnect(result);
            return conRes;
        }

        public void Req(string route,byte[] data,Action<object> action){
            this.request(route,data,res=>{
                
              string jsonString = string.Empty;
              if (res == null)
              {
                  action.Invoke(null);
              }
              else
              {
                 // jsonString = res.ToJson();
                  action.Invoke(res);
              }
            });
        }

        public void On(string eventName, Action<object> action)
        {
            this.onReceivePushCallbackDic.Add(eventName, action);
            this.eventManager.AddOnEvent(eventName, HandleOnEventTrigger);
        }

        private void HandleOnEventTrigger(string eventName, byte[] data)
        {
            Debug.Log("HandleOnEventTrigger" + JsonMapper.ToJson(data));
            if (eventName == EVENT_DISCONNECT)
            {
                //TODO
                Debug.LogError("TJODODODODODO");
                //this.onReceivePushCallbackDic[eventName].Invoke(data["reason"].ToJson());
                return;
            }

            if (data == null)
            {
                this.onReceivePushCallbackDic[eventName].Invoke("");
                return;
            }
            else
            {
                this.onReceivePushCallbackDic[eventName].Invoke(data);
            }
        }

        private void request(string route, byte[] msg, Action<byte[]> action)
        {
            this.eventManager.AddCallBack(reqId, action);
            Message message = new Message(MessageType.MSG_REQUEST, reqId, route, msg);
            byte[] encodedMsg = this.msgPipline.MsgEncode(message);
            byte[] encodedPkg = this.msgPipline.PackageEncode(new Package(PackageType.PKG_DATA, encodedMsg));
            this.transporter.send(encodedPkg);
            reqId++;
        }

        public void Disconnect(string reason)
        {
            // JsonData reasonJD = new JsonData();
            // reasonJD["reason"] = reason;
            this.CloseAll(new byte[]{});
        }

        public void Disconnect(JsonData reasonJD)
        {

            this.CloseAll(new byte[]{});
        }

        private void CloseAll(byte[] disconnectReason)
        {
            try
            {
                this.transporter.stop();
                this.heartBeat.stop();
                this.tcpclient.Close();
            }
            catch (Exception e)
            {
                 Debug.Log("TODODODODOODODODODO");
                // if (!disconnectReason.ContainsKey("reason"))
                // {
                //     //TODO
                //     Debug.Log("TODODODODOODODODODO");
                   
                // }
            }
            finally
            {
                eventManager.InvokeOnEvent(EVENT_DISCONNECT, disconnectReason);
            }
        }

        private void onTransportDisconnect()
        {
            this.Disconnect("transporter closed");
        }

        private void onTransportSendError(Exception e)
        {
            this.Disconnect(e.Message);
        }

        private void onTransportReceiveError(Exception e)
        {
            this.Disconnect(e.Message);
        }

        private void SendHandShake()
        {
            string data = "{}";
            byte[] body = Encoding.UTF8.GetBytes(data);
            Package pkg = new Package(PackageType.PKG_HANDSHAKE, body);
            byte[] pkgData = this.msgPipline.PackageEncode(pkg);
            this.transporter.send(pkgData);
        }

        private void SendHandShakeAck()
        {
            Package pkg = new Package(PackageType.PKG_HANDSHAKE_ACK);
            byte[] pkgData = this.msgPipline.PackageEncode(pkg);
            this.transporter.send(pkgData);
        
        }

        internal void SendHeartBeat()
        {
            Package pkg = new Package(PackageType.PKG_HEARTBEAT);
            byte[] pkgData = this.msgPipline.PackageEncode(pkg);
            this.transporter.send(pkgData);
        }

        private void ProcessServerMessage(Message msg)
        {
            if (msg.type == MessageType.MSG_RESPONSE)
            {
                eventManager.InvokeCallBack(msg.id, msg.data);
            }
            else if (msg.type == MessageType.MSG_PUSH)
            {
                eventManager.InvokeOnEvent(msg.route, msg.data);
            }
        }

        private void ProcessServerPackage(byte[] bytes)
        {
            Package pkg = this.msgPipline.PackageDecode(bytes);
            Debug.Log("rec pkg:"+pkg.type.ToString()+pkg.body.ToString());
            //Ignore all the message except handshading at handshake stage
            if (pkg.type == PackageType.PKG_HANDSHAKE && this.handshaking)
            {
                //Ignore all the message except handshading
                string handshakeContent = Encoding.UTF8.GetString(pkg.body);
                Debug.Log("[Protocol]received handshake:" + handshakeContent);
                try
                {
                    JsonData data = JsonMapper.ToObject(handshakeContent);
                    if (!data.ContainsKey("code") || !data.ContainsKey("sys") || Convert.ToInt32(data["code"].ToJson()) != 200)
                    {
                        throw new Exception("Handshake error! Please check your handshake config.");
                    }

                    //Set compress data
                    JsonData sys = data["sys"];
                    JsonData protos = new JsonData();
                    JsonData reqProtos = new JsonData();
                    JsonData resProtos = new JsonData();
                    JsonData pushProtos = new JsonData();
                    if (!sys.ContainsKey("protos") || !sys.ContainsKey("heartbeat"))
                    {
                        throw new Exception("[sys] no [protos] or [heartbeat]");
                    }

                    protos = sys["protos"];
                    reqProtos = protos["req"];
                    resProtos = protos["res"];
                    pushProtos = protos["push"];

                    //Init heartbeat service
                    int interval = Convert.ToInt32(sys["heartbeat"].ToJson());
                    heartBeat = new HeartBeat(this, interval);

                    if (interval > 0)
                    {
                        heartBeat.start();
                    }

                    //send ack and change protocol state
                    this.SendHandShakeAck();
                    this.handshaking = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("Parse JSON error:" + e.Message);
                    throw e;
                }
            }
            else if (pkg.type == PackageType.PKG_HEARTBEAT && this.handshaking == false)
            {
                this.heartBeat.resetTimeout();
            }
            else if (pkg.type == PackageType.PKG_DATA && this.handshaking == false)
            {
                this.heartBeat.resetTimeout();
                Message serverMsg = this.msgPipline.MsgDecode(pkg.body);
                this.ProcessServerMessage(serverMsg);
            }
            else if (pkg.type == PackageType.PKG_KICK)
            {
                string kickReason = Encoding.UTF8.GetString(pkg.body);
                this.Disconnect(JsonMapper.ToObject(kickReason));
            }
        }
    }
}

