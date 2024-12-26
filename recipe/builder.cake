//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

public Builder Build
{
    get
    {
        if (CommandLineOptions.Usage)
            return new Builder(() => Information(HelpMessages.Usage));

        if (CommandLineOptions.Targets.Values.Count() == 1)
            return new Builder(() => RunTarget(CommandLineOptions.Target.Value));

        return new Builder(() => RunTargets(CommandLineOptions.Targets.Values));
    }
}

CakeReport RunTargets(ICollection<string> targets)
    => RunTarget(GetOrAddTargetsTask(targets).Name);

Task<CakeReport> RunTargetsAsync(ICollection<string> targets)
    => RunTargetAsync(GetOrAddTargetsTask(targets).Name);

private ICakeTaskInfo GetOrAddTargetsTask(ICollection<string> targets)
{
    var targetsTaskName = string.Join('+', targets);
    var targetsTask = Tasks.FirstOrDefault(task => task.Name.Equals(targetsTaskName, StringComparison.OrdinalIgnoreCase));

    if (targetsTask == null)
    {
        var task = Task(targetsTaskName);

        foreach(var target in targets)
            task.IsDependentOn(target);

        targetsTask = task.Task;
    }

    return targetsTask;
}

public class Builder
{
    private Action _action;

    public Builder(Action action)
    {
        _action = action;
    }

    public void Run()
    {
        _action();
    }
}
