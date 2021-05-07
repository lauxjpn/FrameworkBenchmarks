// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Linq;

namespace PlatformBenchmarks
{
    internal class BatchUpdateString
    {
        private const int MaxBatch = 500;

        public static DatabaseServer DatabaseServer;

        internal static readonly string[] Ids = Enumerable.Range(0, MaxBatch).Select(i => $"@Id_{i}").ToArray();
        internal static readonly string[] Randoms = Enumerable.Range(0, MaxBatch).Select(i => $"@Random_{i}").ToArray();

        private static string[] _queries = new string[MaxBatch + 1];

        public static string Query(int batchSize)
        {
            if (_queries[batchSize] != null)
            {
                return _queries[batchSize];
            }

            var lastIndex = batchSize - 1;

            var sb = StringBuilderCache.Acquire();

            if (DatabaseServer == DatabaseServer.PostgreSql)
            {
                sb.Append("UPDATE world SET randomNumber = temp.randomNumber FROM (VALUES ");
                Enumerable.Range(0, lastIndex).ToList().ForEach(i => sb.Append($"(@Id_{i}, @Random_{i}), "));
                sb.Append($"(@Id_{lastIndex}, @Random_{lastIndex}) ORDER BY 1) AS temp(id, randomNumber) WHERE temp.id = world.id");
            }
            else if (DatabaseServer == DatabaseServer.MySql)
            {
                // sb.Append("INSERT INTO world (id, randomNumber) VALUES ");
                // Enumerable.Range(0, lastIndex).ToList().ForEach(i => sb.Append($"(@Id_{i}, @Random_{i}), "));
                // sb.Append($"(@Id_{lastIndex}, @Random_{lastIndex}) ON DUPLICATE KEY UPDATE randomNumber = VALUES (randomNumber)");

                sb.Append("UPDATE world SET randomNumber = CASE id ");
                Enumerable.Range(0, lastIndex).ToList().ForEach(i => sb.Append($"WHEN @Id_{i} THEN @Random_{i} "));
                sb.Append($"WHEN @Id_{lastIndex} THEN @Random_{lastIndex} END WHERE id IN (");
                Enumerable.Range(0, lastIndex).ToList().ForEach(i => sb.Append($"@Id_{i}, "));
                sb.Append($"@Id_{lastIndex})");

                // INSERT INTO table (id,Col1,Col2) VALUES (1,1,1),(2,2,3),(3,9,3),(4,10,12) ON DUPLICATE KEY UPDATE Col1=VALUES(Col1),Col2=VALUES(Col2);
            }
            else
            {
                Enumerable.Range(0, batchSize).ToList().ForEach(i => sb.Append($"UPDATE world SET randomnumber = @Random_{i} WHERE id = @Id_{i};"));
            }

            return _queries[batchSize] = StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}
