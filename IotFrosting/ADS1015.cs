﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace IotFrosting
{
    /// <summary>
    /// https://cdn-shop.adafruit.com/datasheets/ads1015.pdf
    /// </summary>
    class ADS1015 : IDisposable
    {
        public static async Task<ADS1015> Open()
        {
            var i2cSettings = new I2cConnectionSettings(I2C_ADDRESS);
            var controller = await I2cController.GetDefaultAsync();
            var device = controller.GetDevice(i2cSettings);
            var result = new ADS1015(device);

            return result;
        }

        /// <summary>
        /// Read the analog value on a given channel
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="programmable_gain"></param>
        /// <param name="samples_per_second"></param>
        /// <returns>Value of input from 0.0 to 1.0</returns>
        public async Task<double> Read(int channel = 0, int programmable_gain = PGA_4_096V, int samples_per_second = 1600)
        {
            double result = 0.0;

            // TODO: Optimize this by moving PGA and samples per second into constructor
            // sane defaults
            UInt16 config = 0x0003 | 0x0100;
            config |= SAMPLES_PER_SECOND_MAP[samples_per_second];
            config |= CHANNEL_MAP[channel];
            config |= PROGRAMMABLE_GAIN_MAP[programmable_gain];

            // set "single shot" mode
            config |= 0x8000;

            byte[] writeBuf = { REG_CFG, (byte)(config >> 8), (byte)(config & 0xff) };
            Device.Write(writeBuf);

            byte[] config_read = new byte[2];
            int timeout = 10;
            do
            {
                // Make sure we don't get stuck here foreever
                if (--timeout <= 0)
                    return 0.0;

                // Wait for the sample to become available
                await Task.Delay(TimeSpan.FromSeconds(1 / samples_per_second));

                // Check the status of the device to see if the sample is ready
                Device.Read(config_read);
            }
            // Continue this until the sampe is ready
            while ((config_read[1] & 0x80) != 0x80);

            Device.Write(new byte[] { REG_CONV });

            byte[] data = new byte[2];
            Device.Read(data);

            UInt16 value = (UInt16)((UInt16)((byte)(data[0]) << 4) | (UInt16)((byte)(data[1]) >> 4));
            if ((value & 0x800) != 0)
                value -= 1 << 12;

            result = value / 2047.0; //  Divide down to percentage of FS
            result *= programmable_gain;

            result /= 3300.0; // Divide by VCC

            if (result > 1.0)
                result = 0.0;

            return result;
        }

        SemaphoreSlim sem = new SemaphoreSlim(1);

        public async Task<double[]> ReadAll(int programmable_gain = PGA_4_096V, int samples_per_second = 1600)
        {
            const int num_inputs = 4;
            var result = new double[num_inputs];
            if (sem.CurrentCount >= 0)
            {
                await sem.WaitAsync();
                for (int i = 0; i < num_inputs; i++)
                    result[i] = await Read(i, programmable_gain, samples_per_second);

                sem.Release();
            }
            return result;
        }

        protected ADS1015(I2cDevice device)
        {
            Device = device;
        }

        const int I2C_ADDRESS = 0x48;
        const byte REG_CONV = 0x00;
        const byte REG_CFG = 0x01;

        const int PGA_6_144V = 6144;
        const int PGA_4_096V = 4096;
        const int PGA_2_048V = 2048;
        const int PGA_1_024V = 1024;
        const int PGA_0_512V = 512;
        const int PGA_0_256V = 256;

        static readonly Dictionary<int, UInt16> SAMPLES_PER_SECOND_MAP = new Dictionary<int, ushort>()
            { { 128, 0x0000 }, { 250, 0x0020 }, { 490, 0x0040 }, { 920, 0x0060 }, { 1600, 0x0080 }, { 2400, 0x00A0 }, { 3300, 0x00C0} };

        static readonly Dictionary<int, UInt16> CHANNEL_MAP = new Dictionary<int, ushort>()
            { {0, 0x4000 }, { 1, 0x5000 }, { 2, 0x6000 }, { 3, 0x7000 } };

        static readonly Dictionary<int, UInt16> PROGRAMMABLE_GAIN_MAP = new Dictionary<int, ushort>()
            { { PGA_6_144V, 0x0000 }, { PGA_4_096V, 0x0200 }, { PGA_2_048V, 0x0400 }, { PGA_1_024V, 0x0600 }, { PGA_0_512V, 0x0800 }, { PGA_0_256V, 0x0A00} };

        I2cDevice Device;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Device?.Dispose();
                    Device = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ADS1015() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
