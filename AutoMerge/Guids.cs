// Guids.cs
// MUST match guids.h
using System;

namespace AutoMerge
{
    static class GuidList
    {
        public const string guidAutoMergePkgString = "f05bac3e-6794-4a9e-9ee7-1b8a200778ee";
        public const string guidAutoMergeCmdSetString = "550e8690-9fae-46d1-8ff7-d6d0edf9449c";

        public static readonly Guid guidAutoMergeCmdSet = new Guid(guidAutoMergeCmdSetString);
    };
}