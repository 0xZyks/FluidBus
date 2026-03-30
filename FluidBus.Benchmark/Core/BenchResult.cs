using FluidBus.Benchmark.Display;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Benchmark.Core
{
	public class BenchResult
	{
		public int Iteration { get; }
		public int Warmup { get; }
		public DateTime Start { get; }
		public DateTime End { get; }
		public double Duration { get; }
		public string Case { get; }

		public BenchResult(int iteration, int warmup, string useCase, DateTime start, DateTime end, double duration)
		{
			this.Start = start;
			this.End = end;
			this.Duration = duration / 1_000_000;
			this.Case = useCase;
			this.Warmup = warmup;
			this.Iteration = iteration;
		}

		public void Print()
			=> BenchPrinter.Print(this);
	}
}
