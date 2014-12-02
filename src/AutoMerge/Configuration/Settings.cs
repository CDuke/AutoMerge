using System;
using System.Linq;

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

        private const string mergeOperationDefaultKey = "merge_operation_default";
        private const string mergeOperationDefaultLast = "last";
        private const string mergeOperationDefaultMerge = mergeModeMerge;
        private const string mergeOperationDefaultMergeCheckin = mergeModeMergeAndCheckin;
        private readonly string[] _mergeOperationDefaultValues;

        private const string commentFormatKey = "comment_format";
        private const string commentFormatDefault = "MERGE {FromOriginalToTarget} ({OriginalComment})";
        private const string commentFormatDiscardKey = "comment_format_discard";
        private const string commentFormatDiscardDefault = "DISCARD {" + commentFormatKey + "}";
        private const string branchDelimiterKey = "branch_delimiter";
        private const string branchDelimiterDefault = " -> ";

        static Settings()
        {
            _instance = new Lazy<Settings>(() => new Settings());
        }

        private Settings()
        {
            _settingProvider = new FileSettingProvider();
            _mergeOperationDefaultValues = new[]
                {mergeOperationDefaultLast, mergeOperationDefaultMerge, mergeOperationDefaultMergeCheckin};
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
            }
        }

        public CommentFormat CommentFormat
        {
            get { return CommentFormatGet(); }
        }

        private CommentFormat CommentFormatGet()
        {
            string commentFormat;
            if (!_settingProvider.TryReadValue(commentFormatKey, out commentFormat))
            {
                commentFormat = commentFormatDefault;
            }

            string commentFormatDiscard;
            if (!_settingProvider.TryReadValue(commentFormatDiscardKey, out commentFormatDiscard))
            {
                commentFormatDiscard = commentFormatDiscardDefault;
            }

            commentFormatDiscard = commentFormatDiscard.Replace("{" + commentFormatKey + "}", commentFormat);

            string branchDelimiter;
            if (!_settingProvider.TryReadValue(branchDelimiterKey, out branchDelimiter))
            {
                branchDelimiter = branchDelimiterDefault;
            }

            return new CommentFormat
            {
                Format = commentFormat,
                BranchDelimiter = branchDelimiter,
                DiscardFormat = commentFormatDiscard
            };

        }

        private MergeMode LastMergeOperationGet()
        {
            MergeMode result;
            var mergeOperationDefaultValue = MergeOperationDefaultGet();
            if (mergeOperationDefaultValue == mergeOperationDefaultLast)
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

                result = _lastMergeOperation.Value;
            }
            else
            {
                result = ToMergeMode(mergeOperationDefaultValue);
            }

            return result;
        }

        private void LastMergeOperationSet(MergeMode mergeMode)
        {
            if (_lastMergeOperation != mergeMode)
            {
                var stringValue = ToString(mergeMode);
                _settingProvider.WriteValue(lastMergeOperationKey, stringValue);
                _lastMergeOperation = mergeMode;
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

        private string MergeOperationDefaultGet()
        {
            string mergeOperationDefaultValue;
            if (!_settingProvider.TryReadValue(mergeOperationDefaultKey, out mergeOperationDefaultValue))
            {
                mergeOperationDefaultValue = mergeOperationDefaultLast;
                _settingProvider.WriteValue(mergeOperationDefaultKey, mergeOperationDefaultValue);
            }

            if (!_mergeOperationDefaultValues.Contains(mergeOperationDefaultValue))
                mergeOperationDefaultValue = "mergeOperationDefaultLast";

            return mergeOperationDefaultValue;
        }
    }
}

