using SchedulerCore.Models;

namespace SchedulerCore.Algorithms;

public interface IScheduler
{
    string Name { get; }
    SimulationResult Run(IEnumerable<ProcessModel> processes);
}
