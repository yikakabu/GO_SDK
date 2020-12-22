/*
 * ReceiveSurface.cs
 * 
 * Gocator 2000/2300 C# Sample
 * Copyright (C) 2013-2018 by LMI Technologies Inc.
 * 
 * Licensed under The MIT License.
 * Redistributions of files must retain the above copyright notice.
 *
 * Purpose: Connect to Gocator system and receive Surface data and translate to engineering units. Gocator must be in Surface Mmode.
/*
 * ReceiveSurface.cs
 * 
 * Gocator 2000/2300 C# Sample
 * Copyright (C) 2013-2018 by LMI Technologies Inc.
 * 
 * Licensed under The MIT License.
 * Redistributions of files must retain the above copyright notice.
 *
 * Purpose: Connect to Gocator system and receive Surface data and translate to engineering units. Gocator must be in Surface Mmode.
 * Ethernet output for the whole part and/or intensity data must be enabled.
 */

using System;
using System.Runtime.InteropServices;
using Lmi3d.GoSdk;
using Lmi3d.Zen;
using Lmi3d.Zen.Io;
using Lmi3d.GoSdk.Messages;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

static class Constants
{
    public const string SENSOR_IP = "192.168.1.10"; // IP of the sensor used for sensor connection GoSystem_FindSensorByIpAddress() call.
}

namespace ReceiveWholePart
{
    //public class DataContext
    //{
    //    public double xResolution;
    //    public double yResolution;
    //    public double zResolution;
    //    public double xOffset;
    //    public double yOffset;
    //    public double zOffset;
    //    public uint serialNumber;
    //}
    public struct SurfacePoints
    {
        public double x;
        public double y;
        public double z;
    }

    class ReceiveSurface
    {
        static void Main(string[] args)
        {

            //1,创建socket
            Socket tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //2,发起建立连接的请求
            IPAddress ipaddress = IPAddress.Parse("192.168.1.20");//可以把一个字符串的ip地址转化成一个ipaddress的对象
            EndPoint point = new IPEndPoint(ipaddress, 8080);
            tcpClient.Connect(point);//通过ip：端口号 定位一个要连接到的服务器端
            //从服务器端发送消息
            byte[] data = new byte[1024];
            //byte[] data1 = new byte[1024];
            //DateTime now = DateTime.Now;

            //string name = now.ToString("HHmmss");
            string name = "surface";
            string name1 = "caiji";
            //string name1 = now.ToString("HHmmss");
            //tcp(tcpClient, data, name);
            string signal = tcp1(tcpClient, data, name1);

            while (signal != null)
            {
                if (signal == "Start Collecting")
                {
                    Console.WriteLine("开始采集");

                    takephoto(name);
                    tcpClient.Send(Encoding.UTF8.GetBytes("OK"));
                    Console.WriteLine("发送成功");
                }

                signal = tcp1(tcpClient, data, name1);

            }

        }
        public static void tcp(Socket tcpClient, byte[] data, string name)
        {
            int length = tcpClient.Receive(data);          //length返回值表示接收了多少字节的数据  
            string zuobiao = Encoding.UTF8.GetString(data, 0, length);//只把接收到的数据做一个转化
            string string1 = string.Format("D:\\zuobiao\\{0}.txt", name);

            FileStream fs;

            fs = new FileStream(string1, FileMode.Create, FileAccess.Write);
            StreamWriter sr = new StreamWriter(fs);
            sr.WriteLine(zuobiao);
            sr.Close();
            fs.Close();
            Console.WriteLine("接收成功");

        }

        public static string tcp1(Socket tcpClient, byte[] data, string name)
        {
            int length = tcpClient.Receive(data);          //length返回值表示接收了多少字节的数据  
            string signal = Encoding.UTF8.GetString(data, 0, length);//只把接收到的数据做一个转化

            Console.WriteLine("接收数据" + signal);
            return signal;



        }


