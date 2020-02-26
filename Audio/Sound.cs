using Microsoft.Xna.Framework.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading.Tasks;

namespace P3.AudioLib
{
    public class Sound : IDisposable
    {

        public SoundState state = SoundState.Stopped;

        public bool Looping { get; set; } = false;
        public bool IsPlaying { get { return state == SoundState.Playing; } }
        public bool IsDisposed { get; set; } = false;
        public bool IsComplete { get { return IsPlaying && !Looping && CurrentTime > Duration.Seconds; } }

        public float Volume
        {
            set
            {
                AL.Source(openALSourceId, ALSourcef.Gain, value);
                ALHelper.CheckError("Failed to set source volume.");
            }
        }

        public TimeSpan Duration { get; set; }
        public int openALSourceId;

        public float CurrentTime
        {
            get
            {
                AL.GetSource(openALSourceId, ALSourcef.SecOffset, out float result);
                return result;
            }
        }

        public Action OnComplete { get; set; }

        public AudioData data;

        public Sound(AudioData data)
        {
            this.data = data;

            //get duration
            Duration = GetSampleDuration(data.buffer.Length, data.frequency, data.channels);
        }

        public void Play()
        {
            openALSourceId = 0;
            openALSourceId = Loader.ReserveSource();

            AL.GetError();//clear errors

            AL.Source(openALSourceId, ALSourcei.Buffer, data.soundBuffer.OpenALDataBuffer);
            ALHelper.CheckError("Failed to bind buffer to source.");
          
            // Volume
            AL.Source(openALSourceId, ALSourcef.Gain, Audio.MasterVolume);
            ALHelper.CheckError("Failed to set source volume.");

            // Looping
            AL.Source(openALSourceId, ALSourceb.Looping, Looping);
            ALHelper.CheckError("Failed to set source loop state.");

            AL.SourcePlay(openALSourceId);
            ALHelper.CheckError("Failed to play source.");
            
            state = SoundState.Playing;
        }

        public void Stop()
        {
            state = SoundState.Stopped;
        }

        public void Resume()
        {
            //play source
            AL.SourcePlay(openALSourceId);
            state = SoundState.Playing;
        }

        public void Pause()
        {
            AL.SourcePause(openALSourceId);
            state = SoundState.Paused;
        }

        public void SoundComplete()
        {
            OnComplete?.Invoke();
            state = SoundState.Stopped;
            System.Diagnostics.Debug.WriteLine("sound complete : " + data.fileName);
        }

        // Get sample duration method taken from MonoGame source

        // MonoGame - Copyright (C) The MonoGame Team
        // This file is subject to the terms and conditions defined in
        // file 'LICENSE.txt', which is part of this source code package.

        /// <summary>
        /// Returns the duration for 16-bit PCM audio.
        /// </summary>
        /// <param name="sizeInBytes">The length of the audio data in bytes.</param>
        /// <param name="sampleRate">Sample rate, in Hertz (Hz). Must be between 8000 Hz and 48000 Hz</param>
        /// <param name="channels">Number of channels in the audio data.</param>
        /// <returns>The duration of the audio data.</returns>
        private static TimeSpan GetSampleDuration(int sizeInBytes, int sampleRate, int channels)
        {
            //from monogame sound effect class
            if (sizeInBytes < 0)
                throw new ArgumentException("Buffer size cannot be negative.", "sizeInBytes");
            if (sampleRate < 8000 || sampleRate > 48000)
                throw new ArgumentOutOfRangeException("sampleRate");

            var numChannels = (int)channels;
            if (numChannels != 1 && numChannels != 2)
                throw new ArgumentOutOfRangeException("channels");

            if (sizeInBytes == 0)
                return TimeSpan.Zero;

            // Reference
            // http://tinyurl.com/hq9slfy

            var dur = sizeInBytes / (sampleRate * numChannels * 16f / 8f);

            var duration = TimeSpan.FromSeconds(dur);

            return duration;
        }

        private void FreeSource()
        {

            AL.SourceStop(openALSourceId);
            ALHelper.CheckError("Failed to stop source.");

            AL.Source(openALSourceId, ALSourcei.Buffer, 0);
            ALHelper.CheckError("Failed to free source from buffer.");

            Loader.FreeSource(this);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                }

                /*
                if (data.soundBuffer != null)
                {
                    data.soundBuffer.Dispose();
                    data.soundBuffer = null;
                }
                */

                FreeSource();
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
                IsDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Sound() {
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