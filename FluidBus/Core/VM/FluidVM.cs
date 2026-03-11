using FluidBus.Core;

namespace FluidBus;

public class FluidVM
{
    public object? Run(byte[] token)
        => FluidCoreAPI.Execute(FluidCoreAPI.GetMethod(token));
}
