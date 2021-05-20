// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Runtime.CompilerServices;

namespace PlatformBenchmarks
{
    public readonly struct Fortune : IComparable<Fortune>, IComparable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fortune(int id, string message)
        {
            Id = id;
            Message = message;
        }

        public int Id { get; }

        public string Message { get; }

        public int CompareTo(object obj) => throw new InvalidOperationException("The non-generic CompareTo should not be used");

        // Performance critical, using culture insensitive comparison
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Fortune other) => string.CompareOrdinal(Message, other.Message);
    }
}
