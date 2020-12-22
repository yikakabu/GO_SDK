/*
 * DualSensor.cs
 * 
 * Gocator 2000/2300 C# Sample
 * Copyright (C) 2013-2018 by LMI Technologies Inc.
 * 
 * Licensed under The MIT License.
 * Redistributions of files must retain the above copyright notice.
 *
 * Purpose: Connect to Gocator buddy system and receive range data in Profile Mode and translate to engineering units (mm). Gocator must be in Profile Mmode.
 * Ethernet output for the profile data must be enabled. Gocator buddy is removed at the end.
 */

using System;
using System.Runtime.InteropServices;

static class Constants
{
    public const string SENSOR_IP = "192.168.1.10"; // IP of the main sensor used for sensor connection GoSystem_FindSensorByIpAddress() call.
    public const string BUDDY_IP = "192.168.1.11"; // IP of the buddy sensor used for sensor connection GoSystem_FindSensorByIpAddress() call.
#if DEBUG
    public const string GODLLPATH = @"GoSdk.dll";
    public const string KAPIDLLPATH = @"kApi.dll";
#else
    public const string GODLLPATH = @"GoSdk.dll";
    public const string KAPIDLLPATH = @"kApi.dll";
#endif
}

namespace ReceiveProfile
{
    public class DataContext
    {
        public double xResolution;
        public double zResolution;
        public double xOffset;
        public double zOffset;
        public uint serialNumber;
    }

