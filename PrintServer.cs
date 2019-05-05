using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using PrinterHelper;

public class PrintServer
{

    
    static string DATA_PATH = "C:\\vPrinter\\data\\";
    static string PROPERTIES_FILE = "C:\\vPrinter\\app.properties";
    private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public static IDictionary ReadDictionaryFile(string fileName)
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        foreach (string line in File.ReadAllLines(fileName))
        {
            if ((!string.IsNullOrEmpty(line)) &&
                (!line.StartsWith(";")) &&
                (!line.StartsWith("#")) &&
                (!line.StartsWith("'")) &&
                (line.Contains("=")))
            {
                int index = line.IndexOf('=');
                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();

                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                    (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                dictionary.Add(key, value);
            }
        }
        return dictionary;
    }

    static void Main(string[] args)
    {
        IDictionary  properties = ReadDictionaryFile(PROPERTIES_FILE);
        logger.Info("Starting Printer Daemon !");

        int PRINTER_PORT = Convert.ToInt32(properties["port"]);
        string PRINTER_NAME = Convert.ToString(properties["printer_name"]);

        logger.Info("FORWARDING PRINTER NAME => " + PRINTER_NAME);

        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, PRINTER_PORT);
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);
            }
            catch(Exception e)
            {
                logger.Info("Cannot bind to localhost!");
                logger.Info(e.ToString());
            }

            logger.Info("Printer Daemon listening on => "+ localEndPoint);

            while (true)
            {
                logger.Info("Waiting connection ... ");
                Socket clientSocket = listener.Accept();
                byte[] bytes = new Byte[2048];
                string data = null;
                int numByte = clientSocket.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes,0, numByte);

                long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                string filename = "txn_" + milliseconds+ ".txt";
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(DATA_PATH, filename)))
                {
                    outputFile.WriteLine(data);
                }
                logger.Info("Written file => " + filename );

                RawPrinterHelper.SendStringToPrinter(PRINTER_NAME, data);
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }
        catch (Exception e)
        {
            logger.Info(e.ToString());
        }

    }
}
