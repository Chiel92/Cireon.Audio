﻿using System;
using System.Collections.Generic;
using NVorbis;
using NVorbis.OpenTKSupport;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace Cireon.Audio
{
    /// <summary>
    /// Main audio manager.
    /// Keeps track of several sub-managers and provides global access to audio-related objects.
    /// </summary>
    public sealed class AudioManager
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static AudioManager Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        /// <param name="sfxPath">Path containing all soundeffects that should be pre-buffered.</param>
        public static void Initialize(string sfxPath)
        {
            AudioManager.Instance = new AudioManager(sfxPath);
        }

        /// <summary>
        /// Disposes the singleton instance.
        /// Includes clearing all buffers and releasing all OpenAL resources.
        /// </summary>
        public static void Dispose()
        {
            AudioManager.Instance.dispose();
        }

        /// <summary>
        /// The manager that keeps track of all OpenAL Sources.
        /// </summary>
        public readonly SourceManager SourceManager;
        /// <summary>
        /// Collection of all soundeffects.
        /// </summary>
        public readonly SoundLibrary Sounds;

        private readonly AudioContext context;
        private readonly OggStreamer streamer;

        private BackgroundMusic currentBGM;

        private float masterVolume, musicVolume, effectsVolume;

        /// <summary>
        /// The master volume that is applied to both the music and soundeffects.
        /// </summary>
        public float MasterVolume
        {
            get { return this.masterVolume; }
            set
            {
                this.masterVolume = value;
                this.onMusicVolumeChanged();
                this.onEffectsVolumeChanged();
            }
        }
        /// <summary>
        /// The volume that is used for playing the music.
        /// </summary>
        public float MusicVolume
        {
            get { return this.musicVolume; }
            set
            {
                this.musicVolume = value;
                this.onMusicVolumeChanged();
            }
        }
        /// <summary>
        /// The volume that is used for playing the sound effects.
        /// </summary>
        public float EffectsVolume
        {
            get { return this.effectsVolume; }
            set
            {
                this.effectsVolume = value;
                this.onEffectsVolumeChanged();
            }
        }

        private AudioManager(string sfxPath)
        {
            this.context = new AudioContext();
            this.streamer = new OggStreamer();

            this.SourceManager = new SourceManager();
            this.Sounds = new SoundLibrary(sfxPath);

            this.masterVolume = 1;
            this.musicVolume = 1;
            this.effectsVolume = 1;
        }

        /// <summary>
        /// Updates all audio-related resources.
        /// </summary>
        /// <param name="elapsedTimeS">The elapsed time in seconds since the last update.</param>
        public void Update(float elapsedTimeS)
        {
            this.SourceManager.Update();
        }

        #region Volumes
        private void onMusicVolumeChanged()
        {
            if (this.currentBGM != null)
                this.currentBGM.Volume = this.masterVolume * this.musicVolume;
        }

        private void onEffectsVolumeChanged()
        {

        }
        #endregion

        /// <summary>
        /// Changes the background music to the specified file.
        /// </summary>
        /// <param name="file">The file containing the new background music (ogg-file).</param>
        public void SetBGM(string file)
        {
            if (this.currentBGM != null)
                this.currentBGM.Dispose();

            this.currentBGM = new BackgroundMusic(file);
            this.currentBGM.Volume = this.masterVolume * this.musicVolume;
            this.currentBGM.Play();
        }

        private void dispose()
        {
            if (this.currentBGM != null)
                this.currentBGM.Dispose();

            this.SourceManager.Dispose();
            this.streamer.Dispose();
            this.context.Dispose();
        }
    }
}