    public struct address
    {
        public Int32 version;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] IPaddress;
    }

    public struct GoStamp
    {
        public UInt64 frameIndex;
        public UInt64 timestamp;
        public Int64 encoder;
        public Int64 encoderAtZ;
        public UInt64 reserved;
    }

    public struct GoPoints
    {
        public Int16 x;
        public Int16 y;
    }

    public struct ProfilePoint
    {
        public double x;
        public double z;
        byte intensity;
    }

    class ReceiveProfile
    {
        enum kStatus
        {
            kERROR_STATE = -1000,                                                // Invalid state.
            kERROR_NOT_FOUND = -999,                                             // Item is not found.
            kERROR_COMMAND = -998,                                               // Command not recognized.
            kERROR_PARAMETER = -997,                                             // Parameter is invalid.
            kERROR_UNIMPLEMENTED = -996,                                         // Feature not implemented.
            kERROR_HANDLE = -995,                                                // Handle is invalid.
            kERROR_MEMORY = -994,                                                // Out of memory.
            kERROR_TIMEOUT = -993,                                               // Action timed out.
            kERROR_INCOMPLETE = -992,                                            // Buffer not large enough for data.
            kERROR_STREAM = -991,                                                // Error in stream.
            kERROR_CLOSED = -990,                                                // Resource is no longer avaiable. 
            kERROR_VERSION = -989,                                               // Invalid version number.
            kERROR_ABORT = -988,                                                 // Operation aborted.
            kERROR_ALREADY_EXISTS = -987,                                        // Conflicts with existing item.
            kERROR_NETWORK = -986,                                               // Network setup/resource error.
            kERROR_HEAP = -985,                                                  // Heap error (leak/double-free).
            kERROR_FORMAT = -984,                                                // Data parsing/formatting error. 
            kERROR_READ_ONLY = -983,                                             // Object is read-only (cannot be written).
            kERROR_WRITE_ONLY = -982,                                            // Object is write-only (cannot be read). 
            kERROR_BUSY = -981,                                                  // Agent is busy (cannot service request).
            kERROR_CONFLICT = -980,                                              // State conflicts with another object.
            kERROR_OS = -979,                                                    // Generic error reported by underlying OS.
            kERROR_DEVICE = -978,                                                // Hardware device error.
            kERROR_FULL = -977,                                                  // Resource is already fully utilized.
            kERROR_IN_PROGRESS = -976,                                           // Operation is in progress, but not yet complete.
            kERROR = 0,                                                          // General error. 
            kOK = 1                                                              // Operation successful. 
        }

        enum GoDataMessageTypes
        {
            GO_DATA_MESSAGE_TYPE_UNKNOWN = -1,
            GO_DATA_MESSAGE_TYPE_STAMP = 0,
            GO_DATA_MESSAGE_TYPE_HEALTH = 1,
            GO_DATA_MESSAGE_TYPE_VIDEO = 2,
            GO_DATA_MESSAGE_TYPE_RANGE = 3,
            GO_DATA_MESSAGE_TYPE_RANGE_INTENSITY = 4,
            GO_DATA_MESSAGE_TYPE_PROFILE = 5,
            GO_DATA_MESSAGE_TYPE_PROFILE_INTENSITY = 6,
            GO_DATA_MESSAGE_TYPE_RESAMPLED_PROFILE = 7,
            GO_DATA_MESSAGE_TYPE_SURFACE = 8,
            GO_DATA_MESSAGE_TYPE_SURFACE_INTENSITY = 9,
            GO_DATA_MESSAGE_TYPE_MEASUREMENT = 10,
            GO_DATA_MESSAGE_TYPE_ALIGNMENT = 11,
            GO_DATA_MESSAGE_TYPE_EXPOSURE_CAL = 12
        }

        enum GoRole
        {
            GO_ROLE_MAIN = 0,                                                    // Sensor is operating as a main sensor.
            GO_ROLE_BUDDY = 1                                                    // Sensor is operating as a buddy sensor.
        }

        // use DLL import to access GoSdkd.dll/GoSdk.dll and kApid.dll/kApi.dll
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSdk_Construct(ref IntPtr assembly);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSystem_Construct(ref IntPtr system, IntPtr allocator);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSystem_FindSensorById(IntPtr system, UInt32 id, ref IntPtr sensor);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSystem_FindSensorByIpAddress(IntPtr system, IntPtr addr, ref IntPtr sensor);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSystem_EnableData(IntPtr system, bool enable);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSystem_ReceiveData(IntPtr system, ref IntPtr data, UInt64 timeout);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSensor_Connect(IntPtr sensor);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSystem_Start(IntPtr system);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSystem_Stop(IntPtr system);
        [DllImport(Constants.GODLLPATH)]
        private static extern IntPtr GoSensor_Setup(IntPtr sensor);
        [DllImport(Constants.GODLLPATH)]
        private static extern bool GoSensor_HasBuddy(IntPtr sensor);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSensor_AddBuddy(IntPtr sensor, IntPtr buddy);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoSensor_RemoveBuddy(IntPtr sensor);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoDataSet_Count(IntPtr dataset);
        [DllImport(Constants.GODLLPATH)]
        private static extern IntPtr GoDataSet_At(IntPtr dataset, UInt32 index);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoStampMsg_Count(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern IntPtr GoStampMsg_At(IntPtr msg, UInt32 index);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoResampledProfileMsg_Count(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern IntPtr GoResampledProfileMsg_At(IntPtr msg, UInt32 index);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoResampledProfileMsg_Width(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoResampledProfileMsg_XResolution(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoResampledProfileMsg_ZResolution(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoResampledProfileMsg_XOffset(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoResampledProfileMsg_ZOffset(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoProfileMsg_Count(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern IntPtr GoProfileMsg_At(IntPtr msg, UInt32 index);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoProfileMsg_Width(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoProfileMsg_XResolution(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoProfileMsg_ZResolution(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoProfileMsg_XOffset(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoProfileMsg_ZOffset(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoProfileIntensityMsg_Count(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern IntPtr GoProfileIntensityMsg_At(IntPtr msg, UInt32 index);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoProfileIntensityMsg_Width(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern GoDataMessageTypes GoDataMsg_Type(IntPtr msg);
        [DllImport(Constants.GODLLPATH)]
        private static extern UInt32 GoSetup_XSpacingCount(IntPtr setup, GoRole role);
        [DllImport(Constants.GODLLPATH)]
        private static extern Int32 GoDestroy(IntPtr obj);
        [DllImport(Constants.KAPIDLLPATH)]
        private static extern Int32 kIpAddress_Parse(IntPtr addrPtr, [MarshalAs(UnmanagedType.LPStr)] string text);

        static int Main(string[] args)
        {
            kStatus status;
            IntPtr api = IntPtr.Zero;
            IntPtr system = IntPtr.Zero;
            IntPtr sensor = IntPtr.Zero;
            IntPtr buddy = IntPtr.Zero;
            IntPtr dataset = IntPtr.Zero;
            IntPtr dataObj = IntPtr.Zero;
            IntPtr stampMsg = IntPtr.Zero;
            IntPtr profileMsg = IntPtr.Zero;
            IntPtr stampPtr = IntPtr.Zero;
            GoStamp stamp;
            IntPtr addrPtr = IntPtr.Zero;
            IntPtr intensityPtr = IntPtr.Zero;
            address addr = new address();
            address addr_buddy = new address();
            IntPtr addrPtr_buddy = IntPtr.Zero;

            DataContext context = new DataContext();

            if ((status = (kStatus)GoSdk_Construct(ref api)) != kStatus.kOK)
            {
                Console.WriteLine("GoSdk_Construct Error:{0}", (int)status);
                return (int)status;
            }

            if ((status = (kStatus)GoSystem_Construct(ref system, IntPtr.Zero)) != kStatus.kOK)
            {
                Console.WriteLine("GoSystem_Construct Error:{0}", (int)status);
                return (int)status;
            }

            addrPtr = Marshal.AllocHGlobal(Marshal.SizeOf(addr));
            Marshal.StructureToPtr(addr, addrPtr, false);

            if ((status = (kStatus)kIpAddress_Parse(addrPtr, Constants.SENSOR_IP)) != kStatus.kOK)
            {
                Console.WriteLine("kIpAddress_Parse Error:{0}", (int)status);
                return (int)status;
            }

            if ((status = (kStatus)GoSystem_FindSensorByIpAddress(system, addrPtr, ref sensor)) != kStatus.kOK)
            {
                Console.WriteLine("GoSystem_FindSensorByIpAddress Error:{0}", (int)status);
                return (int)status;
            }
            //NOTE: GoSystem_Connect() can be used on Gocator buddy systems already configured, the GoSystem() will be able to
            //recognize the buddy sensor as a paired buddy and not attempt to obtain a separate connection from the budfdy sensor. 
            //In this example, we are trying to pair two sensors as buddies, hence the need to use GoSensor_Connect() to retrieve sensor
            if ((status = (kStatus)GoSensor_Connect(sensor)) != kStatus.kOK)
            {
                Console.WriteLine("GoSensor_Connect Error:{0}", (int)status);
                return (int)status;
            }

            if (!GoSensor_HasBuddy(sensor))
            {
                addrPtr_buddy = Marshal.AllocHGlobal(Marshal.SizeOf(addr_buddy));
                Marshal.StructureToPtr(addr_buddy, addrPtr_buddy, false);
                if ((status = (kStatus)kIpAddress_Parse(addrPtr_buddy, Constants.BUDDY_IP)) != kStatus.kOK)
                {
                    Console.WriteLine("kIpAddress_Parse Error:{0}", (int)status);
                    return (int)status;
                }
                if ((status = (kStatus)GoSystem_FindSensorByIpAddress(system, addrPtr_buddy, ref buddy)) != kStatus.kOK)
                {
                    Console.WriteLine("GoSystem_FindSensorByIpAddress Error:{0}", (int)status);
                    return (int)status;
                }
                if ((status = (kStatus)GoSensor_Connect(buddy)) != kStatus.kOK)
                {
                    Console.WriteLine("GoSensor_Connect Error:{0}", (int)status);
                    return (int)status;
                }
                if ((status = (kStatus)GoSensor_AddBuddy(sensor, buddy)) != kStatus.kOK)
                {
                    Console.WriteLine("GoSensor_AddBuddy Error:{0}", (int)status);
                    return (int)status;
                }
                Console.WriteLine("Buddy sensor assigned.");
            }

            if ((status = (kStatus)GoSystem_EnableData(system, true)) != kStatus.kOK)
            {
                Console.WriteLine("GoSystem_EnableData Error:{0}", (int)status);
                return (int)status;
            }

            if ((status = (kStatus)GoSystem_Start(system)) != kStatus.kOK)
            {
                Console.WriteLine(" GoSystem_Start Error:{0}", (int)status);
                return (int)status;
            }

            // read data from sensor
            if ((status = (kStatus)GoSystem_ReceiveData(system, ref dataset, 2000000)) == kStatus.kOK)
            {
                // each result can have multiple data items
                // loop through all items in result message
                for (UInt32 i = 0; i < GoDataSet_Count(dataset); ++i)
                {
                    dataObj = GoDataSet_At(dataset, i);
                    switch (GoDataMsg_Type(dataObj))
                    {
                        // retrieve GoStamp message
                        case GoDataMessageTypes.GO_DATA_MESSAGE_TYPE_STAMP:
                            {
                                stampMsg = dataObj;
                                for (UInt32 j = 0; j < GoStampMsg_Count(stampMsg); j++)
                                {
                                    stampPtr = GoStampMsg_At(stampMsg, j);
                                    stamp = (GoStamp)Marshal.PtrToStructure(stampPtr, typeof(GoStamp));
                                    Console.WriteLine("Frame Index = {0}", stamp.frameIndex);
                                    Console.WriteLine("Time Stamp = {0}", stamp.timestamp);
                                    Console.WriteLine("Encoder Value = {0}", stamp.encoder);
                                }
                            }
                            break;
                        // retreieve resampled profile data
                        case GoDataMessageTypes.GO_DATA_MESSAGE_TYPE_RESAMPLED_PROFILE:
                            {
                                profileMsg = dataObj;
                                Console.WriteLine("  Resampled Profile Message batch count: {0}", GoResampledProfileMsg_Count(profileMsg));
                                for (UInt32 k = 0; k < GoResampledProfileMsg_Count(profileMsg); ++k)
                                {
                                    int validPointCount = 0;
                                    UInt32 profilePointCount = GoResampledProfileMsg_Width(profileMsg);
                                    Console.WriteLine("    Item[{0}]: Profile data ({1} points)", k, GoResampledProfileMsg_Width(profileMsg));
                                    context.xResolution = GoResampledProfileMsg_XResolution(profileMsg) / 1000000;
                                    context.zResolution = GoResampledProfileMsg_ZResolution(profileMsg) / 1000000;
                                    context.xOffset = GoResampledProfileMsg_XOffset(profileMsg) / 1000;
                                    context.zOffset = GoResampledProfileMsg_ZOffset(profileMsg) / 1000;
                                    short[] points = new short[profilePointCount];
                                    ProfilePoint[] profileBuffer = new ProfilePoint[profilePointCount];
                                    IntPtr pointsPtr = GoResampledProfileMsg_At(profileMsg, k);
                                    Marshal.Copy(pointsPtr, points, 0, points.Length);

                                    for (UInt32 arrayIndex = 0; arrayIndex < profilePointCount; ++arrayIndex)
                                    {
                                        if (points[arrayIndex] != -32768)
                                        {
                                            profileBuffer[arrayIndex].x = context.xOffset + context.xResolution * arrayIndex;
                                            profileBuffer[arrayIndex].z = context.zOffset + context.zResolution * points[arrayIndex];
                                            validPointCount++;
                                        }
                                        else
                                        {
                                            profileBuffer[arrayIndex].x = context.xOffset + context.xResolution * arrayIndex;
                                            profileBuffer[arrayIndex].z = -32768;
                                        }
                                    }
                                    Console.WriteLine("Received {0} Range Points", profilePointCount);
                                    Console.WriteLine("Valid Points {0}", validPointCount);
                                }
                            }
                            break;

                        case GoDataMessageTypes.GO_DATA_MESSAGE_TYPE_PROFILE:
                            {
                                profileMsg = dataObj;
                                Console.WriteLine("  Profile Message batch count: {0}", GoProfileMsg_Count(profileMsg));
                                for (UInt32 k = 0; k < GoProfileMsg_Count(profileMsg); ++k)
                                {
                                    int validPointCount = 0;
                                    UInt32 profilePointCount = GoProfileMsg_Width(profileMsg);
                                    Console.WriteLine("    Item[{0}]: Profile data ({1} points)", i, GoProfileMsg_Width(profileMsg));
                                    context.xResolution = GoProfileMsg_XResolution(profileMsg) / 1000000;
                                    context.zResolution = GoProfileMsg_ZResolution(profileMsg) / 1000000;
                                    context.xOffset = GoProfileMsg_XOffset(profileMsg) / 1000;
                                    context.zOffset = GoProfileMsg_ZOffset(profileMsg) / 1000;
                                    GoPoints[] points = new GoPoints[profilePointCount];
                                    ProfilePoint[] profileBuffer = new ProfilePoint[profilePointCount];
                                    int structSize = Marshal.SizeOf(typeof(GoPoints));
                                    IntPtr pointsPtr = GoProfileMsg_At(profileMsg, k);
                                    for (UInt32 array = 0; array < profilePointCount; ++array)
                                    {
                                        IntPtr incPtr = new IntPtr(pointsPtr.ToInt64() + array * structSize);
                                        points[array] = (GoPoints)Marshal.PtrToStructure(incPtr, typeof(GoPoints)); 
                                    }

                                    for (UInt32 arrayIndex = 0; arrayIndex < profilePointCount; ++arrayIndex)
                                    {
                                        if (points[arrayIndex].x != -32768)
                                        {
                                            profileBuffer[arrayIndex].x = context.xOffset + context.xResolution * points[arrayIndex].x;
                                            profileBuffer[arrayIndex].z = context.xOffset + context.xResolution * points[arrayIndex].y;
                                            validPointCount++;
                                        }
                                        else
                                        {
                                            profileBuffer[arrayIndex].x = -32768;
                                            profileBuffer[arrayIndex].z = -32768;
                                        }
                                    }
                                    Console.WriteLine("Received {0} Range Points", profilePointCount);
                                    Console.WriteLine("Valid Points {0}", validPointCount);
                                }
                            }
                            break;

                        case GoDataMessageTypes.GO_DATA_MESSAGE_TYPE_PROFILE_INTENSITY:
                            {
                                profileMsg = dataObj;
                                Console.WriteLine("  Profile Intensity Message batch count: {0}", GoProfileIntensityMsg_Count(profileMsg));
                                for (UInt32 k = 0; k < GoProfileIntensityMsg_Count(profileMsg); ++k)
                                {
                                    byte[] intensity = new byte[GoProfileIntensityMsg_Width(profileMsg)];
                                    intensityPtr = GoProfileIntensityMsg_At(profileMsg, k);
                                    Marshal.Copy(intensityPtr, intensity, 0, intensity.Length);
                                }
                            }
                            break;
                    }
                }
            }

            if ((status = (kStatus)GoSystem_Stop(system)) != kStatus.kOK)
            {
                Console.Write("GoSystem_Stop Error:{0}", (int)status);
                return (int) status;
            }

            if (GoSensor_HasBuddy(sensor))
            {
                if ((status = (kStatus)GoSensor_RemoveBuddy(sensor)) != kStatus.kOK)
                {
                    Console.Write("GoSensor_RemoveBuddy Error:{0}", (int)status);
                    return (int)status;
                }
                Console.WriteLine("Buddy sensor removed.");
            }
            // destroy handles
            GoDestroy(system);
            GoDestroy(api);

            // wait for ESC key
            Console.WriteLine("\nPress ENTER to continue");
            do
            {
                System.Threading.Thread.Sleep(100);

            } while (Console.Read() != (int)ConsoleKey.Enter);

            return (int)kStatus.kOK;
        }
    }
}