        public static void takephoto(string name2)
        {
            try
            {
                KApiLib.Construct();
                GoSdkLib.Construct();
                GoSystem system = new GoSystem();
                GoSensor sensor;
                KIpAddress ipAddress = KIpAddress.Parse(Constants.SENSOR_IP);
                GoDataSet dataSet = new GoDataSet();
                sensor = system.FindSensorByIpAddress(ipAddress);
                sensor.Connect();
                GoSetup setup = sensor.Setup;
                setup.ScanMode = GoMode.Surface;
                system.EnableData(true);
                system.Start();
                Console.WriteLine("Waiting for Whole Part Data...");
                dataSet = system.ReceiveData(30000000);
                //DataContext context = new DataContext();
                for (UInt32 i = 0; i < dataSet.Count; i++)
                {
                    GoDataMsg dataObj = (GoDataMsg)dataSet.Get(i);
                    switch (dataObj.MessageType)
                    {
                        case GoDataMessageType.Stamp:
                            {
                                GoStampMsg stampMsg = (GoStampMsg)dataObj;
                                for (UInt32 j = 0; j < stampMsg.Count; j++)
                                {
                                    GoStamp stamp = stampMsg.Get(j);
                                    Console.WriteLine("Frame Index = {0}", stamp.FrameIndex);
                                    Console.WriteLine("Time Stamp = {0}", stamp.Timestamp);
                                    Console.WriteLine("Encoder Value = {0}", stamp.Encoder);
                                }
                            }
                            break;
                        case GoDataMessageType.UniformSurface:
                            {
                                GoUniformSurfaceMsg goSurfaceMsg = (GoUniformSurfaceMsg)dataObj; // 定义变量gosurfacemsg,类型gosurfacemsg
                                long length = goSurfaceMsg.Length;    //surface长度
                                long width = goSurfaceMsg.Width;      //surface宽度
                                long bufferSize = width * length;
                                double XResolution = goSurfaceMsg.XResolution / 1000000.0;  //surface 数据X方向分辨率为nm,转为mm
                                double YResolution = goSurfaceMsg.YResolution / 1000000.0;  //surface 数据Y方向分辨率为nm,转为mm
                                double ZResolution = goSurfaceMsg.ZResolution / 1000000.0;  //surface 数据Z方向分辨率为nm,转为mm
                                double XOffset = goSurfaceMsg.XOffset / 1000.0;             //接收到surface数据X方向补偿单位um，转mm
                                double YOffset = goSurfaceMsg.YOffset / 1000.0;             //接收到surface数据Y方向补偿单位um，转mm
                                double ZOffset = goSurfaceMsg.ZOffset / 1000.0;             //接收到surface数据Z方向补偿单位um，转mm
                                IntPtr bufferPointer = goSurfaceMsg.Data;
                                int rowIdx, colIdx;
                                SurfacePoints[] surfacePointCloud = new SurfacePoints[bufferSize];
                                short[] ranges = new short[bufferSize];
                                Marshal.Copy(bufferPointer, ranges, 0, ranges.Length);
                                FileStream fs;
                                //string path = string.Format("D:\\xiangmu\\Grasp2.1-rc\\GrabMe2.1-rc\\data\\{0}.txt", name2);
                                string path = string.Format("D:\\Grasp\\Grasp\\data\\{0}.txt", name2);
                                //if (!File.Exists(path))
                                //{
                                fs = new FileStream(path, FileMode.Create, FileAccess.Write);

                                //else
                                //{
                                //    fs = new FileStream(path, FileMode.Append, FileAccess.Write);
                                //}
                                StreamWriter sr = new StreamWriter(fs);
                                for (rowIdx = 0; rowIdx < length; rowIdx++)//row is in Y direction
                                {
                                    for (colIdx = 0; colIdx < width; colIdx++)//col is in X direction
                                    {
                                        surfacePointCloud[rowIdx * width + colIdx].x = colIdx * XResolution + XOffset;//客户需要的点云数据X值
                                        surfacePointCloud[rowIdx * width + colIdx].y = rowIdx * YResolution + YOffset;//客户需要的点云数据Y值
                                        surfacePointCloud[rowIdx * width + colIdx].z = ranges[rowIdx * width + colIdx] * ZResolution + ZOffset;//客户需要的点云数据Z值
                                        sr.WriteLine(surfacePointCloud[rowIdx * width + colIdx].x + "," + surfacePointCloud[rowIdx * width + colIdx].y + "," + surfacePointCloud[rowIdx * width + colIdx].z);//开始写入值
                                    }
                                }
                                sr.Write("end");
                                //ushort[] ZValues = new ushort[ranges.Length];
                                //for (int k = 0; k < ranges.Length; k++)
                                //{
                                //    ZValues[k] = (ushort)(ranges[k] - short.MinValue);
                                //}

                                //for (UInt32 k = 0; k < bufferSize; k++)
                                //{
                                //    sr.WriteLine(surfacePointCloud[k].x.ToString() + "," + surfacePointCloud[k].y.ToString() + "," + surfacePointCloud[k].z.ToString());//开始写入值
                                //}
                                sr.Close();
                                fs.Close();
                            }
                            break;
                        case GoDataMessageType.SurfaceIntensity:
                            {
                                GoSurfaceIntensityMsg surfaceMsg = (GoSurfaceIntensityMsg)dataObj;
                                long width = surfaceMsg.Width;
                                long height = surfaceMsg.Length;
                                long bufferSize = width * height;
                                IntPtr bufferPointeri = surfaceMsg.Data;

                                Console.WriteLine("Whole Part Intensity Image received:");
                                Console.WriteLine(" Buffer width: {0}", width);
                                Console.WriteLine(" Buffer height: {0}", height);
                                byte[] ranges = new byte[bufferSize];
                                Marshal.Copy(bufferPointeri, ranges, 0, ranges.Length);
                            }
                            break;
                    }
                }
                system.Stop();
            }
            catch (KException ex)
            {
                Console.WriteLine("Error: {0}", ex.Status);
            }
            // wait for ESC key
            //Console.WriteLine("\nPress ENTER to continue");
            //do
            //{
            //    System.Threading.Thread.Sleep(100);
            //} while (Console.Read() != (int)ConsoleKey.Enter);
        }
    }
}