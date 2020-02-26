// Uses some code from MonoGame source, see the MonoGame license

// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Microsoft.Xna.Framework;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace P3.AudioLib
{
    public class Loader
    {
        private const int MAX_INSTANCES = 30;

        internal const int FormatPcm = 1;
        internal const int FormatMsAdpcm = 2;
        internal const int FormatIeee = 3;
        internal const int FormatIma4 = 17;

        protected static IDictionary<string, AudioData> audioData = new Dictionary<string, AudioData>();

        private static int[] allSourcesArray = new int[MAX_INSTANCES];
        private static List<int> availableSourcesCollection;
        private static List<int> inUseSourcesCollection;

        private static AudioContext context;

        private static void CheckInit()
        {
            if (context == null)
            {
                context = new AudioContext();
                AL.GenSources(allSourcesArray);
                availableSourcesCollection = new List<int>(allSourcesArray);
                inUseSourcesCollection = new List<int>();
            }
        }

        public static Sound Load(string uri, string ext = "wav")
        {
            string[] split = uri.Split('/');
            string name = split[split.Length - 1];

            //check if we already loaded this sound and return a new instance using same data
            if (Loader.audioData.ContainsKey(name))
                return new Sound(Loader.audioData[name]);

            CheckInit();

            var audioData = new AudioData();

            //todo - this should probably use string builder for speed
            using (Stream stream = TitleContainer.OpenStream(Path.Combine("Content", uri) + "." + ext))
            {
                //read in data from file
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // for now only support wav
                    audioData.buffer = LoadWave(
                     reader,
                     out audioData.format,
                     out audioData.frequency,
                     out audioData.channels,
                     out audioData.blockAlignment,
                     out audioData.bitsPerSample,
                     out audioData.samplesPerBlock,
                     out audioData.sampleCount);
                }
            }

            audioData.fileName = name;

            audioData.soundBuffer = new OALSoundBuffer();
            audioData.soundBuffer.BindDataBuffer(audioData.buffer, audioData.format, audioData.buffer.Length, audioData.frequency);

            //create sound instance
            var sound = new Sound(audioData);

            //store data in our dictionary
            Loader.audioData.Add(name, audioData);

            return sound;
        }

        /*
        private static byte[] LoadOGG(string uri, out ALFormat format, out int frequency, out int channels)
        {
           
            using (Stream stream = TitleContainer.OpenStream(uri + "." + "ogg"))
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    // now work with ms

                    using (var vorbis = new NVorbis.VorbisReader(ms, true))
                    {
                        // get the channels & sample rate
                        channels = vorbis.Channels;
                        frequency = vorbis.SampleRate;
                        format = GetSoundFormat(channels, 8);

                        // OPTIONALLY: get a TimeSpan indicating the total length of the Vorbis stream
                        var totalTime = vorbis.TotalTime;

                        // create a buffer for reading samples
                        var readBuffer = new float[channels * frequency / 5];  // 200ms

                        // get the initial position (obviously the start)
                        var position = TimeSpan.Zero;

                        // go grab samples
                        int cnt;
                        while ((cnt = vorbis.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
                        {
                            // do stuff with the buffer
                            // samples are interleaved (chan0, chan1, chan0, chan1, etc.)
                            // sample value range is -0.99999994f to 0.99999994f unless vorbis.ClipSamples == false

                            // OPTIONALLY: get the position we just read through to...
                            position = vorbis.DecodedTime;
                        }

                        // create a byte array and copy the floats into it...
                        var byteArray = new byte[readBuffer.Length * 4];
                        Buffer.BlockCopy(readBuffer, 0, byteArray, 0, byteArray.Length);

                        return byteArray;
                    }
                
                }
            }

        }*/

        private static byte[] LoadWave(BinaryReader reader, out ALFormat format, out int frequency, out int channels, out int blockAlignment, out int bitsPerSample, out int samplesPerBlock, out int sampleCount)
        {
            byte[] audioData = null;

            //header
            string signature = new string(reader.ReadChars(4));
            if (signature != "RIFF")
                throw new ArgumentException("Specified stream is not a wave file.");
            reader.ReadInt32(); // riff_chunk_size

            string wformat = new string(reader.ReadChars(4));
            if (wformat != "WAVE")
                throw new ArgumentException("Specified stream is not a wave file.");

            int audioFormat = 0;
            channels = 0;
            bitsPerSample = 0;
            format = ALFormat.Mono16;
            frequency = 0;
            blockAlignment = 0;
            samplesPerBlock = 0;
            sampleCount = 0;

            // WAVE header
            while (audioData == null)
            {
                string chunkType = new string(reader.ReadChars(4));
                int chunkSize = reader.ReadInt32();
                switch (chunkType)
                {
                    case "fmt ":
                        {
                            audioFormat = reader.ReadInt16(); // 2
                            channels = reader.ReadInt16(); // 4
                            frequency = reader.ReadInt32();  // 8
                            int byteRate = reader.ReadInt32();    // 12
                            blockAlignment = (int)reader.ReadInt16();  // 14
                            bitsPerSample = reader.ReadInt16(); // 16

                            // Read extra data if present
                            if (chunkSize > 16)
                            {
                                int extraDataSize = reader.ReadInt16();
                                if (audioFormat == FormatIma4)
                                {
                                    samplesPerBlock = reader.ReadInt16();
                                    extraDataSize -= 2;
                                }
                                if (extraDataSize > 0)
                                {
                                    if (reader.BaseStream.CanSeek)
                                        reader.BaseStream.Seek(extraDataSize, SeekOrigin.Current);
                                    else
                                    {
                                        for (int i = 0; i < extraDataSize; ++i)
                                            reader.ReadByte();
                                    }
                                }
                            }
                        }
                        break;
                    case "fact":
                        if (audioFormat == FormatIma4)
                        {
                            sampleCount = reader.ReadInt32() * channels;
                            chunkSize -= 4;
                        }
                        // Skip any remaining chunk data
                        if (chunkSize > 0)
                        {
                            if (reader.BaseStream.CanSeek)
                                reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                            else
                            {
                                for (int i = 0; i < chunkSize; ++i)
                                    reader.ReadByte();
                            }
                        }
                        break;
                    case "data":
                        audioData = reader.ReadBytes(chunkSize);
                        break;
                    default:
                        // Skip this chunk
                        if (reader.BaseStream.CanSeek)
                            reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                        else
                        {
                            for (int i = 0; i < chunkSize; ++i)
                                reader.ReadByte();
                        }
                        break;
                }
            }

            // Calculate fields we didn't read from the file
            format = GetSoundFormat(channels, bitsPerSample);

            if (samplesPerBlock == 0)
            {
                samplesPerBlock = SampleAlignment(format, blockAlignment);
            }

            if (sampleCount == 0)
            {
                switch (audioFormat)
                {
                    case FormatIma4:
                    case FormatMsAdpcm:
                        sampleCount = ((audioData.Length / blockAlignment) * samplesPerBlock) + SampleAlignment(format, audioData.Length % blockAlignment);
                        break;
                    case FormatPcm:
                    case FormatIeee:
                        sampleCount = audioData.Length / ((channels * bitsPerSample) / 8);
                        break;
                    default:
                        throw new InvalidDataException("Unhandled WAV format " + format.ToString());
                }
            }

            return audioData;
        }

        public static ALFormat GetSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1: return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                case 2: return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                default: throw new NotSupportedException("The specified sound format is not supported.");
            }
        }

        // Converts block alignment in bytes to sample alignment, primarily for compressed formats
        // Calculation of sample alignment from http://kcat.strangesoft.net/openal-extensions/SOFT_block_alignment.txt
        public static int SampleAlignment(ALFormat format, int blockAlignment)
        {
            switch (format)
            {
                case ALFormat.MonoIma4Ext:
                    return (blockAlignment - 4) / 4 * 8 + 1;
                case ALFormat.StereoIma4Ext:
                    return (blockAlignment / 2 - 4) / 4 * 8 + 1;
            }
            return 0;
        }

        /// <summary>
        /// Reserves a sound buffer and return its identifier. If there are no available sources
        /// or the controller was not able to setup the hardware then an
        /// <see cref="InstancePlayLimitException"/> is thrown.
        /// </summary>
        /// <returns>The source number of the reserved sound buffer.</returns>
        public static int ReserveSource()
        {
            int sourceNumber;

            lock (availableSourcesCollection)
            {
                if (availableSourcesCollection.Count == 0)
                {
                    //  throw new InstancePlayLimitException();
                    System.Diagnostics.Debug.WriteLine("no avail sources");
                    throw new Exception("no avail sources");
                }

                sourceNumber = availableSourcesCollection.Last();
                inUseSourcesCollection.Add(sourceNumber);
                availableSourcesCollection.Remove(sourceNumber);
            }

            return sourceNumber;
        }

        public static void RecycleSource(int sourceId)
        {
            lock (availableSourcesCollection)
            {
                inUseSourcesCollection.Remove(sourceId);
                availableSourcesCollection.Add(sourceId);
            }
        }

        public static void FreeSource(Sound inst)
        {
            RecycleSource(inst.openALSourceId);
            inst.openALSourceId = 0;
        }

        public static void Dispose()
        {
            // TODO : dispose of context
            ///Dispose
            /*
			if (context != ContextHandle.Zero)
            {
                Alc.MakeContextCurrent(ContextHandle.Zero);
                Alc.DestroyContext(context);
            }
            context = ContextHandle.Zero;
            */
        }
    }

    public struct AudioData
    {
        public byte[] buffer;
        public ALFormat format;
        public int frequency;
        public int channels;
        public int blockAlignment;
        public int bitsPerSample;
        public int samplesPerBlock;
        public int sampleCount;
        public string fileName;
        public OALSoundBuffer soundBuffer;
    }
}
