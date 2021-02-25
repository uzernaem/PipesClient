using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Pipes
{
    public partial class Client : Form
    {

        private IntPtr PipeHandle;   // дескриптор канала
        private IntPtr ReturnPipeHandle;   // дескриптор канала
        private string login;
        public string Login { get { return login; } set { this.login = value; } }
        private string server;
        public string Server { get { return server; } set { this.server = value; } }
        private List<Thread> threads = new List<Thread>();

        public Client(string svr, string lg)
        {
            InitializeComponent();
            Server = svr;
            Login = lg;
            this.Text += ": " + login;
            SendMessage(Login);
            Thread t = new Thread(ReceiveMessage);
            threads.Add(t);
            t.Start();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage(Login + " >> " + tbMessage.Text);// закрываем дескриптор канала
            tbMessage.Text = "";
        }

        private void SendMessage(string msg)
        {
            uint BytesWritten = 0;
            byte[] buff = Encoding.Unicode.GetBytes(msg);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            // открываем именованный канал, имя которого указано в поле tbPipe

            PipeHandle = NamedPipeStream.CreateFile("\\\\" + Server + "\\pipe\\ServerPipe", (uint)NamedPipeStream.EFileAccess.GenericWrite, (uint)NamedPipeStream.EFileShare.Read, (IntPtr)0, (uint)NamedPipeStream.ECreationDisposition.OpenExisting, 0, (IntPtr)0);


            NamedPipeStream.WriteFile(PipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, (IntPtr)0);         // выполняем запись последовательности байт в канал
            NamedPipeStream.FlushFileBuffers(PipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала

            NamedPipeStream.CloseHandle(PipeHandle);
        }

        private void ReceiveMessage()
        {
            while (true)
            {
                uint realBytesReaded = 0;
                byte[] buff = new byte[1024];


                ReturnPipeHandle = NamedPipeStream.CreateFile("\\\\" + Server + "\\pipe\\" + login, (uint)NamedPipeStream.EFileAccess.GenericRead, (uint)NamedPipeStream.EFileShare.Write, (IntPtr)0, (uint)NamedPipeStream.ECreationDisposition.OpenExisting, 0, (IntPtr)0);

                NamedPipeStream.FlushFileBuffers(ReturnPipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                if (NamedPipeStream.ReadFile(ReturnPipeHandle, buff, 1024, ref realBytesReaded, (IntPtr)0))
                {
                    string msg = Encoding.Unicode.GetString(buff);

                    rtbMessages.Invoke((MethodInvoker)delegate
                    {
                        rtbMessages.Text += "\n >> " + msg;                             // выводим полученное сообщение на форму
                    });
                }

                NamedPipeStream.CloseHandle(ReturnPipeHandle);
            }


        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendMessage(Login);
            foreach (Thread t in threads)
            {
                if (t != null)
                    t.Abort();
            }
        }

        private void rtbMessages_TextChanged(object sender, EventArgs e)
        {
            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }
    }
}
