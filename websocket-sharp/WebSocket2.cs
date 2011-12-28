#region MIT License
/**
 * WebSocket.cs
 *
 * A C# implementation of a WebSocket protocol client.
 * This code derived from WebSocket.java (http://github.com/adamac/Java-WebSocket-client).
 *
 * The MIT License
 *
 * Copyright (c) 2009 Adam MacBeth
 * Copyright (c) 2010 sta.blockhead
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

//namespace WebSocketSharp {
//    using System;
//    using System.Collections.Generic;
//    using System.IO;
//    using System.Linq;
//    using System.Net;
//    using System.Net.Security;
//    using System.Net.Sockets;
//    using System.Text;
//    using System.Threading;
//    using System.Security.Cryptography;

//    public delegate void MessageEventHandler(Object sender, String eventdata);

//    public class WebSocket : IDisposable {

//        private Uri uri;
//        public String Url {
//            get { return uri.ToString(); }
//        }

//        private Object sync = new Object();

//        private volatile WsState readyState;
//        public WsState ReadyState {
//            get { return readyState; }

//            private set {
//                switch (value) {
//                    case WsState.OPEN:
//                        this.readyState = value;
//                        if (this.OnOpen != null) {
//                            this.OnOpen(this, EventArgs.Empty);
//                        }
//                        break;
//                    case WsState.CLOSING:
//                    case WsState.CLOSED:
//                        lock (sync) {
//                            this.Close(value);
//                        }
//                        break;
//                }
//            }
//        }

//        private StringBuilder unTransmittedBuffer;
//        public String UnTransmittedBuffer {
//            get { return unTransmittedBuffer.ToString(); }
//        }

//        public Int64 BufferedAmount { get; private set; }
//        public String Protocol { get; private set; }

//        private TcpClient tcpClient;
//        private IWsStream wsStream;

//        public event EventHandler OnOpen;
//        public event MessageEventHandler OnMessage;
//        public event MessageEventHandler OnError;
//        public event EventHandler OnClose;

//        /// <summary>
//        /// コンストラクタ
//        /// </summary>
//        /// <param name="url"></param>
//        public WebSocket(String url)
//            : this(url, String.Empty) {
//        }

//        /// <summary>
//        /// コンストラクタ
//        /// </summary>
//        /// <param name="url"></param>
//        /// <param name="protocol"></param>
//        public WebSocket(String url, String protocol) {
//            var ub = new UriBuilder(url);

//            if (ub.Scheme != "ws" && ub.Scheme != "wss") {
//                throw new ArgumentException("Unsupported scheme: " + ub.Scheme);
//            }

//            if (ub.Port <= 0) {
//                if (ub.Scheme == "wss") {
//                    ub.Port = 443;
//                } else {
//                    ub.Port = 80;
//                }
//            }

//            this.uri = ub.Uri;

//            this.readyState = WsState.CONNECTING;
//            this.unTransmittedBuffer = new StringBuilder();
//            this.BufferedAmount = 0;
//            this.Protocol = protocol;
//        }

//        /// <summary>
//        /// 接続します。
//        /// </summary>
//        public void Connect() {
//            if (this.readyState == WsState.OPEN) {
//                return;
//            }

//            this.tcpClient = new TcpClient(this.uri.Host, this.uri.Port);
//            var host = uri.Scheme == "wss" ? this.uri.Host : String.Empty;
//            this.wsStream = this.CreateConnection(this.tcpClient.GetStream(), host);
//            this.DoHandshake(this.uri);

//            //			ThreadPool.QueueUserWorkItem(obj => Message());
//        }

//        public void Send(String data) {
//            if (this.readyState == WsState.CONNECTING) {
//                throw new InvalidOperationException("Handshake not complete.");
//            }

//            var dataBuffer = Encoding.UTF8.GetBytes(data);

//            try {
//                this.wsStream.WriteByte(0x00);
//                this.wsStream.Write(dataBuffer, 0, dataBuffer.Length);
//                this.wsStream.WriteByte(0xff);
//            } catch (Exception e) {
//                this.unTransmittedBuffer.Append(data);
//                this.BufferedAmount += dataBuffer.Length;

//                if (OnError != null) {
//                    OnError(this, e.Message);
//                }
//            }
//        }

//        public void Close() {
//            this.ReadyState = WsState.CLOSING;
//        }

//        public void Dispose() {
//            this.Close();
//        }

//        private void Close(WsState state) {
//            if (this.readyState == WsState.CLOSING ||
//                this.readyState == WsState.CLOSED) {
//                return;
//            }

//            this.readyState = state;

//            if (this.wsStream != null) {
//                this.wsStream.Close();
//                this.wsStream.Dispose();
//                this.wsStream = null;
//            }

//            if (this.tcpClient != null) {
//                this.tcpClient.Close();
//                this.tcpClient = null;
//            }

//            if (this.OnClose != null) {
//                this.OnClose(this, EventArgs.Empty);
//            }
//        }

//        private IWsStream CreateConnection(NetworkStream netStream, String sslHost) {
//            if (String.IsNullOrWhiteSpace(sslHost)) {
//                return new WsStream<NetworkStream>(netStream);
//            } else {
//                var sslStream = new SslStream(netStream);
//                sslStream.AuthenticateAsClient(sslHost);
//                return new WsStream<SslStream>(sslStream);
//            }
//        }

//        private void DoHandshake(Uri uri) {
//            var secKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
//            var requestMessage = this.CreateHandshakeMessage(uri, secKey);
//            var response = this.SendHandshake(requestMessage);
//            //if (!(response.IsValid(expectedCR, actualCR, out msg))) {
//            //    throw new IOException(msg);
//            //}

//            for (int i = 3; i < response.Length; i++) {
//                if (response[i].Contains("Sec-WebSocket-Protocol:")) {
//                    int j = response[i].IndexOf(":");
//                    this.Protocol = response[i].Substring(j + 1).Trim();
//                    break;
//                }
//            }
//            ReadyState = WsState.OPEN;
//        }

//        private String CreateHandshakeMessage(Uri uri, String secKey) {
//            var path = uri.PathAndQuery;
//            var host = uri.DnsSafeHost;

//            if (uri.Port != 80) {
//                host += ":" + uri.Port;
//            }

//            var sb = new StringBuilder();
//            sb.AppendFormat("GET {0} HTTP/1.1\r\n", path);
//            sb.AppendFormat("Host: {0}\r\n", host);
//            sb.Append("Upgrade: websocket\r\n");
//            sb.Append("Connection: Upgrade\r\n");
//            sb.AppendFormat("Sec-WebSocket-Key: {0}\r\n", secKey);
//            sb.Append("Origin: null\r\n");
//            sb.Append("Sec-WebSocket-Protocol: chat, superchat\r\n");
//            sb.Append("Sec-WebSocket-Version: 13");
//            return sb.ToString();
//        }

//        private String[] SendHandshake(String openingHandshake) {
//            var sendBuffer = Encoding.UTF8.GetBytes(openingHandshake);
//            this.wsStream.Write(sendBuffer, 0, sendBuffer.Length);

//            IList<Byte> rawdata = new List<Byte>();
//            while (true) {
//                var data = wsStream.ReadByte();
//                if (data == -1) {
//                    break;
//                }

//                rawdata.Add((Byte)data);

//                //if (wsStream.ReadByte().EqualsWithSaveTo('\r', rawdata) &&
//                //    wsStream.ReadByte().EqualsWithSaveTo('\n', rawdata) &&
//                //    wsStream.ReadByte().EqualsWithSaveTo('\r', rawdata) &&
//                //    wsStream.ReadByte().EqualsWithSaveTo('\n', rawdata)) {
//                //    wsStream.Read(challengeResponse, 0, challengeResponse.Length);
//                //    break;
//                //}
//            }

//            return Encoding.UTF8.GetString(rawdata.ToArray()).Replace("\r\n", "\n").Replace("\n\n", "\n").Split('\n');
//        }

//        private void Message() {
//            String data;
//            while (readyState == WsState.OPEN) {
//                data = Receive();

//                if (OnMessage != null && data != null) {
//                    OnMessage(this, data);
//                }
//            }
//        }

//        private String Receive() {
//            try {
//                byte frame_type = (byte)wsStream.ReadByte();
//                byte b;

//                if ((frame_type & 0x80) == 0x80) {
//                    // Skip data frame
//                    int len = 0;
//                    int b_v;

//                    do {
//                        b = (Byte)wsStream.ReadByte();
//                        b_v = b & 0x7f;
//                        len = len * 128 + b_v;
//                    } while ((b & 0x80) == 0x80);

//                    for (int i = 0; i < len; i++) {
//                        wsStream.ReadByte();
//                    }

//                    if (frame_type == 0xff && len == 0) {
//                        ReadyState = WsState.CLOSED;
//                    }
//                } else if (frame_type == 0x00) {
//                    IList<Byte> raw_data = new List<Byte>();

//                    while (true) {
//                        b = (Byte)wsStream.ReadByte();

//                        if (b == 0xff) {
//                            break;
//                        }

//                        raw_data.Add(b);
//                    }

//                    return Encoding.UTF8.GetString(raw_data.ToArray());
//                }
//            } catch (Exception e) {
//                if (readyState == WsState.OPEN) {
//                    if (OnError != null) {
//                        OnError(this, e.Message);
//                    }

//                    ReadyState = WsState.CLOSED;
//                }
//            }

//            return null;
//        }
//    }
//}
