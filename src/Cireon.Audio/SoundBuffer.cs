﻿using System;
using System.Collections.Generic;
using System.IO;
using NVorbis;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;

namespace Cireon.Audio
{
    /// <summary>
    /// A wrapper class for a set of audiobuffers.
    /// </summary>
    public class SoundBuffer : IDisposable
    {
        /// <summary>
        /// Disposal state of this buffer.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// List of OpenAL buffer handles.
        /// </summary>
        public readonly int[] Handles;

        /// <summary>
        /// Generates a new sound buffer of the given size.
        /// </summary>
        /// <param name="amount">The amount of buffers to reserve.</param>
        public SoundBuffer(int amount)
        {
            this.Handles = AL.GenBuffers(amount);
            ALHelper.Check();
        }

        /// <summary>
        /// Generates a news sound buffer and fills it.
        /// </summary>
        /// <param name="buffers">The content of the buffers.</param>
        /// <param name="format">The format the buffers are in.</param>
        /// <param name="sampleRate">The samplerate of the buffers.</param>
        public SoundBuffer(IList<short[]> buffers, ALFormat format, int sampleRate)
            : this(buffers.Count)
        {
            this.FillBuffer(buffers, format, sampleRate);
        }

        /// <summary>
        /// Fills the buffer with new data.
        /// </summary>
        /// <param name="index">The starting index from where to fill the buffer.</param>
        /// <param name="data">The new content of the buffers.</param>
        /// <param name="format">The format the buffers are in.</param>
        /// <param name="sampleRate">The samplerate of the buffers.</param>
        public void FillBuffer(int index, short[] data, ALFormat format, int sampleRate)
        {
            if (index < 0 || index >= this.Handles.Length)
                throw new ArgumentOutOfRangeException("index");

            AL.BufferData(this.Handles[index], format, data, data.Length * sizeof(short), sampleRate);
            ALHelper.Check();
        }

        /// <summary>
        /// Fills the buffer with new data.
        /// </summary>
        /// <param name="data">The new content of the buffers.</param>
        /// <param name="format">The format the buffers are in.</param>
        /// <param name="sampleRate">The samplerate of the buffers.</param>
        public void FillBuffer(IList<short[]> data, ALFormat format, int sampleRate)
        {
            this.FillBuffer(0, data, format, sampleRate);
        }

        /// <summary>
        /// Fills the buffer with new data.
        /// </summary>
        /// <param name="index">The starting index from where to fill the buffer.</param>
        /// <param name="data">The new content of the buffers.</param>
        /// <param name="format">The format the buffers are in.</param>
        /// <param name="sampleRate">The samplerate of the buffers.</param>
        public void FillBuffer(int index, IList<short[]> data, ALFormat format, int sampleRate)
        {
            if (index < 0 || index >= this.Handles.Length)
                throw new ArgumentOutOfRangeException("index");
            if (data.Count > this.Handles.Length)
                throw new ArgumentException("This data does not fit in the buffer.", "data");

            for (int i = 0; i < data.Count; i++)
                this.FillBuffer((index + i) % this.Handles.Length, data[i], format, sampleRate);
        }

        /// <summary>
        /// Disposes the buffer.
        /// </summary>
        public void Dispose()
        {
            if (this.Disposed)
                return;

            AL.DeleteBuffers(this.Handles);
            ALHelper.Check();

            this.Disposed = true;
        }

        /// <summary>
        /// Creates a new soundbuffer from a file.
        /// </summary>
        /// <param name="file">The file to load the data from.</param>
        /// <returns>A SoundBuffer object containing the data from the specified file.</returns>
        [Obsolete("This method is obsolete. Please use SoundBuffer.FromOgg() instead.")]
        public static SoundBuffer FromFile(string file)
        {
            return SoundBuffer.FromOgg(file);
        }

        /// <summary>
        /// Creates a new soundbuffer from an ogg-file.
        /// </summary>
        /// <param name="file">The file to load the data from.</param>
        /// <returns>A SoundBuffer object containing the data from the specified file.</returns>
        public static SoundBuffer FromOgg(string file)
        {
            return SoundBuffer.FromOgg(File.OpenRead(file));
        }

        /// <summary>
        /// Creates a new soundbuffer from a file.
        /// </summary>
        /// <param name="file">The file to load the data from.</param>
        /// <returns>A SoundBuffer object containing the data from the specified file.</returns>
        [Obsolete("This method is obsolete. Please use SoundBuffer.FromOgg() instead.")]
        public static SoundBuffer FromFile(Stream file)
        {
            return SoundBuffer.FromOgg(file);
        }

