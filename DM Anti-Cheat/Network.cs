using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace DM_Anti_Cheat
{
   static public class Network
    {
        //Http Request
        public static string host = "https://dbd-mix.xyz";
        public static HttpResponseMessage Http(string url, HttpMethod method, List<List<string>> headers, string body = "")
        {
            try
            {
                HttpClient client = new HttpClient();
               // ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                HttpRequestMessage request = new HttpRequestMessage
                {                   
                    RequestUri = new Uri(url),
                    Method = method
                };

                if (method != HttpMethod.Get)
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                if (headers != null)
                    for (int i = 0; i < headers.Count; i++)
                        request.Headers.Add(headers[i][0], headers[i][1]);

                HttpResponseMessage response = null;
                Thread th = new Thread(async () =>
                {
                    response = await client.SendAsync(request);
                });
                th.Start();
                while (response == null) Thread.Sleep(200);
                //Task<HttpResponseMessage> response = await client.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.TargetSite);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException.Message);

                throw ex; //Http Error
            }
        }

        //Connnect to Service Server
        //public static Socket ServerConnect() 
        //{
        //    const string ip = "37.140.192.106";
        //    const int port = 7777;
        //    //Entry Endpoint
        //    try
        //    {
        //        IPEndPoint tcpEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        //        //Socket
        //        Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //        tcpSocket.Connect(tcpEndpoint);
        //        return tcpSocket;
        //    }
        //    catch(Exception ex) //IF Socket Connect Error then throw Exception
        //    {
        //        MessageBox.Show(ex.Message);
        //        throw new Exception("Server Connect Error");
        //    }
        //}

        //public static void ConnectionAlive(ref Socket connection, ref string cookie)
        //{
        //    string json = $"{{\"Session\":\"{cookie}\",\"Action\":\"Alive\"}}";
        //    //Connection Alive
        //    try
        //    {
        //        while (true)
        //        {
        //            connection.Send(Encoding.UTF8.GetBytes(json));
        //            Thread.Sleep(5000);
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //        throw new Exception("Connection Alive Error");
        //    }
        //}

    }
}
