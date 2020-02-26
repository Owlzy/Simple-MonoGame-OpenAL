// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

namespace P3.AudioLib
{
      internal static class ALHelper
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerHidden]
        internal static void CheckError(string message = "", params object[] args)
        {
            ALError error;
            if ((error = AL.GetError()) != ALError.NoError)
            {
                if (args != null && args.Length > 0)
                    message = String.Format(message, args);
                
                throw new InvalidOperationException(message + " (Reason: " + AL.GetErrorString(error) + ")");
            }
        }

        public static bool IsStereoFormat(ALFormat format)
        {
            return (format == ALFormat.Stereo8
                || format == ALFormat.Stereo16);
        }
    }

    public class OALSoundBuffer : IDisposable
    {
        int openALDataBuffer;
        ALFormat openALFormat;
        int dataSize;
        bool _isDisposed;

        public OALSoundBuffer()
        {
            AL.GetError();
            AL.GenBuffers(1, out openALDataBuffer);
            ALHelper.CheckError("Failed to generate OpenAL data buffer.");
        }

        ~OALSoundBuffer()
        {
            Dispose(false);
        }

        public int OpenALDataBuffer
        {
            get
            {
                return openALDataBuffer;
            }
        }

        public double Duration
        {
            get;
            set;
        }

        internal enum ALBufferi
        {
            UnpackBlockAlignmentSoft = 0x200C,
            LoopSoftPointsExt = 0x2015,
        }

        public void BindDataBuffer(byte[] dataBuffer, ALFormat format, int size, int sampleRate, int sampleAlignment = 0)
        {
            //if ((format == ALFormat.MonoMSAdpcm || format == ALFormat.StereoMSAdpcm) && !OpenALSoundController.Instance.SupportsAdpcm)
               // throw new InvalidOperationException("MS-ADPCM is not supported by this OpenAL driver");
           /// if ((format == ALFormat.MonoIma4 || format == ALFormat.StereoIma4) && !OpenALSoundController.Instance.SupportsIma4)
             //   throw new InvalidOperationException("IMA/ADPCM is not supported by this OpenAL driver");

            openALFormat = format;
            dataSize = size;
            int unpackedSize = 0;

            if (sampleAlignment > 0)
            {
              // AL.Bufferi(openALDataBuffer, ALBufferi.UnpackBlockAlignmentSoft, sampleAlignment);
               ALHelper.CheckError("Failed to fill buffer.");
            }

            AL.BufferData(openALDataBuffer, openALFormat, dataBuffer, size, sampleRate);
            ALHelper.CheckError("Failed to fill buffer.");

            int bits, channels;
            Duration = -1;
            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Bits, out bits);
            ALHelper.CheckError("Failed to get buffer bits");
            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Channels, out channels);
            ALHelper.CheckError("Failed to get buffer channels");
            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Size, out unpackedSize);
            ALHelper.CheckError("Failed to get buffer size");
            Duration = (float)(unpackedSize / ((bits / 8) * channels)) / (float)sampleRate;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Clean up managed objects
                }
                // Release unmanaged resources
                if (AL.IsBuffer(openALDataBuffer))
                {
                    ALHelper.CheckError("Failed to fetch buffer state.");
                    AL.DeleteBuffers(1, ref openALDataBuffer);
                    ALHelper.CheckError("Failed to delete buffer.");
                }

                _isDisposed = true;
            }
        }
    }
}