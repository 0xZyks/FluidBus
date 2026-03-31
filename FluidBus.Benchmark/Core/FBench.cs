using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FluidBus.Benchmark.Core
{
	public delegate void BenchScenario();

	public class FBench
    {
		public static BenchResult Measure(string useCase, int iterations, int warmup, BenchScenario callback)
		{
			for (int i = 0; i < warmup; i++)
				callback();

			Console.WriteLine($"{'-' * 40}");
			Console.WriteLine($"Warmup End");
			Console.WriteLine($"{'-' * 40}");
			DateTime start = DateTime.Now;
			long startTick = Stopwatch.GetTimestamp();

			for (int i = 0; i < iterations; i++)
				callback();

			long endTick = Stopwatch.GetTimestamp();
			DateTime end = DateTime.Now;

			double duration = Stopwatch.GetElapsedTime(startTick, endTick).TotalNanoseconds;

			return new BenchResult(iterations, warmup, useCase, start, end, duration);
		}
	}
}
