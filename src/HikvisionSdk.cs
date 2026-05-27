using System.Runtime.InteropServices;

namespace IoCameraCapture;

internal static class HikvisionSdk
{
    public const int COMM_ALARM_V30 = 0x4000;
    public const int AlarmTypeSignal = 0;
    public const int MaxAlarmOutV30 = 96;
    public const int MaxChannelV30 = 64;
    public const int MaxDiskV30 = 33;
    public const int SerialNumberLength = 48;
    public const int DeviceAddressMaxLength = 129;
    public const int LoginUserNameMaxLength = 64;
    public const int LoginPasswordMaxLength = 64;
    public const int NetSdkInitCfgSdkPath = 2;
    public const int NetSdkInitCfgLibeayPath = 3;
    public const int NetSdkInitCfgSsleayPath = 4;
    public const int NetDvrLocalCfgTypeGeneral = 17;

    public delegate bool MsgCallBackV31(
        int command,
        ref NET_DVR_ALARMER alarmer,
        IntPtr alarmInfo,
        uint bufferLength,
        IntPtr user);

    public delegate void LoginResultCallback(
        int userId,
        int result,
        IntPtr deviceInfo,
        IntPtr user);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetDllDirectory(string? pathName);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint GetShortPathName(string longPath, char[] shortPath, uint bufferLength);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_SetSDKInitCfg(int enumType, ref NET_DVR_LOCAL_SDK_PATH inBuffer);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_SetSDKInitCfg(int enumType, byte[] inBuffer);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_Init();

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_Cleanup();

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_SetLogToFile(int logLevel, string logDir, bool autoDelete);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_SetSDKLocalCfg(int enumType, ref NET_DVR_LOCAL_GENERAL_CFG inBuffer);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_SetDVRMessageCallBack_V31(MsgCallBackV31 callback, IntPtr user);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern int NET_DVR_Login_V40(ref NET_DVR_USER_LOGIN_INFO loginInfo, ref NET_DVR_DEVICEINFO_V40 deviceInfo);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_Logout(int userId);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern int NET_DVR_SetupAlarmChan_V41(int userId, ref NET_DVR_SETUPALARM_PARAM setupParam);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_CloseAlarmChan_V30(int alarmHandle);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern bool NET_DVR_CaptureJPEGPicture(int userId, int channel, ref NET_DVR_JPEGPARA jpegPara, string pictureFileName);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool NET_DVR_CaptureJPEGPicture_NEW(
        int userId,
        int channel,
        ref NET_DVR_JPEGPARA jpegPara,
        [Out] byte[] jpegPicBuffer,
        uint picSize,
        ref uint sizeReturned);

    [DllImport("HCNetSDK.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern uint NET_DVR_GetLastError();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NET_DVR_LOCAL_SDK_PATH
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string sPath;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] byRes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_LOCAL_GENERAL_CFG
    {
        public byte byExceptionCbDirectly;
        public byte byNotSplitRecordFile;
        public byte byResumeUpgradeEnable;
        public byte byAlarmJsonPictureSeparate;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] byRes;

        public ulong i64FileSize;
        public uint dwResumeUpgradeTimeout;
        public byte byAlarmReconnectMode;
        public byte byStdXmlBufferSize;
        public byte byMultiplexing;
        public byte byFastUpgrade;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 232)]
        public byte[] byRes1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_DEVICEINFO_V30
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SerialNumberLength)]
        public byte[] sSerialNumber;

        public byte byAlarmInPortNum;
        public byte byAlarmOutPortNum;
        public byte byDiskNum;
        public byte byDVRType;
        public byte byChanNum;
        public byte byStartChan;
        public byte byAudioChanNum;
        public byte byIPChanNum;
        public byte byZeroChanNum;
        public byte byMainProto;
        public byte bySubProto;
        public byte bySupport;
        public byte bySupport1;
        public byte bySupport2;
        public ushort wDevType;
        public byte bySupport3;
        public byte byMultiStreamProto;
        public byte byStartDChan;
        public byte byStartDTalkChan;
        public byte byHighDChanNum;
        public byte bySupport4;
        public byte byLanguageType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] byRes2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_DEVICEINFO_V40
    {
        public NET_DVR_DEVICEINFO_V30 struDeviceV30;
        public byte bySupportLock;
        public byte byRetryLoginTime;
        public byte byPasswordLevel;
        public byte byProxyType;
        public uint dwSurplusLockTime;
        public byte byCharEncodeType;
        public byte bySupportDev5;
        public byte bySupport;
        public byte byLoginMode;
        public int dwOEMCode;
        public int iResidualValidity;
        public byte byResidualValidity;
        public byte bySingleStartDTalkChan;
        public byte bySingleDTalkChanNums;
        public byte byPassWordResetLevel;
        public byte bySupportStreamEncrypt;
        public byte byMarketType;
        public byte byTLSCap;
        public byte byChildManage;
        public byte byPlaybackNewPosCap;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 235)]
        public byte[] byRes2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_USER_LOGIN_INFO
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DeviceAddressMaxLength)]
        public byte[] sDeviceAddress;

        public byte byUseTransport;
        public ushort wPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LoginUserNameMaxLength)]
        public byte[] sUserName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LoginPasswordMaxLength)]
        public byte[] sPassword;

        public LoginResultCallback? cbLoginResult;
        public IntPtr pUser;

        [MarshalAs(UnmanagedType.Bool)]
        public bool bUseAsynLogin;

        public byte byProxyType;
        public byte byUseUTCTime;
        public byte byLoginMode;
        public byte byHttps;
        public int iProxyID;
        public byte byVerifyMode;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 119)]
        public byte[] byRes3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_ALARMER
    {
        public byte byUserIDValid;
        public byte bySerialValid;
        public byte byVersionValid;
        public byte byDeviceNameValid;
        public byte byMacAddrValid;
        public byte byLinkPortValid;
        public byte byDeviceIPValid;
        public byte bySocketIPValid;
        public int lUserID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SerialNumberLength)]
        public byte[] sSerialNumber;

        public uint dwDeviceVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string sDeviceName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] byMacAddr;

        public ushort wLinkPort;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string sDeviceIP;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string sSocketIP;

        public byte byIpProtocol;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] byRes1;

        public byte bJSONBroken;
        public ushort wSocketPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] byRes2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_ALARMINFO_V30
    {
        public uint dwAlarmType;
        public uint dwAlarmInputNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxAlarmOutV30)]
        public byte[] byAlarmOutputNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxChannelV30)]
        public byte[] byAlarmRelateChannel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxChannelV30)]
        public byte[] byChannel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxDiskV30)]
        public byte[] byDiskNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_SETUPALARM_PARAM
    {
        public uint dwSize;
        public byte byLevel;
        public byte byAlarmInfoType;
        public byte byRetAlarmTypeV40;
        public byte byRetDevInfoVersion;
        public byte byRetVQDAlarmType;
        public byte byFaceAlarmDetection;
        public byte bySupport;
        public byte byBrokenNetHttp;
        public ushort wTaskNo;
        public byte byDeployType;
        public byte bySubScription;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] byRes1;

        public byte byAlarmTypeURL;
        public byte byCustomCtrl;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_JPEGPARA
    {
        public ushort wPicSize;
        public ushort wPicQuality;
    }
}
