using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoRIO.Bluetooth {

    #region Types
    public enum BtError { None, NoBluetooth, NoBLE, Disabled, NoServer, AlreadyRunning, Unknown, UnsupportedPlatform }
    #endregion

    public static class BTValues {
        public const string uartService = "49535343-FE7D-4AE5-8FA9-9FAFD205E455";
        public const string txCharacteristic = "49535343-1E4D-4BD9-BA61-23C647249616";
        public const string rxCharacteristic = "49535343-8841-43F4-A8D4-ECBE34729BB3";
    }

    public interface IBluetooth {
        void scanForService(string serviceUuid);
        BtError checkBtSupport();
        void showEnableBtPrompt(string title, string message, string confirmText, string cancelText);
        BtError enumerateDevices();
        void endEnumeration();
        void disconnect();
        void connect(string deviceAddress);
        void subscribeToUartChars();
        void writeToUart(byte[] data);
        bool hasUartService();
    }
}
