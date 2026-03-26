using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace MuteMaster.Core
{
    public class AudioDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
        public override string ToString() => Name;
    }

    public static class AudioManager
    {
        private static readonly MMDeviceEnumerator _enumerator = new();
        private static Timer? _levelTimer;
        private static WasapiCapture? _capture;
        private static float _peakLevel;
        private static bool _capturing;

        public static event Action<float>? MicLevelChanged;
        public static event Action<bool>?  MicMuteChanged;
        public static event Action<bool>?  OutputMuteChanged;

        // ── Device lists ───────────────────────────────────────────────

        public static List<AudioDevice> GetInputDevices()
        {
            var list = new List<AudioDevice>();
            try
            {
                // Get actual default device name to show in brackets
                string defaultName = "Windows Default";
                try
                {
                    var def = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                    defaultName = $"Windows Default ({def.FriendlyName})";
                }
                catch { }

                list.Add(new AudioDevice { Id = "", Name = defaultName, IsDefault = true });

                foreach (var d in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                    list.Add(new AudioDevice { Id = d.ID, Name = d.FriendlyName });
            }
            catch { }
            return list;
        }

        public static List<AudioDevice> GetOutputDevices()
        {
            var list = new List<AudioDevice>();
            try
            {
                string defaultName = "Windows Default";
                try
                {
                    var def = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    defaultName = $"Windows Default ({def.FriendlyName})";
                }
                catch { }

                list.Add(new AudioDevice { Id = "", Name = defaultName, IsDefault = true });

                foreach (var d in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                    list.Add(new AudioDevice { Id = d.ID, Name = d.FriendlyName });
            }
            catch { }
            return list;
        }

        // ── Device resolution ──────────────────────────────────────────

        private static MMDevice? GetInput(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var col = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    var m = col.FirstOrDefault(d => d.ID == id);
                    if (m != null) return m;
                }
                return _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            }
            catch { return null; }
        }

        private static MMDevice? GetOutput(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var col = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    var m = col.FirstOrDefault(d => d.ID == id);
                    if (m != null) return m;
                }
                return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch { return null; }
        }

        // ── Mic mute ───────────────────────────────────────────────────

        public static bool IsMicMuted(string id = "")
        {
            try { return GetInput(id)?.AudioEndpointVolume.Mute ?? false; }
            catch { return false; }
        }

        public static void SetMicMute(bool mute, string id = "")
        {
            try
            {
                var dev = GetInput(id);
                if (dev == null) return;
                dev.AudioEndpointVolume.Mute = mute;
                MicMuteChanged?.Invoke(mute);
                if (SettingsManager.Current.ToggleSoundEnabled)
                    Task.Run(() => PlaySound(mute));
            }
            catch { }
        }

        public static void SetMicMuteNoSound(bool mute, string id = "")
        {
            try
            {
                var dev = GetInput(id);
                if (dev == null) return;
                dev.AudioEndpointVolume.Mute = mute;
                MicMuteChanged?.Invoke(mute);
            }
            catch { }
        }

        public static void ToggleMicMute(string id = "")
            => SetMicMute(!IsMicMuted(id), id);

        // ── Output mute ────────────────────────────────────────────────

        public static bool IsOutputMuted(string id = "")
        {
            try { return GetOutput(id)?.AudioEndpointVolume.Mute ?? false; }
            catch { return false; }
        }

        public static void SetOutputMute(bool mute, string id = "")
        {
            try
            {
                var dev = GetOutput(id);
                if (dev == null) return;
                dev.AudioEndpointVolume.Mute = mute;
                OutputMuteChanged?.Invoke(mute);
            }
            catch { }
        }

        public static void ToggleOutputMute(string id = "")
            => SetOutputMute(!IsOutputMuted(id), id);

        // ── Level monitoring via WasapiCapture ─────────────────────────
        // Uses actual captured audio samples to compute RMS peak level.
        // This avoids the InvalidCastException from AudioMeterInformation.

        public static void StartLevelMonitoring(string id = "")
        {
            StopLevelMonitoring();
            try
            {
                MMDevice? device = GetInput(id);
                if (device == null) return;

                _capture = new WasapiCapture(device);
                _peakLevel = 0f;
                _capturing = true;

                _capture.DataAvailable += (s, e) =>
                {
                    if (!_capturing || e.BytesRecorded == 0) return;
                    float peak = ComputePeak(e.Buffer, e.BytesRecorded, _capture.WaveFormat);
                    // Smooth with previous value
                    _peakLevel = Math.Max(peak, _peakLevel * 0.6f);
                };

                _capture.StartRecording();

                // Emit level on timer so UI updates at steady 20fps
                _levelTimer = new Timer(50);
                _levelTimer.AutoReset = true;
                _levelTimer.Elapsed += (s, e) =>
                {
                    MicLevelChanged?.Invoke(_peakLevel);
                    _peakLevel *= 0.85f; // decay
                };
                _levelTimer.Start();
            }
            catch { }
        }

        private static float ComputePeak(byte[] buffer, int bytesRecorded, WaveFormat fmt)
        {
            float peak = 0f;
            try
            {
                if (fmt.BitsPerSample == 32 && fmt.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    int samples = bytesRecorded / 4;
                    for (int i = 0; i < samples; i++)
                    {
                        float s = Math.Abs(BitConverter.ToSingle(buffer, i * 4));
                        if (s > peak) peak = s;
                    }
                }
                else if (fmt.BitsPerSample == 16)
                {
                    int samples = bytesRecorded / 2;
                    for (int i = 0; i < samples; i++)
                    {
                        float s = Math.Abs(BitConverter.ToInt16(buffer, i * 2) / 32768f);
                        if (s > peak) peak = s;
                    }
                }
            }
            catch { }
            return Math.Min(peak, 1f);
        }

        public static void StopLevelMonitoring()
        {
            _capturing = false;
            _levelTimer?.Stop();
            _levelTimer?.Dispose();
            _levelTimer = null;
            try { _capture?.StopRecording(); } catch { }
            try { _capture?.Dispose(); } catch { }
            _capture = null;
        }

        // ── Toggle sounds ──────────────────────────────────────────────

        private static void PlaySound(bool muting)
        {
            try
            {
                var s = SettingsManager.Current;

                // Try specific mute or unmute sound first
                string specific = muting ? s.CustomMuteSound : s.CustomUnmuteSound;
                if (!string.IsNullOrEmpty(specific) && File.Exists(specific))
                {
                    PlayFile(specific); return;
                }

                // Fall back to legacy single sound
                if (!string.IsNullOrEmpty(s.CustomSoundPath) && File.Exists(s.CustomSoundPath))
                {
                    PlayFile(s.CustomSoundPath); return;
                }

                // Built-in beep — higher pitch for mute, lower for unmute
                float freq = muting ? 880f : 660f;
                PlayBeep(freq);
            }
            catch { }
        }

        private static void PlayFile(string path)
        {
            try
            {
                using var reader = new AudioFileReader(path);
                using var output = new WaveOutEvent();
                output.Init(reader);
                output.Play();
                while (output.PlaybackState == PlaybackState.Playing)
                    Thread.Sleep(10);
            }
            catch { }
        }

        private static void PlayBeep(float freq = 880f)
        {
            try
            {
                var sine = new SineWaveProvider(freq, 0.12f, 44100, 1);
                var limited = new TakeSamplesProvider(sine, 80);
                using var waveOut = new WaveOutEvent();
                waveOut.Init(limited);
                waveOut.Play();
                Thread.Sleep(120);
            }
            catch { }
        }

        public static void Dispose()
        {
            StopLevelMonitoring();
            _enumerator.Dispose();
        }
    }

    internal class SineWaveProvider : WaveProvider32
    {
        private readonly float _freq, _amp;
        private float _phase;
        public SineWaveProvider(float freq, float amp, int rate, int ch) : base(rate, ch)
        { _freq = freq; _amp = amp; }
        public override int Read(float[] buf, int offset, int count)
        {
            float d = 2f * MathF.PI * _freq / WaveFormat.SampleRate;
            for (int i = 0; i < count; i++)
            {
                buf[offset + i] = _amp * MathF.Sin(_phase);
                _phase = (_phase + d) % (2f * MathF.PI);
            }
            return count;
        }
    }

    internal class TakeSamplesProvider : ISampleProvider
    {
        private readonly ISampleProvider _src;
        private readonly int _total;
        private int _read;
        public TakeSamplesProvider(ISampleProvider src, int ms)
        { _src = src; _total = (int)(src.WaveFormat.SampleRate * ms / 1000.0); }
        public WaveFormat WaveFormat => _src.WaveFormat;
        public int Read(float[] buf, int offset, int count)
        {
            int rem = _total - _read;
            if (rem <= 0) return 0;
            int n = _src.Read(buf, offset, Math.Min(count, rem));
            _read += n;
            return n;
        }
    }
}
