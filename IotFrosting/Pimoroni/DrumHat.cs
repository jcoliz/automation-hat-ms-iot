﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotFrosting.Pimoroni
{
    public class DrumHat: IDisposable
    {
        /// <summary>
        /// One particular drum pad
        /// </summary>
        public class Pad : CAP1XXX.Pad
        {
            /// <summary>
            /// Id of the key, starting with 0
            /// </summary>
            public int Id { get; set; }
        }
        /// <summary>
        /// Handler for Key.Updated events
        /// </summary>
        /// <param name="sender">The key which was updated</param>
        /// <param name="args">Empty args, may be used for expansion</param>
        public delegate void PadUpdateEventHandler(Pad sender, EventArgs args);

        /// <summary>
        /// A combined set of keys, which shared a combined Updated event
        /// </summary>
        public class PadSet
        {
            /// <summary>
            /// Add keys into the set
            /// </summary>
            /// <param name="keys">Keys to add</param>
            public void AddRange(IEnumerable<IInput> keys)
            {
                foreach (var input in keys)
                {
                    var key = input as Pad;
                    key.Updated += (s, a) => Updated?.Invoke(s as Pad, a);
                    Pads[key.Id] = key;
                }
            }

            /// <summary>
            /// Extract a key by name
            /// </summary>
            /// <param name="name">Name of a key</param>
            /// <returns>Key with that name</returns>
            public Pad this[int id] => Pads[id];

            /// <summary>
            /// Raised every time any one of the keys are updated
            /// </summary>
            public event PadUpdateEventHandler Updated;

            /// <summary>
            /// Internal dictinoary of keys for fast lookup
            /// </summary>
            private Dictionary<int, Pad> Pads = new Dictionary<int, Pad>();
        }

        #region Public methods
        /// <summary>
        /// Open a connection to the piano hat
        /// </summary>
        /// <returns>Piano Hat controller</returns>
        public static async Task<DrumHat> Open()
        {
            var result = new DrumHat();
            result.Cap = await CAP1XXX.Open(0x2C, 25);

            for (int i = 0; i < 8; i++)
            {
                result.Cap.Pads[i] = new Pad() { Id = i };
            }

            result.Pads.AddRange(result.Cap.Pads);

            return result;
        }
        #endregion

        #region Public properties
        public PadSet Pads = new PadSet();
        #endregion

        #region Internal methods

        /// <summary>
        /// Don't call consturctor directly, use PianoHat.Open()
        /// </summary>
        protected DrumHat()
        {
        }
        #endregion

        #region Internal properties
        /// <summary>
        /// Capacitive input controller
        /// </summary>
        CAP1XXX Cap;

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Cap?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~PianoHat() {
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