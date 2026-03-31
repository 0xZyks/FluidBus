using FluidBus.React.Interfaces;
using FluidBus.React.Core;
using FluidBus.Router.Interfaces;
using FluidBus.Router.Core;
using FluidBus.Benchmark.Core;
using FluidBus.Core.Abstracts;
using FluidBus.CallBack.Core;

namespace FluidBus;

public class FBus
{
    public static bool Route(IRouteEvent evt)
        => FRouter.Publish(evt);

    public static bool React(IReactEvent evt)
        => FReact.Publish(evt);

	public static BenchResult Bench(string useCase, int iteration, int warmup, BenchScenario scenario)
		=> FBench.Measure(useCase, iteration, warmup, scenario);

	public static object? CallBack(string name, object? data)
		=> FCallBack.Execute(name, data);
}
