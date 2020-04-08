﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace StatsdClient
{
    #pragma warning disable CS1591
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "See ObsoleteAttribute.")]
    [ObsoleteAttribute("This class will become private in a future release.")]
    public class MetricsTimer : IDisposable
    {
        private readonly string _name;
        private readonly DogStatsdService _dogStatsd;
        private readonly Stopwatch _stopWatch;
        private readonly double _sampleRate;
        private bool _disposed;

        public MetricsTimer(string name, double sampleRate = 1.0, string[] tags = null)
            : this(null, name, sampleRate, tags)
        {
        }

        public MetricsTimer(DogStatsdService dogStatsd, string name, double sampleRate = 1.0, string[] tags = null)
        {
            _name = name;
            _dogStatsd = dogStatsd;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _sampleRate = sampleRate;
            Tags = new List<string>();
            if (tags != null)
            {
                Tags.AddRange(tags);
            }
        }

        public List<string> Tags { get; set; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _stopWatch.Stop();

                if (_dogStatsd == null)
                {
                    DogStatsd.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, Tags.ToArray());
                }
                else
                {
                    _dogStatsd.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, Tags.ToArray());
                }
            }
        }
    }
}