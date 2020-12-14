using MyLib;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AsyncBank
{
    class Bank
    {

        public static DataBase db = new DataBase();

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static void StartListening(string localIp, int localPort)
        {
            // Punto de conexión con el servidor
            IPAddress ipAddress = System.Net.IPAddress.Parse(localIp);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, localPort);
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                Console.WriteLine("Esperando conexiones en {0}", localEndPoint);
                while (true)
                {
                    allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR en la iniciación del servidor.");
            }
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                allDone.Set();
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR en la llamada de aceptar conexiones.");
            }
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            // Leer datos del socket cliente  
            int bytesRead = handler.EndReceive(ar);
            // Problema cuando se leen los datos en una lectura de buffer en el servidor,
            // si no quedan datos por leer desde el cliente no se vuelve a llamar a ReadCallback
            if (handler.Available > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            else
            {
                Transaction tr;
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                }
                if (state.sb.Length > 1)
                {
                    string request = state.sb.ToString();
                    tr = Transaction.FromXml(request);
                    //-------------------------
                    Register(tr);
                    //-------------------------
                }
                else
                {
                    tr = new Transaction();
                    Console.WriteLine("Transacción incorrectamente recibida.");
                    Console.WriteLine("ERROR");
                }
                Send(handler, tr);
            }
        }

        private static void Send(Socket handler, Transaction tr)
        {
            try
            {
                byte[] byteData = tr.ToByteArray();
                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR en el envío de datos.");
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR en la llamada de envío de datos.");
            }
        }

        public static void Register(Transaction tr)
        {
            Console.WriteLine("La siguiente transacción va a registrarse: {0}", tr.ToString());
            if (db.Transfer(tr))
            {
                Console.WriteLine("Los nuevos saldos son los siguientes:");
                Console.WriteLine("Saldo usuario {0}: {1}", tr.From, db.Balance(tr.From));
                Console.WriteLine("Saldo usuario {0}: {1}", tr.To, db.Balance(tr.To));
                tr.Status = true;
            }
            else
            {
                Console.WriteLine("ERROR, no se puede realizar la transacción.\n");
                tr.Status = false;
            }
        }

        public static int Main(String[] args)
        {
            //Iniciación servidor
            string localIp = "192.168.1.103";
            int listeningPort = 11000;
            //Escucha de comunicaciones
            StartListening(localIp, listeningPort);
            return 0;
        }
    }

}
