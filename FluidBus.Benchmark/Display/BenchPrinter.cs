using FluidBus.Benchmark.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FluidBus.Benchmark.Display
{
	public class BenchPrinter
	{
		public static void Print(BenchResult result)
		{
			string tag = "[BENCH] - ";
			Console.WriteLine($"{tag}Interation: {result.Iteration} - Warmup: {result.Warmup}");
			Console.WriteLine($"{tag}Case: {result.Case}");
			Console.WriteLine($"{tag}Start: {result.Start:HH:mm:ss:ffff} - End: {result.End:HH:mm:ss:ffff}");
			Console.WriteLine($"{tag}Duration: {result.Duration} ms");
			Console.WriteLine($"{tag}Average: {(result.Duration * 1_000_000) / result.Iteration:F0} ns/iteration");
		}
	}
}
