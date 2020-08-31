using System;
using StatsdClient.Aggregator;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;

namespace StatsdClient
{
    /// <summary>
    /// Route `Stats` instances.
    /// Route a metric of type <see cref="MetricType.Count"/>, <see cref="MetricType.Gauge"/>
    /// and <see cref="MetricType.Set"/> respectively to <see cref="CountAggregator"/>,
    /// <see cref="GaugeAggregator"/> and <see cref="SetAggregator"/>.
    /// Others stats are routed to <see cref="BufferBuilder"/>.
    /// </summary>
    internal class StatsRouter
    {
        private readonly Serializers _serializers;
        private readonly BufferBuilder _bufferBuilder;
        private readonly CountAggregator _optionalCountAggregator;
        private readonly GaugeAggregator _optionalGaugeAggregator;
        private readonly SetAggregator _optionalSetAggregator;

        private SerializedMetric _serializedMetric = new SerializedMetric();

        public StatsRouter(
            Serializers serializers,
            BufferBuilder bufferBuilder,
            Aggregators optionalAggregators)
        {
            _serializers = serializers;
            _bufferBuilder = bufferBuilder;
            if (optionalAggregators != null)
            {
                _optionalCountAggregator = optionalAggregators.OptionalCount;
                _optionalGaugeAggregator = optionalAggregators.OptionalGauge;
                _optionalSetAggregator = optionalAggregators.OptionalSet;
            }
        }

        public void Route(Stats stats)
        {
            switch (stats.Kind)
            {
                case StatsKind.Event:
                    this._serializers.EventSerializer.SerializeTo(ref stats.Event, _serializedMetric);
                    break;
                case StatsKind.Metric:
                    if (!RouteMetric(ref stats.Metric))
                    {
                        return;
                    }

                    break;
                case StatsKind.ServiceCheck:
                    this._serializers.ServiceCheckSerializer.SerializeTo(ref stats.ServiceCheck, _serializedMetric);
                    break;
                default:
                    throw new ArgumentException($"{stats.Kind} is not supported");
            }

            _bufferBuilder.Add(_serializedMetric);
        }

        public void OnIdle()
        {
            TryFlush(force: false);
        }

        public void Flush()
        {
            TryFlush(force: true);
        }

        private void TryFlush(bool force)
        {
            _bufferBuilder.HandleBufferAndReset();
            _optionalCountAggregator?.TryFlush(force);
            _optionalGaugeAggregator?.TryFlush(force);
            _optionalSetAggregator?.TryFlush(force);
        }

        private bool RouteMetric(ref StatsMetric metric)
        {
            switch (metric.MetricType)
            {
                case MetricType.Count:
                    if (_optionalCountAggregator != null)
                    {
                        _optionalCountAggregator.OnNewValue(ref metric);
                        return false;
                    }

                    break;
                case MetricType.Gauge:
                    if (_optionalGaugeAggregator != null)
                    {
                        _optionalGaugeAggregator.OnNewValue(ref metric);
                        return false;
                    }

                    break;
                case MetricType.Set:
                    if (_optionalSetAggregator != null)
                    {
                        _optionalSetAggregator.OnNewValue(ref metric);
                        return false;
                    }

                    break;
                default:
                    break;
            }

            this._serializers.MetricSerializer.SerializeTo(ref metric, _serializedMetric);
            return true;
        }
    }
}