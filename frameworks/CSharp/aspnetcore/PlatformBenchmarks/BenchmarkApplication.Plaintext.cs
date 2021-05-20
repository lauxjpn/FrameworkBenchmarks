// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace PlatformBenchmarks
{
    public partial class BenchmarkApplication
    {
        private readonly static AsciiString _plaintextPreamble =
            _http11OK +
            _headerServer + _crlf +
            _headerContentTypeText + _crlf +
            _headerContentLength + _plainTextBody.Length.ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PlainText(ref BufferWriter<WriterAdapter> writer)
        {
            writer.Write(_plaintextPreamble);

            // Date header
            writer.Write(DateHeader.HeaderBytes);

            // Body
            writer.Write(_plainTextBody);
        }
    }
}
