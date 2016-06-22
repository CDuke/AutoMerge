using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using AutoMerge.Configuration;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using IServiceProvider = System.IServiceProvider;

namespace AutoMerge
{
    [Export]
    public class Settings
    {
        private MergeMode? _lastMergeOperation;
        private readonly string[] _mergeOperationDefaultValues;
        private readonly WritableSettingsStore _vsSettingsProvider;
	    private BranchNameMatch[] _aliases;

	    private const string collectionKey = "AutoMerge";

        private const string lastMergeOperationKey = "last_operation";
        private const string mergeModeMerge = "merge";
        private const string mergeModeMergeAndCheckin = "merge_checkin";

        private const string mergeOperationDefaultKey = "merge_operation_default";
        private const string mergeOperationDefaultLast = "last";
        private const string mergeOperationDefaultMerge = mergeModeMerge;
        private const string mergeOperationDefaultMergeCheckin = mergeModeMergeAndCheckin;

        private const string commentFormatKey = "comment_format";
        private const string commentFormatDefault = "MERGE {FromOriginalToTarget} ({OriginalComment})";
        private const string commentFormatDiscardKey = "comment_format_discard";
        private const string commentFormatDiscardDefault = "DISCARD {" + commentFormatKey + "}";
        private const string branchDelimiterKey = "branch_delimiter";
        private const string branchDelimiterDefault = " -> ";
        private const string branchNameMatchesKey = "branch_overrides";

        private static WritableSettingsStore GetWritableSettingsStore(IServiceProvider vsServiceProvider)
        {
            var shellSettingsManager = new ShellSettingsManager(vsServiceProvider);
            return shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        [ImportingConstructor]
        public Settings(SVsServiceProvider serviceProvider)
        {
            _vsSettingsProvider = GetWritableSettingsStore(serviceProvider);

            if (!_vsSettingsProvider.CollectionExists(collectionKey))
            {
                _vsSettingsProvider.CreateCollection(collectionKey);
            }

            _mergeOperationDefaultValues = new[] { mergeOperationDefaultLast, mergeOperationDefaultMerge, mergeOperationDefaultMergeCheckin };

            BranchNameMatches = BranchNameMatches ?? new BranchNameMatch[0];
        }

        public MergeMode LastMergeOperation
        {
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
            get
            {
                return new CommentFormat
                    {
                        Format = _vsSettingsProvider.GetString(collectionKey, commentFormatKey, commentFormatDefault),
                        BranchDelimiter = _vsSettingsProvider.GetString(collectionKey, branchDelimiterKey, branchDelimiterDefault),
                        DiscardFormat = _vsSettingsProvider.GetString(collectionKey, commentFormatDiscardKey, commentFormatDiscardDefault)
                    };
            }
        }

        public BranchNameMatch[] BranchNameMatches
        {
            get
            {
                if (_aliases != null)
                    return _aliases;

                var json = _vsSettingsProvider.GetString(collectionKey, branchNameMatchesKey, "");
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var serializer = new JavaScriptSerializer();
                var result = serializer.Deserialize<BranchNameMatch[]>(json);
                _aliases = result;
                return result;
            }
            set
            {
                _aliases = value;
                var serializer = new JavaScriptSerializer();
                var result = serializer.Serialize(_aliases);
                _vsSettingsProvider.SetString(collectionKey, branchNameMatchesKey, result);
            }
        }

        private MergeMode LastMergeOperationGet()
        {
            MergeMode result;
            var mergeOperationDefaultValue = MergeOperationDefaultGet();
            if (mergeOperationDefaultValue == mergeOperationDefaultLast)
            {
                if (!_lastMergeOperation.HasValue)
                {
                    _lastMergeOperation =
                        ToMergeMode(_vsSettingsProvider.GetString(collectionKey, lastMergeOperationKey,
                            mergeModeMergeAndCheckin));
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
                _vsSettingsProvider.SetString(collectionKey, lastMergeOperationKey, stringValue);
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
            var mergeOperationDefaultValue = _vsSettingsProvider.GetString(collectionKey, mergeOperationDefaultKey, mergeOperationDefaultLast);

            if (!_mergeOperationDefaultValues.Contains(mergeOperationDefaultValue))
                mergeOperationDefaultValue = mergeOperationDefaultLast;

            _vsSettingsProvider.SetString(collectionKey, mergeOperationDefaultKey, mergeOperationDefaultValue);

            return mergeOperationDefaultValue;
        }
    }
}