        /// <summary>
        /// Creates a new soundbuffer from an ogg-file.
        /// </summary>
        /// <param name="file">The file to load the data from.</param>
        /// <returns>A SoundBuffer object containing the data from the specified file.</returns>
        public static SoundBuffer FromOgg(Stream file)
        {
            var buffers = new List<short[]>();

            ALFormat format;
            int sampleRate;

            using (var vorbis = new VorbisReader(file, true))
            {
                // Save format and samplerate for playback
                format = vorbis.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;
                sampleRate = vorbis.SampleRate;

                var buffer = new float[16384];
                int count;

                while ((count = vorbis.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    // Sample value range is -0.99999994f to 0.99999994f
                    // Samples are interleaved (chan0, chan1, chan0, chan1, etc.)

                    // Use the OggStreamer method to cast to the right format
                    var castBuffer = new short[count];
                    SoundBuffer.CastBuffer(buffer, castBuffer, count);
                    buffers.Add(castBuffer);
                }
            }

            return new SoundBuffer(buffers, format, sampleRate);
        }

        /// <summary>
        /// Creates a new soundbuffer from an uncompressed wave-file.
        /// </summary>
        /// <param name="file">The file to load the data from.</param>
        /// <returns>A SoundBuffer object containing the data from the specified file.</returns>
        public static SoundBuffer FromWav(string file)
        {
            return SoundBuffer.FromWav(File.OpenRead(file));
        }

        /// <summary>
        /// Creates a new soundbuffer from an uncompressed wave-file.
        /// </summary>
        /// <param name="file">The file to load the data from.</param>
        /// <returns>A SoundBuffer object containing the data from the specified file.</returns>
        public static SoundBuffer FromWav(Stream file)
        {
            using (var reader = new BinaryReader(file))
            {
                // RIFF header
                var signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                int riffChunckSize = reader.ReadInt32();

                var format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                // WAVE header
                var formatSignature = new string(reader.ReadChars(4));
                if (formatSignature != "fmt ")
                    throw new NotSupportedException("Specified wave file is not supported.");

                int formatChunkSize = reader.ReadInt32();
                int audioFormat = reader.ReadInt16();
                int numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                int blockAlign = reader.ReadInt16();
                int bitsPerSample = reader.ReadInt16();

                if (formatChunkSize > 16)
                    reader.ReadBytes(formatChunkSize - 16);

                var dataSignature = new string(reader.ReadChars(4));

                if (dataSignature != "data")
                    throw new NotSupportedException("Only uncompressed wave files are supported.");

                int dataChunkSize = reader.ReadInt32();

                var alFormat = SoundBuffer.getSoundFormat(numChannels, bitsPerSample);

                var data = reader.ReadBytes((int)reader.BaseStream.Length);
                var buffers = new List<short[]>();
                int count;
                int i = 0;
                const int bufferSize = 16384;

                while ((count = (Math.Min(data.Length, (i + 1) * bufferSize * 2) - i * bufferSize * 2) / 2) > 0)
                {
                    var buffer = new short[bufferSize];
                    SoundBuffer.convertBuffer(data, buffer, count, i * bufferSize * 2);
                    buffers.Add(buffer);
                    i++;
                }

                return new SoundBuffer(buffers, alFormat, sampleRate);
            }
        }

        /// <summary>
        /// Casts the buffer read by the vorbis reader to an Int16 buffer.
        /// </summary>
        /// <param name="inBuffer">The buffer as read by the vorbis reader.</param>
        /// <param name="outBuffer">A reference to the output buffer.</param>
        /// <param name="length">The length of the buffer.</param>
        public static void CastBuffer(float[] inBuffer, short[] outBuffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                var temp = (int)(32767f * inBuffer[i]);
                if (temp > Int16.MaxValue) temp = Int16.MaxValue;
                else if (temp < Int16.MinValue) temp = Int16.MinValue;
                outBuffer[i] = (short) temp;
            }
        }

        private static void convertBuffer(byte[] inBuffer, short[] outBuffer, int length, int inOffset = 0)
        {
            for (int i = 0; i < length; i++)
                outBuffer[i] = BitConverter.ToInt16(inBuffer, inOffset + 2 * i);
        }

        private static ALFormat getSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1: return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                case 2: return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                default: throw new NotSupportedException("The specified sound format is not supported.");
            }
        }

        /// <summary>
        /// Casts the buffer to an integer array.
        /// </summary>
        /// <param name="buffer">The buffer that should be casted.</param>
        /// <returns>The OpenAL handles of the buffers.</returns>
        static public implicit operator int[](SoundBuffer buffer)
        {
            return buffer.Handles;
        }
    }
}
