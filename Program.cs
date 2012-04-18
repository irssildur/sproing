using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Threading;

namespace SproingServer
{

    /*
     * 
     * Communication protocol:
     * The first byte is the Command, others are the message (ASCII)
     * 
     */

    class Program
    {
        // Server's IP and Port
        const String IP = "192.168.1.9";
        const Int32 PORT = 1234;

        // List for store the connected clients
        static List<MyClient> clients = new List<MyClient>();


        static void Main(string[] args)
        {

            // Setting up and starting the server
            IPAddress serverIP = IPAddress.Parse(IP);
            TcpListener server = new TcpListener(serverIP, PORT);
            server.Start();

            // Setting up and 
            Timer clientReportTimer = new Timer(clientReport);
            clientReportTimer.Change(0, 1000);

            // Waiting for new connections
            while (true)
            {

                Console.WriteLine("Waiting for connection");

                // If there is a new connection we accept it
                // and increase the clientId variable
                TcpClient client = server.AcceptTcpClient();
                MyClient.clientId++;


                Console.WriteLine("Client Connection Request Accepted");

                // We create a new instance of MyClient, which manages communication
                MyClient tempClient = new MyClient();

                tempClient.id = MyClient.clientId;

                tempClient.client = client;
                tempClient.ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

                // Creating a new thread for the client
                Thread thread = new Thread(new ParameterizedThreadStart(tempClient.clientHandler));
                tempClient.thread = thread;

                // adding the client to the List
                clients.Add(tempClient);

                thread.Start(tempClient);



            }


        }


        // Timer eventHandler for reporting about the connected Clients
        static void clientReport(object state)
        {

            foreach (MyClient client in clients)
            {
                Console.WriteLine(client.report());
            }

        }


        // Removing the client from the clients List
        static public void removeClient(MyClient client)
        {

            clients.Remove(client);


        }


    }





    // MyClient Class
    // Representing and handling the client
    public class MyClient
    {
        // Server - client command constants
        public const byte AUTH = 1;
        public const byte MSG = 2;
        public const byte DISCONNECT = 3;



        // Server to Client Messages
        public const String WELCOME_MESSAGE = "Welcome";
        public const String LOGOUT_MESSAGE = "Good Bye";

        // Handles the unique ID for the Class
        public static int clientId = 0;

        // Unique ID for the Client instance
        public int id { get; set; }

        public TcpClient client { get; set; }
        public IPAddress ip { get; set; }
        public Thread thread { get; set; }

        // Constructors
        public MyClient()
        {

        }

        public MyClient(int id, IPAddress ip)
        {
            this.id = id;
            this.ip = ip;
        }

        public MyClient(int id, IPAddress ip, Thread thread)
        {
            this.id = id;
            this.ip = ip;
            this.thread = thread;

        }



        // Runs in a thread, handles the client
        public void clientHandler(object param)
        {

            TcpClient client = (param as MyClient).client;
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);

            // True if client sent DISCONNECT command
            bool disconnectRequested = false;

            // Wait for messages from the client while the connection is alive or disconnect isn't requested
            while (isConnectionAlive(stream) && !disconnectRequested)
            {
                // The command byte
                byte[] command = new Byte[50];

                // read from the stream
                try
                {
                    stream.Read(command, 0, command.Length);
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }

                // Decide what to do based on the command byte
                switch (command[0])
                {

                    case AUTH:

                        // encode the Welcome Message to ascii bytes
                        byte[] strByte = System.Text.Encoding.ASCII.GetBytes(WELCOME_MESSAGE);
                        byte[] temp = new Byte[strByte.Length + 1];

                        // set the command byte
                        temp[0] = MSG;

                        // copy the message to the temp array which is to be sent
                        for (int j = 1; j < temp.Length; j++) temp[j] = strByte[j - 1];


                        // write the array to the stream and flush it
                        stream.Write(temp, 0, temp.Length);
                        stream.Flush();

                        break;


                    case DISCONNECT:

                        String disconnMsg = System.Text.Encoding.ASCII.GetString(command, 1, command.Length - 1);

                        // encode the Goodbye Message to ascii bytes
                        byte[] strByte2 = System.Text.Encoding.ASCII.GetBytes(LOGOUT_MESSAGE);
                        byte[] temp2 = new Byte[strByte2.Length + 1];

                        // set the command byte
                        temp2[0] = DISCONNECT;

                        // copy the message to the temp array which is to be sent
                        for (int j = 1; j < temp2.Length; j++) temp2[j] = strByte2[j - 1];

                        // write the array to the stream and flush it
                        stream.Write(temp2, 0, temp2.Length);
                        stream.Flush();

                        // set the disconnectRequested variable to true to exit the loop
                        disconnectRequested = true;
                        Console.WriteLine("Disconnect requested: {0}", disconnMsg);

                        break;


                }

            }


            // close the connection to the client and remove it from the List of the connected clients
            client.Close();

            Program.removeClient(param as MyClient);
            Console.WriteLine("____{0}. CLIENT Disconnected____", this.id);
        }


        // Determine if the connection to the client is avaliable or not
        private bool isConnectionAlive(NetworkStream stream)
        {
            bool isAlive = true;


            Byte[] tmp = new Byte[1];
            try
            {
                stream.Write(tmp, 0, tmp.Length);
            }
            catch
            {
                isAlive = false;
            }

            return isAlive;
        }

        // Client report: Client's ID and IP
        public String report()
        {
            return this.id + ". client has IP: " + ((IPEndPoint)this.client.Client.RemoteEndPoint).Address.ToString();
        }




    }





}
