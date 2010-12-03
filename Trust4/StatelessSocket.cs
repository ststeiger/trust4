using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Timers;
using System.Net;
using System.Threading;

namespace Trust4
{
    public class StatelessSocket
    {
        private Socket m_Socket = null;
        private System.Timers.Timer m_Timer = new System.Timers.Timer(5000);
        private IPAddress p_IPAddress = IPAddress.None;
        private int p_Port = 12000;
        private ManualResetEvent m_ListenDone = new ManualResetEvent(false);
        private bool m_Connected = false;

        public event EventHandler OnSent;
        public event StatelessEventHandler OnReceived;
        public event StatelessEventHandler OnConnected;
        public event EventHandler OnDisconnected;

        /// <summary>
        /// Creates a client stateless socket (periodically attempts to
        /// reconnect).
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public StatelessSocket(IPAddress ip, int port)
        {
            this.p_IPAddress = ip;
            this.p_Port = port;
            this.m_Timer.Elapsed += new ElapsedEventHandler(m_Timer_Elapsed);
            this.m_Timer.Start();
        }

        /// <summary>
        /// Creates a client stateless socket based on an existing socket.
        /// </summary>
        public StatelessSocket(Socket client)
        {
            this.m_Socket = client;
            this.m_Connected = client.Connected;
        }

        /// <summary>
        /// Creates a listening stateless socket (but does not start listening).
        /// </summary>
        public StatelessSocket()
        {
        }

        /// <summary>
        /// This event is raised every 5 seconds to check to make sure the socket is connected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.m_Connected && this.m_Socket != null && !this.m_Socket.Connected && this.OnDisconnected != null)
                this.OnDisconnected(this, new EventArgs());

 	        if (this.m_Socket == null || !this.m_Socket.Connected)
            {
                this.m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    this.m_Socket.Connect(this.p_IPAddress, this.p_Port);
                }
                catch (SocketException) { }

                this.m_Connected = this.m_Socket.Connected;

                if (this.m_Connected)
                {
                    if (this.OnConnected != null)
                        this.OnConnected(this, new StatelessEventArgs(this.m_Socket, null));
                }
            }
        }

        /// <summary>
        /// Whether the socket is currently connected.
        /// </summary>
        public bool Connected
        {
            get
            {
                if (this.m_Socket != null)
                    return this.m_Socket.Connected;
                else
                    return false;
            }
        }

        /// <summary>
        /// The host's end point (the local computer).
        /// </summary>
        public EndPoint HostEndPoint
        {
            get
            {
                return this.m_Socket.LocalEndPoint;
            }
        }

        /// <summary>
        /// The client's end point (the peer's computer).
        /// </summary>
        public EndPoint ClientEndPoint
        {
            get
            {
                return this.m_Socket.RemoteEndPoint;
            }
        }

        /// <summary>
        /// Sends the data to the peer.
        /// </summary>
        /// <param name="data">The data to send.</param>
        public void Send(string data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data + "<EOF>");

            Console.WriteLine(this.m_Socket.RemoteEndPoint + " < " + data);

            // Begin sending the data to the remote device.
            try
            {
                this.m_Socket.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(this.SendCallback), this.m_Socket);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    this.m_Timer_Elapsed(this, null);
                    this.m_Socket.BeginSend(byteData, 0, byteData.Length, 0,
                        new AsyncCallback(this.SendCallback), this.m_Socket);
                }
            }
        }

        /// <summary>
        /// This function is called when the send operation completes.
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            // End the send operation.
            this.m_Socket.EndSend(ar);

            if (this.OnSent != null)
                this.OnSent(this, new EventArgs());
        }

        /// <summary>
        /// This function synchronously waits for a response.
        /// </summary>
        /// <returns>The next message.</returns>
        public string Receive()
        {
            StringBuilder storage = new StringBuilder();

            byte[] buffer = new byte[1024];
            int read = 0;
            try
            {
                read = this.m_Socket.Receive(buffer);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    this.m_Timer_Elapsed(this, null);
                    read = this.m_Socket.Receive(buffer);
                }
            }

            if (read > 0)
            {
                // There might be more data, so store the data received so far.
                storage.Append(Encoding.ASCII.GetString(
                    buffer, 0, read));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                string content = storage.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read.  Handle it.
                    Console.WriteLine(this.m_Socket.RemoteEndPoint + " > " + content.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0]);

                    return content.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0];
                }
                else
                {
                    // Not all data received. Get more.
                    return content + this.Receive();
                }
            }
            else
                return "";
        }

        /// <summary>
        /// Puts the socket into listening mode, raising OnReceive when
        /// new messages arrive.  This is a synchronous operation (i.e.
        /// it listens on the current thread).
        /// </summary>
        public void Listen(IPEndPoint endpoint)
        {
            this.m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.m_Socket.Bind(endpoint);
            this.m_Socket.Listen(20);

            while (true)
            {
                // Set the event to nonsignaled state.
                this.m_ListenDone.Reset();

                // Start an asynchronous socket to listen for connections.
                this.m_Socket.BeginAccept(
                    new AsyncCallback(this.AcceptCallback),
                    this.m_Socket);

                // Wait until a connection is made before continuing.
                this.m_ListenDone.WaitOne();
            }
        }

        /// <summary>
        /// A class for keeping state between accepts and receive operations.
        /// </summary>
        private class StateObject
        {
            // Client socket.
            public Socket Socket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] Buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder Storage = new StringBuilder();
        }

        /// <summary>
        /// This function is called when the listening socket accepts a
        /// connection from a client.
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            this.m_ListenDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            if (this.OnConnected != null)
                this.OnConnected(this, new StatelessEventArgs(handler, null));

            // Create the state object.
            StateObject state = new StateObject();
            state.Socket = handler;
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(this.ReadCallback), state);
        }

        /// <summary>
        /// This function is called when the listening socket has read data
        /// from the client.
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.Socket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.Storage.Append(Encoding.ASCII.GetString(
                    state.Buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.Storage.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read.  Handle it.
                    Console.WriteLine(handler.RemoteEndPoint + " > " + content.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0]);

                    if (this.OnReceived != null)
                        this.OnReceived(this, new StatelessEventArgs(handler, content.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0]));

                    // Start receiving data again.
                    StateObject nstate = new StateObject();
                    nstate.Socket = handler;
                    handler.BeginReceive(nstate.Buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(this.ReadCallback), nstate);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(this.ReadCallback), state);
                }
            }
        }
    }    

    public class StatelessEventArgs
    {
        private string p_Data = null;
        private StatelessSocket p_Client = null;

        public StatelessEventArgs(Socket client, string data)
        {
            this.p_Data = data;
            this.p_Client = new StatelessSocket(client);
        }

        public string Data
        {
            get
            {
                return this.p_Data;
            }
        }

        public StatelessSocket Client
        {
            get
            {
                return this.p_Client;
            }
        }
    }

    public delegate void StatelessEventHandler(object sender, StatelessEventArgs e);
}
