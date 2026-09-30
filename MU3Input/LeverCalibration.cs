using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MU3Input
{
    // Maps the raw lever ADC value to the signed position segatools expects
    // (negative = left). The raw range is taken from MU3Input.lever.ini next to
    // the DLL; without a saved range it follows the min/max seen so far.
    public class LeverCalibration
    {
        public int Min = -1;
        public int Max = -1;
        public bool Invert;
        public int Range = 20000;

        public volatile bool Calibrating;

        private readonly object _lock = new object();
        private int _raw;
        private int _seenMin = int.MaxValue;
        private int _seenMax = int.MinValue;

        public int Raw => _raw;
        public int SeenMin => _seenMin;
        public int SeenMax => _seenMax;

        public bool HasSaved => Min >= 0 && Max - Min >= 8;

        public void Observe(int raw)
        {
            lock (_lock)
            {
                _raw = raw;
                if (raw < _seenMin) _seenMin = raw;
                if (raw > _seenMax) _seenMax = raw;
            }
        }

        public void ResetSeen()
        {
            lock (_lock)
            {
                _seenMin = int.MaxValue;
                _seenMax = int.MinValue;
            }
        }

        public short Map(int raw)
        {
            int lo, hi;
            lock (_lock)
            {
                if (HasSaved && !Calibrating)
                {
                    lo = Min;
                    hi = Max;
                }
                else
                {
                    lo = _seenMin;
                    hi = _seenMax;
                }
            }

            if (hi - lo < 8)
                return 0;

            var t = (raw - lo) / (double) (hi - lo);
            t = Math.Max(0.0, Math.Min(1.0, t));

            var pos = (t * 2.0 - 1.0) * Range;
            if (Invert)
                pos = -pos;

            return (short) Math.Round(pos);
        }

        // Stores the range swept since the last ResetSeen().
        public bool SaveSeen()
        {
            lock (_lock)
            {
                if (_seenMax - _seenMin < 8)
                    return false;

                Min = _seenMin;
                Max = _seenMax;
            }

            return Save();
        }

        private static string FilePath
        {
            get
            {
                var dir = Path.GetDirectoryName(typeof(LeverCalibration).Assembly.Location);
                return Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "MU3Input.lever.ini");
            }
        }

        public static LeverCalibration Load()
        {
            var cal = new LeverCalibration();

            try
            {
                if (!File.Exists(FilePath))
                    return cal;

                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                        continue;

                    var key = parts[0].Trim().ToLowerInvariant();
                    int value;
                    if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        continue;

                    switch (key)
                    {
                        case "min": cal.Min = value; break;
                        case "max": cal.Max = value; break;
                        case "invert": cal.Invert = value != 0; break;
                        case "range": cal.Range = Math.Max(1000, Math.Min(30000, value)); break;
                    }
                }
            }
            catch
            {
                // fall back to auto range
            }

            return cal;
        }

        public bool Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("min=" + Min.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("max=" + Max.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("invert=" + (Invert ? "1" : "0"));
                sb.AppendLine("range=" + Range.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(FilePath, sb.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
