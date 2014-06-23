using System;

namespace AutoMerge
{
    internal class Settings
    {
        private readonly ISettingProvider _settingProvider;
        private static readonly Lazy<Settings> _instance;

        private MergeMode? _lastMergeOperation;

        private const string lastMergeOperationKey = "last_operation";
        private const string mergeModeMerge = "merge";
        private const string mergeModeMergeAndCheckin = "merge_checkin";

        static Settings()
        {
            _instance = new Lazy<Settings>(() => new Settings());
        }

        private Settings()
        {
            _settingProvider = new FileSettingProvider();
        }

        public static Settings Instance
        {
            get { return _instance.Value; }
        }

        public MergeMode LastMergeOperation {
            get
            {
                return LastMergeOperationGet();
            }
            set
            {
                LastMergeOperationSet(value);
            } }

        private MergeMode LastMergeOperationGet()
        {
            if (!_lastMergeOperation.HasValue)
            {
                string stringValue;
                if (!_settingProvider.TryReadValue(lastMergeOperationKey, out stringValue))
                {
                    stringValue = mergeModeMergeAndCheckin;
                }

                _lastMergeOperation = ToMergeMode(stringValue);
            }

            return _lastMergeOperation.Value;
        }

        private void LastMergeOperationSet(MergeMode mergeMode)
        {
            if (_lastMergeOperation != mergeMode)
            {
                var stringValue = ToString(mergeMode);
                _settingProvider.WriteValue(lastMergeOperationKey, stringValue);
            }
        }

        private static MergeMode ToMergeMode(string stringValue)
        {
            if (stringValue == mergeModeMergeAndCheckin)
                return MergeMode.MergeAndCheckIn;
            return MergeMode.Merge;
        }

        private static string ToString(MergeMode mergeMode)
        {
            switch (mergeMode)
            {
                case MergeMode.Merge:
                    return mergeModeMerge;
                case MergeMode.MergeAndCheckIn:
                    return mergeModeMergeAndCheckin;
                default:
                    return "unknown";
            }
        }
    }
}

